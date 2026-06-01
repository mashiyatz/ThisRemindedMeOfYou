using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseService : MonoBehaviour
{
    [Tooltip("e.g. https://xyzxyzxyz.supabase.co")]
    public string supabaseUrl;

    [Tooltip("Project anon/public key from Settings → API")]
    public string anonKey;

    // Fetches all books for a room, ordered newest-first.
    public IEnumerator FetchBooks(string roomId, Action<List<BookEntry>> onDone)
    {
        if (!IsConfigured()) { onDone?.Invoke(new List<BookEntry>()); yield break; }

        string url = $"{supabaseUrl.TrimEnd('/')}/rest/v1/books" +
                     $"?room_id=eq.{Uri.EscapeDataString(roomId)}&order=submitted_at.desc";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            SetHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SupabaseService] FetchBooks failed: {req.error}");
                onDone?.Invoke(new List<BookEntry>());
                yield break;
            }

            List<BookEntry> books = ParseBookRows(req.downloadHandler.text);
            onDone?.Invoke(books);
        }
    }

    // Inserts a single book row for a room. Calls onDone(true) on success.
    public IEnumerator InsertBook(string roomId, BookEntry entry, Action<bool> onDone)
    {
        if (!IsConfigured()) { onDone?.Invoke(false); yield break; }

        string url  = $"{supabaseUrl.TrimEnd('/')}/rest/v1/books";
        string body = BuildInsertJson(roomId, entry);
        byte[] raw  = Encoding.UTF8.GetBytes(body);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(raw);
            req.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(req, isWrite: true);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Prefer", "return=minimal");

            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success ||
                      req.responseCode == 201;

            if (!ok)
                Debug.LogWarning($"[SupabaseService] InsertBook failed ({req.responseCode}): {req.error}");

            onDone?.Invoke(ok);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private const string Schema = "reminded_me";

    private void SetHeaders(UnityWebRequest req, bool isWrite = false)
    {
        req.SetRequestHeader("apikey", anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {anonKey}");
        if (isWrite)
            req.SetRequestHeader("Content-Profile", Schema);
        else
            req.SetRequestHeader("Accept-Profile", Schema);
    }

    private bool IsConfigured()
    {
        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey))
        {
            Debug.LogWarning("[SupabaseService] supabaseUrl or anonKey is not set.");
            return false;
        }
        return true;
    }

    // Wraps the JSON array so JsonUtility can parse it.
    private static List<BookEntry> ParseBookRows(string json)
    {
        string wrapped = "{\"items\":" + json + "}";
        var wrapper = JsonUtility.FromJson<BookRowWrapper>(wrapped);
        if (wrapper?.items == null) return new List<BookEntry>();

        var result = new List<BookEntry>(wrapper.items.Length);
        foreach (var row in wrapper.items)
        {
            result.Add(new BookEntry
            {
                title         = row.title        ?? "",
                author        = row.author       ?? "",
                submittedAt   = row.submitted_at ?? "",
                coverImageUrl = row.cover_image_url ?? "",
                responseText  = row.response_text   ?? "",
                isHandwritten = row.is_handwritten,
                wantsNarrated = row.wants_narrated,
            });
        }
        return result;
    }

    private static string BuildInsertJson(string roomId, BookEntry e)
    {
        return JsonUtility.ToJson(new InsertPayload
        {
            room_id         = roomId,
            title           = e.title ?? "",
            author          = e.author ?? "",
            cover_image_url = e.coverImageUrl ?? "",
            response_text   = e.responseText  ?? "",
            is_handwritten  = e.isHandwritten,
            wants_narrated  = e.wantsNarrated,
        });
    }

    // ── Serialisation helpers ─────────────────────────────────────────────────

    [Serializable]
    private class BookRow
    {
        public string title;
        public string author;
        public string submitted_at;
        public string cover_image_url;
        public string response_text;
        public bool   is_handwritten;
        public bool   wants_narrated;
    }

    [Serializable]
    private class BookRowWrapper { public BookRow[] items; }

    [Serializable]
    private class InsertPayload
    {
        public string room_id;
        public string title;
        public string author;
        public string cover_image_url;
        public string response_text;
        public bool   is_handwritten;
        public bool   wants_narrated;
    }
}
