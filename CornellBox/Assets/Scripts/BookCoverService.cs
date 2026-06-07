using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Pure fetch service — no UI references.
// Attach to any GameObject; call FetchCover() from V2SubmissionPanel.
// The original BookCoverFetcher.cs is left intact for use in BookCoverTest.unity.
//
// Fetch strategy (tried in order):
//   1. In-memory ISBN cache (per title+author key)
//   2. OpenLibrary search (title+author → ISBNs/cover_i → covers.openlibrary.org)
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
    private const int    MaxCovers     = 5;

    public string LastFetchedUrl { get; private set; }

    // First successful texture (for Unity spawning)
    public event Action<Texture2D> OnCoverFetched;
    // JSON array of ALL successful cover URLs ["url1","url2",...]
    public event Action<string>    OnCoverUrls;
    public event Action<string>    OnStatusChanged;
    public event Action<bool>      OnBusyChanged;

    // In-memory cache: normalized "title|author" → list of ISBNs
    private static readonly Dictionary<string, List<string>> _isbnCache = new();

    public void FetchCover(string title, string author)
    {
        if (string.IsNullOrEmpty(title)) return;
        StartCoroutine(FetchCoroutine(title, author));
    }

    private IEnumerator FetchCoroutine(string title, string author)
    {
        OnBusyChanged?.Invoke(true);
        OnStatusChanged?.Invoke("…");

        // Collect all candidate URLs (ISBN covers + cover_i covers + Google Books)
        List<string> allUrls = new();
        Texture2D    firstTexture = null;

        // ── Step 1: OpenLibrary search ──
        List<string> isbns    = GetCachedIsbns(title, author);
        List<string> coverIds = null;

        if (isbns == null)
        {
            yield return StartCoroutine(SearchOpenLibrary(title, author,
                v => isbns = v, v => coverIds = v));

            if ((isbns == null || isbns.Count == 0) && (coverIds == null || coverIds.Count == 0))
            {
                Debug.Log($"[BookCoverService] Title+author search found nothing, retrying title-only for \"{title}\"");
                yield return StartCoroutine(SearchOpenLibrary(title, null,
                    v => isbns = v, v => coverIds = v));
            }
        }

        // ── Step 2: Try ISBN covers ──
        if (isbns != null)
        {
            foreach (string isbn in isbns)
            {
                if (allUrls.Count >= MaxCovers) break;
                string coverUrl = $"{OlCoverBase}/{isbn}-L.jpg";
                OnStatusChanged?.Invoke("Downloading cover…");

                using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(coverUrl, nonReadable: false))
                {
                    yield return imgReq.SendWebRequest();
                    if (imgReq.result == UnityWebRequest.Result.Success)
                    {
                        if (firstTexture == null)
                        {
                            firstTexture = DownloadHandlerTexture.GetContent(imgReq);
                            LastFetchedUrl = coverUrl;
                            OnCoverFetched?.Invoke(firstTexture);
                        }
                        allUrls.Add(coverUrl);
                    }
                    else
                    {
                        Debug.Log($"[BookCoverService] ISBN {isbn} failed: {imgReq.error}");
                    }
                }
            }
        }

        // ── Step 3: Try cover_i fallbacks ──
        if (coverIds != null)
        {
            foreach (string cid in coverIds)
            {
                if (allUrls.Count >= MaxCovers) break;
                string coverUrl = $"{OlCoverIdBase}/{cid}-L.jpg";

                // Skip if already in list (same cover via ISBN route)
                if (allUrls.Contains(coverUrl)) continue;

                OnStatusChanged?.Invoke("Downloading cover…");

                using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(coverUrl, nonReadable: false))
                {
                    yield return imgReq.SendWebRequest();
                    if (imgReq.result == UnityWebRequest.Result.Success)
                    {
                        if (firstTexture == null)
                        {
                            firstTexture = DownloadHandlerTexture.GetContent(imgReq);
                            LastFetchedUrl = coverUrl;
                            OnCoverFetched?.Invoke(firstTexture);
                        }
                        allUrls.Add(coverUrl);
                    }
                    else
                    {
                        Debug.Log($"[BookCoverService] cover_i={cid} failed: {imgReq.error}");
                    }
                }
            }
        }

        // ── Step 4: Google Books fallback (only if no covers found yet) ──
        if (firstTexture == null)
        {
            string gbCoverUrl = null;
            yield return StartCoroutine(SearchGoogleBooks(title, author, v => gbCoverUrl = v));

            if (!string.IsNullOrEmpty(gbCoverUrl))
            {
                OnStatusChanged?.Invoke("Downloading cover…");

                using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(gbCoverUrl, nonReadable: false))
                {
                    yield return imgReq.SendWebRequest();
                    if (imgReq.result == UnityWebRequest.Result.Success)
                    {
                        firstTexture = DownloadHandlerTexture.GetContent(imgReq);
                        LastFetchedUrl = gbCoverUrl;
                        OnCoverFetched?.Invoke(firstTexture);
                        allUrls.Add(gbCoverUrl);
                    }
                    else
                    {
                        Debug.LogWarning($"[BookCoverService] Google Books cover download failed: {imgReq.error}");
                    }
                }
            }
        }

        // ── Step 5: Nothing worked — signal fallback ──
        if (firstTexture == null)
        {
            OnStatusChanged?.Invoke(string.Empty);
            OnCoverFetched?.Invoke(null);
        }

        // Fire URLs event regardless (may be empty — JS generates fallback)
        string urlsJson = BuildUrlsJson(allUrls);
        OnCoverUrls?.Invoke(urlsJson);

        OnStatusChanged?.Invoke(string.Empty);
        OnBusyChanged?.Invoke(false);
    }

    private static string BuildUrlsJson(List<string> urls)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < urls.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append("\"").Append(urls[i]).Append("\"");
        }
        sb.Append("]");
        return sb.ToString();
    }

    // ── OpenLibrary search ─────────────────────────────────────────────────

    private IEnumerator SearchOpenLibrary(string title, string author,
        Action<List<string>> onIsbns, Action<List<string>> onCoverIds)
    {
        string query = string.IsNullOrEmpty(author)
            ? $"{OlSearchApi}?q={Uri.EscapeDataString(title)}&limit={MaxCovers}&fields=isbn,cover_i"
            : $"{OlSearchApi}?q={Uri.EscapeDataString(title)}+{Uri.EscapeDataString(author)}&limit={MaxCovers}&fields=isbn,cover_i";

        using (UnityWebRequest req = UnityWebRequest.Get(query))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[BookCoverService] OpenLibrary search failed: {req.error}");
                onIsbns(null);
                onCoverIds(null);
                yield break;
            }

            string json = req.downloadHandler.text;
            ParseAllCovers(json, out List<string> isbns, out List<string> coverIds);

            if (isbns != null && isbns.Count > 0)
            {
                CacheIsbns(title, author, isbns);
                Debug.Log($"[BookCoverService] Found {isbns.Count} ISBN(s) for \"{title}\" by {author}");
            }

            onIsbns(isbns);
            onCoverIds(coverIds);
        }
    }

    private static void ParseAllCovers(string json, out List<string> isbns, out List<string> coverIds)
    {
        isbns    = new List<string>();
        coverIds = new List<string>();

        // Walk the JSON for each "isbn":[...] and "cover_i":... in docs[]
        const string docKey = "\"isbn\":[";
        int pos = 0;

        while (pos < json.Length)
        {
            int docStart = json.IndexOf(docKey, pos, StringComparison.Ordinal);
            if (docStart < 0) break;

            docStart += docKey.Length;
            int docEnd = json.IndexOf(']', docStart);
            if (docEnd < 0) break;

            string isbnSection = json.Substring(docStart, docEnd - docStart);
            if (!string.IsNullOrEmpty(isbnSection))
            {
                // Parse comma-separated quoted strings: "123","456"
                int ip = 0;
                while (ip < isbnSection.Length)
                {
                    int qs = isbnSection.IndexOf('"', ip);
                    if (qs < 0) break;
                    int qe = isbnSection.IndexOf('"', qs + 1);
                    if (qe < 0) break;
                    string isbn = isbnSection.Substring(qs + 1, qe - qs - 1);
                    if (!string.IsNullOrEmpty(isbn) && !isbns.Contains(isbn))
                        isbns.Add(isbn);
                    ip = qe + 1;
                }
            }

            pos = docEnd + 1;
        }

        // Parse cover_i values — look for "cover_i":NUMBER after each doc
        pos = 0;
        const string ciKey = "\"cover_i\":";

        while (pos < json.Length)
        {
            int ciStart = json.IndexOf("{\"isbn\"", pos, StringComparison.Ordinal);
            if (ciStart < 0) ciStart = pos;

            int ciFind = json.IndexOf(ciKey, ciStart, StringComparison.Ordinal);
            if (ciFind < 0) break;

            ciFind += ciKey.Length;
            int ciEnd = json.IndexOfAny(new[] { ',', '}' }, ciFind);
            if (ciEnd < 0) break;

            string cid = json.Substring(ciFind, ciEnd - ciFind).Trim();
            if (!string.IsNullOrEmpty(cid) && cid != "0" && !coverIds.Contains(cid))
                coverIds.Add(cid);

            pos = ciEnd + 1;
        }
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

    private static List<string> GetCachedIsbns(string title, string author) =>
        _isbnCache.TryGetValue(BuildCacheKey(title, author), out List<string> isbns) ? isbns : null;

    private static void CacheIsbns(string title, string author, List<string> isbns) =>
        _isbnCache[BuildCacheKey(title, author)] = isbns;
}
