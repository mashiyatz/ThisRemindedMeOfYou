using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Pure fetch service — no UI references.
// Attach to any GameObject; call FetchCover() from V2SubmissionPanel.
// The original BookCoverFetcher.cs is left intact for use in BookCoverTest.unity.
//
// Fetch strategy (tried in order):
//   1. In-memory ISBN cache (per title+author key)
//   2. OpenLibrary search (title+author → ISBN → covers.openlibrary.org)
//   3. Google Books API fallback (requires apiKey set in Inspector)
//   4. If all fail → OnCoverFetched(null) → caller generates solid-color cover
//
// Google Books API key: set `googleBooksApiKey` in the Unity Inspector
// on the GameObject that hosts this component. Get one from
// https://console.cloud.google.com/apis/credentials (enable Books API).
public class BookCoverService : MonoBehaviour
{
    [Tooltip("Google Books API key. Get one at https://console.cloud.google.com/apis/credentials")]
    [SerializeField] private string googleBooksApiKey;

    private const string OlSearchApi   = "https://openlibrary.org/search.json";
    private const string OlCoverBase   = "https://covers.openlibrary.org/b/isbn";
    private const string OlCoverIdBase = "https://covers.openlibrary.org/b/id";
    private const string GbSearchApi   = "https://www.googleapis.com/books/v1/volumes";

    public string LastFetchedUrl { get; private set; }

    public event Action<Texture2D> OnCoverFetched;
    public event Action<string>    OnStatusChanged;
    public event Action<bool>      OnBusyChanged;

    // In-memory cache: normalized "title|author" → first ISBN
    private static readonly Dictionary<string, string> _isbnCache = new();

