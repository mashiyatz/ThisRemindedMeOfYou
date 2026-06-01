using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using DG.Tweening;

public class V2BookLoader : MonoBehaviour
{
    public enum FilterMode { MostRecent, Random }

    [Header("References")]
    public GameObject bookPrefab;
    public Material bookOutlineMaterial;
    public SupabaseService supabaseService;

    [Header("Settings")]
    public int maxBooks = 5;
    public FilterMode filterMode = FilterMode.MostRecent;

    [Header("Spawn Jitter")]
    public float positionJitter = 0.04f;
    public float rotationJitter = 12f;

    private bool _isLoading;
    private int _nextAnchorIndex;

    void Start()
    {
        StartCoroutine(LoadAndSpawnBooks());
    }

    void Update()
    {
        if (V2PlayerController.currentState == V2PlayerController.PlayerState.SUBMITTING) return;
        if (Input.GetKeyDown(KeyCode.R)) SetFilterRecent();
        if (Input.GetKeyDown(KeyCode.T)) SetFilterRandom();
    }

    public void SetFilterRecent()
    {
        filterMode = FilterMode.MostRecent;
        ReloadBooks();
    }

    public void SetFilterRandom()
    {
        filterMode = FilterMode.Random;
        ReloadBooks();
    }

    public void ReloadBooks()
    {
        if (_isLoading) return;
        _nextAnchorIndex = 0;
        foreach (Transform anchor in transform)
        {
            for (int i = anchor.childCount - 1; i >= 0; i--)
                Destroy(anchor.GetChild(i).gameObject);
        }
        StartCoroutine(LoadAndSpawnBooks());
    }

    private IEnumerator LoadAndSpawnBooks()
    {
        _isLoading = true;

        // Load local curator books
        string jsonUrl = StreamingPath("books.json");
        BookCatalog catalog = null;

        using (UnityWebRequest req = UnityWebRequest.Get(jsonUrl))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[V2BookLoader] Failed to load books.json: {req.error}");
            else
                catalog = JsonUtility.FromJson<BookCatalog>(req.downloadHandler.text);
        }

        var localBooks = (catalog?.books != null) ? catalog.books : new List<BookEntry>();

        // Fetch visitor books from Supabase (fire even if local load failed)
        var cloudBooks = new List<BookEntry>();
        if (supabaseService != null)
            yield return supabaseService.FetchBooks(RoomConfig.RoomId, books => cloudBooks = books);

        // Merge: local curator books first, cloud books after; deduplicate by title+author
        var seen   = new System.Collections.Generic.HashSet<string>();
        var merged = new List<BookEntry>();
        foreach (var b in localBooks)
        {
            string key = (b.title + "|" + b.author).ToLowerInvariant();
            if (seen.Add(key)) merged.Add(b);
        }
        foreach (var b in cloudBooks)
        {
            string key = (b.title + "|" + b.author).ToLowerInvariant();
            if (seen.Add(key)) merged.Add(b);
        }

        if (merged.Count == 0)
        {
            Debug.LogError("[V2BookLoader] No books found locally or in Supabase.");
            _isLoading = false;
            yield break;
        }

        List<BookEntry> selected = FilterBooks(merged);
        int count = Mathf.Min(selected.Count, transform.childCount);

        for (int i = 0; i < count; i++)
        {
            BookEntry entry = selected[i];
            Transform anchor = transform.GetChild(i);

            GameObject bookGo = Instantiate(bookPrefab, anchor.position, anchor.rotation, anchor);
            ApplySpawnJitter(bookGo.transform);
            V2BookInteraction interaction = bookGo.AddComponent<V2BookInteraction>();
            interaction.outlineMaterial  = bookOutlineMaterial;
            interaction.bookTitle        = entry.title;
            interaction.bookAuthor       = entry.author;
            interaction.bookResponseText = entry.responseText;

            StartCoroutine(LoadBookAssets(entry, interaction, bookGo));
        }

