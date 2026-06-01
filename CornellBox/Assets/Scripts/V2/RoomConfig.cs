using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RoomConfig : MonoBehaviour
{
    public static string RoomId { get; private set; } = "default";
    public static event Action OnRoomIdReady;

    void Awake()
    {
        StartCoroutine(ResolveRoomId());
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator ResolveRoomId()
    {
        RoomId = ParseQueryParam(Application.absoluteURL, "room") ?? "default";
        OnRoomIdReady?.Invoke();
        yield break;
    }

    private static string ParseQueryParam(string url, string key)
    {
        int q = url.IndexOf('?');
        if (q < 0) return null;
        string query = url.Substring(q + 1);
        foreach (string part in query.Split('&'))
        {
            int eq = part.IndexOf('=');
            if (eq < 0) continue;
            if (string.Equals(part.Substring(0, eq), key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(part.Substring(eq + 1));
        }
        return null;
    }
#else
    private IEnumerator ResolveRoomId()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "room_config.json");
        string uri  = new Uri(path).AbsoluteUri;

        using (UnityWebRequest req = UnityWebRequest.Get(uri))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var cfg = JsonUtility.FromJson<RoomConfigData>(req.downloadHandler.text);
                if (cfg != null && !string.IsNullOrEmpty(cfg.roomId))
                    RoomId = cfg.roomId;
            }
            else
            {
                Debug.LogWarning($"[RoomConfig] Could not read room_config.json ({req.error}), using default room.");
            }
        }

        OnRoomIdReady?.Invoke();
    }
#endif

    [Serializable]
    private class RoomConfigData
    {
        public string roomId;
        public string roomName;
    }
}