    public void FetchCover(string title, string author)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(author)) return;
        StartCoroutine(FetchCoroutine(title, author));
    }

    private IEnumerator FetchCoroutine(string title, string author)
    {
        OnBusyChanged?.Invoke(true);
        OnStatusChanged?.Invoke("…");

        // ── Step 1: OpenLibrary search (title+author → ISBN/cover_i) ──
        string isbn    = GetCachedIsbn(title, author);
        string coverId = null;
        if (isbn == null)
        {
            yield return StartCoroutine(SearchIsbn(title, author, v => isbn = v, v => coverId = v));

            // ── Step 1 retry: title-only, if combined search found nothing ──
            if (string.IsNullOrEmpty(isbn) && string.IsNullOrEmpty(coverId))
            {
                Debug.Log($"[BookCoverService] Title+author search found nothing, retrying with title-only for \"{title}\"");
                yield return StartCoroutine(SearchIsbn(title, null, v => isbn = v, v => coverId = v));
            }
        }

        bool gotCover = false;

        // ── Step 1a: OpenLibrary ISBN cover ──
        if (!string.IsNullOrEmpty(isbn))
        {
            string coverUrl = $"{OlCoverBase}/{isbn}-L.jpg";
            LastFetchedUrl = coverUrl;
            OnStatusChanged?.Invoke("Downloading cover…");

            using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(coverUrl, nonReadable: false))
            {
                yield return imgReq.SendWebRequest();
                if (imgReq.result == UnityWebRequest.Result.Success)
                {
                    gotCover = true;
                    OnStatusChanged?.Invoke(string.Empty);
                    OnCoverFetched?.Invoke(DownloadHandlerTexture.GetContent(imgReq));
                }
                else
                {
                    Debug.LogWarning($"[BookCoverService] OpenLibrary ISBN cover failed for {isbn}: {imgReq.error}");
                }
            }
        }

        // ── Step 1b: OpenLibrary cover-i fallback (tried even when no ISBN) ──
        if (!gotCover && !string.IsNullOrEmpty(coverId))
        {
            string idCoverUrl = $"{OlCoverIdBase}/{coverId}-L.jpg";
            LastFetchedUrl = idCoverUrl;
            OnStatusChanged?.Invoke("Downloading cover…");

            using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(idCoverUrl, nonReadable: false))
            {
                yield return imgReq.SendWebRequest();
                if (imgReq.result == UnityWebRequest.Result.Success)
                {
                    gotCover = true;
                    OnStatusChanged?.Invoke(string.Empty);
                    OnCoverFetched?.Invoke(DownloadHandlerTexture.GetContent(imgReq));
                }
                else
                {
                    Debug.LogWarning($"[BookCoverService] OpenLibrary cover_i={coverId} failed: {imgReq.error}");
                }
            }
        }

        // ── Step 2: Google Books fallback ──
        string gbCoverUrl = null;
        if (!gotCover)
        {
            yield return StartCoroutine(SearchGoogleBooks(title, author, v => gbCoverUrl = v));

            if (!string.IsNullOrEmpty(gbCoverUrl))
            {
                LastFetchedUrl = gbCoverUrl;
                OnStatusChanged?.Invoke("Downloading cover…");

                using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(gbCoverUrl, nonReadable: false))
                {
                    yield return imgReq.SendWebRequest();
                    if (imgReq.result == UnityWebRequest.Result.Success)
                    {
                        gotCover = true;
                        OnStatusChanged?.Invoke(string.Empty);
                        OnCoverFetched?.Invoke(DownloadHandlerTexture.GetContent(imgReq));
                    }
                    else
                    {
                        Debug.LogWarning($"[BookCoverService] Google Books cover download failed: {imgReq.error}");
                    }
                }
            }
        }

        // ── Step 3: Nothing worked — caller generates solid-color ──
        if (!gotCover)
        {
            OnStatusChanged?.Invoke(string.Empty);
            OnCoverFetched?.Invoke(null);
        }

        OnBusyChanged?.Invoke(false);
    }

    // ── OpenLibrary search ─────────────────────────────────────────────────

    private IEnumerator SearchIsbn(string title, string author, Action<string> onIsbn, Action<string> onCoverId)
    {
        string query = string.IsNullOrEmpty(author)
            ? $"{OlSearchApi}?q={Uri.EscapeDataString(title)}&limit=1&fields=isbn,cover_i"
            : $"{OlSearchApi}?q={Uri.EscapeDataString(title)}+{Uri.EscapeDataString(author)}&limit=1&fields=isbn,cover_i";
        string json = null;

        using (UnityWebRequest req = UnityWebRequest.Get(query))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[BookCoverService] OpenLibrary search failed: {req.error}");
                onIsbn(null);
                onCoverId(null);
                yield break;
            }

            json = req.downloadHandler.text;
        }

        string isbn = ParseFirstIsbn(json);
        if (!string.IsNullOrEmpty(isbn))
        {
            Debug.Log($"[BookCoverService] Found ISBN {isbn} for \"{title}\" by {author}");
            CacheIsbn(title, author, isbn);
        }
        onIsbn(isbn);
        onCoverId(ParseFirstCoverId(json));
    }

    private static string ParseFirstCoverId(string json)
    {
        const string key = "\"cover_i\":";
        int start = json.IndexOf(key, StringComparison.Ordinal);
        if (start < 0) return null;
        start += key.Length;
        int end = json.IndexOfAny(new[] { ',', '}' }, start);
        return end < 0 ? null : json.Substring(start, end - start).Trim();
    }

    private static string ParseFirstIsbn(string json)
    {
        const string key = "\"isbn\":[\"";
        int start = json.IndexOf(key, StringComparison.Ordinal);
        if (start < 0) return null;
        start += key.Length;
        int end = json.IndexOf('"', start);
        return end < 0 ? null : json.Substring(start, end - start);
    }

    // ── Google Books search ────────────────────────────────────────────────

    private IEnumerator SearchGoogleBooks(string title, string author, Action<string> onCoverUrl)
    {
        if (string.IsNullOrEmpty(googleBooksApiKey))
        {
            Debug.Log("[BookCoverService] googleBooksApiKey not set — skipping Google Books fallback.");
            onCoverUrl(null);
            yield break;
        }

        string query = $"{GbSearchApi}?q=intitle:{Uri.EscapeDataString(title)}+inauthor:{Uri.EscapeDataString(author)}&key={googleBooksApiKey}";

        using (UnityWebRequest req = UnityWebRequest.Get(query))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[BookCoverService] Google Books search failed: {req.error}");
                onCoverUrl(null);
                yield break;
            }

            string coverUrl = ParseGoogleBooksCoverUrl(req.downloadHandler.text);
            if (!string.IsNullOrEmpty(coverUrl))
            {
                // Google Books returns http:// — upgrade to https://
                if (coverUrl.StartsWith("http://"))
                    coverUrl = "https://" + coverUrl.Substring(7);

                Debug.Log($"[BookCoverService] Found Google Books cover for \"{title}\" by {author}");
                onCoverUrl(coverUrl);
            }
            else
            {
                Debug.Log($"[BookCoverService] Google Books found no cover for \"{title}\" by {author}");
                onCoverUrl(null);
            }
        }
    }

    private static string ParseGoogleBooksCoverUrl(string json)
    {
        // Walk the JSON for items[0].volumeInfo.imageLinks.thumbnail
        const string key = "\"thumbnail\":\"";
        int start = json.IndexOf(key, StringComparison.Ordinal);
        if (start < 0) return null;
        start += key.Length;
        int end = json.IndexOf('"', start);
        if (end < 0) return null;
        return json.Substring(start, end - start);
    }

    // ── ISBN cache (in-memory) ─────────────────────────────────────────────

    private static string BuildCacheKey(string title, string author) =>
        $"{title?.Trim().ToLowerInvariant()}|{author?.Trim().ToLowerInvariant()}";

    private static string GetCachedIsbn(string title, string author) =>
        _isbnCache.TryGetValue(BuildCacheKey(title, author), out string isbn) ? isbn : null;

    private static void CacheIsbn(string title, string author, string isbn) =>
        _isbnCache[BuildCacheKey(title, author)] = isbn;
}