        _nextAnchorIndex = count;
        _isLoading = false;
    }

    private IEnumerator LoadBookAssets(BookEntry entry, V2BookInteraction interaction, GameObject bookGo)
    {
        Renderer rend = bookGo.GetComponentInChildren<Renderer>();
        MeshFilter mf = bookGo.GetComponentInChildren<MeshFilter>();

        // Pre-made material takes priority; fall back to image-based atlas for new submissions
        if (!string.IsNullOrEmpty(entry.materialName) && rend != null)
        {
            Material mat = Resources.Load<Material>(entry.materialName);
            if (mat != null)
                rend.sharedMaterial = mat;
            else
                Debug.LogWarning($"[V2BookLoader] Material not found in Resources: '{entry.materialName}'");
        }
        else if (!string.IsNullOrEmpty(entry.coverImagePath) && rend != null && mf != null)
        {
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(StreamingPath(entry.coverImagePath)))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Texture2D cover = DownloadHandlerTexture.GetContent(req);
                    rend.material.mainTexture = BookCoverTextureComposer.BuildAtlas(cover, mf.sharedMesh);
                }
                else
                {
                    Debug.LogWarning($"[V2BookLoader] Cover load failed for '{entry.title}': {req.error}");
                }
            }
        }
        else if (!string.IsNullOrEmpty(entry.coverImageUrl) && rend != null && mf != null)
        {
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(entry.coverImageUrl))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Texture2D cover = DownloadHandlerTexture.GetContent(req);
                    rend.material.mainTexture = BookCoverTextureComposer.BuildAtlas(cover, mf.sharedMesh);
                }
                else
                {
                    Debug.LogWarning($"[V2BookLoader] Cloud cover load failed for '{entry.title}': {req.error}");
                    Texture2D fallback = BookCoverTextureComposer.GenerateSolidCoverWithText(entry.title, entry.author);
                    rend.material.mainTexture = BookCoverTextureComposer.BuildAtlas(fallback, mf.sharedMesh);
                }
            }
        }
        else if (rend != null && mf != null)
        {
            Texture2D fallback = BookCoverTextureComposer.GenerateSolidCoverWithText(entry.title, entry.author);
            rend.material.mainTexture = BookCoverTextureComposer.BuildAtlas(fallback, mf.sharedMesh);
        }

        if (!string.IsNullOrEmpty(entry.responseSpriteResourcePath))
        {
            interaction.bookResponseImageSprite = Resources.Load<Sprite>(entry.responseSpriteResourcePath);
            if (interaction.bookResponseImageSprite == null)
                Debug.LogWarning($"[V2BookLoader] Response sprite not found: '{entry.responseSpriteResourcePath}'");
        }
        else if (!string.IsNullOrEmpty(entry.responseSpriteImagePath))
        {
            string spriteUrl = StreamingPath(entry.responseSpriteImagePath);
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(spriteUrl))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(req);
                    interaction.bookResponseImageSprite = Sprite.Create(
                        tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    interaction.responseImageUrl = spriteUrl;
                }
                else
                {
                    Debug.LogWarning($"[V2BookLoader] Response sprite load failed for '{entry.title}': {req.error}");
                }
            }
        }

        if (!string.IsNullOrEmpty(entry.audioResourcePath))
        {
            interaction.bookResponseAudio = Resources.Load<AudioClip>(entry.audioResourcePath);
            if (interaction.bookResponseAudio == null)
                Debug.LogWarning($"[V2BookLoader] Audio resource not found: '{entry.audioResourcePath}'");
            // Resources-based audio has no accessible URL
        }
        else if (!string.IsNullOrEmpty(entry.audioPath))
        {
            interaction.responseAudioUrl = StreamingPath(entry.audioPath);
            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(interaction.responseAudioUrl, AudioType.MPEG))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    interaction.bookResponseAudio = DownloadHandlerAudioClip.GetContent(req);
                else
                    Debug.LogWarning($"[V2BookLoader] Audio load failed for '{entry.title}': {req.error}");
            }
        }
    }

    private List<BookEntry> FilterBooks(List<BookEntry> all)
    {
        var pool = new List<BookEntry>(all);

        if (filterMode == FilterMode.MostRecent)
        {
            pool.Sort((a, b) => ParseDate(b.submittedAt).CompareTo(ParseDate(a.submittedAt)));
        }
        else
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
        }

        int take = Mathf.Min(maxBooks, Mathf.Min(transform.childCount, pool.Count));
        return pool.GetRange(0, take);
    }

    private static DateTime ParseDate(string s)
    {
        return DateTime.TryParse(s, out DateTime d) ? d : DateTime.MinValue;
    }

    public void SpawnBook(BookEntry entry)
    {
        Transform anchor = transform.GetChild(_nextAnchorIndex % transform.childCount);
        _nextAnchorIndex++;

        for (int i = anchor.childCount - 1; i >= 0; i--)
            Destroy(anchor.GetChild(i).gameObject);

        GameObject bookGo = Instantiate(bookPrefab, anchor.position, anchor.rotation, anchor);
        ApplySpawnJitter(bookGo.transform);
        V2BookInteraction interaction = bookGo.AddComponent<V2BookInteraction>();
        interaction.outlineMaterial  = bookOutlineMaterial;
        interaction.bookTitle        = entry.title;
        interaction.bookAuthor       = entry.author;
        interaction.bookResponseText = entry.responseText;

        StartCoroutine(LoadBookAssets(entry, interaction, bookGo));
    }

    public void SpawnBookWithTexture(BookEntry entry, Texture2D cover)
    {
        if (transform.childCount == 0) return;
        StartCoroutine(SpawnWithAnimation(entry, cover));
    }

    private IEnumerator SpawnWithAnimation(BookEntry entry, Texture2D cover)
    {
        Transform  oldest    = transform.GetChild(0);
        Vector3    anchorPos = oldest.position;
        Quaternion anchorRot = oldest.rotation;

        // Detach the old book from its anchor so it can animate independently
        if (oldest.childCount > 0)
        {
            Transform oldBook = oldest.GetChild(0);
            oldBook.SetParent(null, true); // preserve world position
            oldBook.DOMove(oldBook.position + Vector3.up * 2.5f - Vector3.right * 2f, 1.0f)
                   .SetEase(Ease.InOutCubic)
                   .OnComplete(() => Destroy(oldBook.gameObject));
        }
        Destroy(oldest.gameObject);

        // Let the old book clear before the new one arrives
        yield return new WaitForSeconds(0.5f);

        // New anchor at end of list, same world slot
        var newAnchorGo = new GameObject("BookAnchor");
        newAnchorGo.transform.SetParent(transform);
        newAnchorGo.transform.SetPositionAndRotation(anchorPos, anchorRot);

        // Spawn from above and drop to the anchor position in world space
        GameObject bookGo = Instantiate(
            bookPrefab, anchorPos + Vector3.up * 1.0f, anchorRot, newAnchorGo.transform);
        V2BookInteraction interaction = bookGo.AddComponent<V2BookInteraction>();
        interaction.outlineMaterial  = bookOutlineMaterial;
        interaction.bookTitle        = entry.title;
        interaction.bookAuthor       = entry.author;
        interaction.bookResponseText = entry.responseText;

        Vector3 landPos = anchorPos + Vector3.up * 0.1f;
        landPos.x += UnityEngine.Random.Range(-positionJitter, positionJitter);
        landPos.z += UnityEngine.Random.Range(-positionJitter, positionJitter);
        Quaternion landRot = Quaternion.AngleAxis(
            UnityEngine.Random.Range(-rotationJitter, rotationJitter), Vector3.up) * anchorRot;

        bookGo.transform.DOMove(landPos, 0.7f).SetEase(Ease.InCubic);
        bookGo.transform.DORotateQuaternion(landRot, 0.7f).SetEase(Ease.InCubic);

        Renderer   rend = bookGo.GetComponentInChildren<Renderer>();
        MeshFilter mf   = bookGo.GetComponentInChildren<MeshFilter>();
        if (rend != null && mf != null)
            rend.material.mainTexture = BookCoverTextureComposer.BuildAtlas(cover, mf.sharedMesh);
    }

    private void ApplySpawnJitter(Transform t)
    {
        t.position += new Vector3(
            UnityEngine.Random.Range(-positionJitter, positionJitter),
            0f,
            UnityEngine.Random.Range(-positionJitter, positionJitter));

        t.rotation = Quaternion.AngleAxis(
            UnityEngine.Random.Range(-rotationJitter, rotationJitter), Vector3.up) * t.rotation;
    }

    private static string StreamingPath(string relativePath)
    {
        string full = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);
#if UNITY_WEBGL && !UNITY_EDITOR
        return full.Replace('\\', '/');
#else
        return new Uri(full).AbsoluteUri;
#endif
    }
}
