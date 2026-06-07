using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Bridges the Unity WebGL build with the SolidJS UI overlay running in the browser.
/// - Outbound (C#→JS): calls jslib functions that fire window.unityBridge callbacks in SolidJS.
/// - Inbound (JS→C#): static methods called via unityInstance.SendMessage('WebGLBridge', ...).
/// The GameObject this is attached to must be named "WebGLBridge" in the scene.
/// </summary>
public class WebGLBridge : MonoBehaviour
{
    [SerializeField] V2UIBridge uiBridge;

    // ── jslib imports (no-ops in Editor/non-WebGL) ────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void JS_NotifyStateChange(string state);
    [DllImport("__Internal")] static extern void JS_NotifyBookHighlight(string title);
    [DllImport("__Internal")] static extern void JS_NotifyCoverUrl(string url);
    [DllImport("__Internal")] static extern void JS_NotifyCoverLoading(bool loading);
    [DllImport("__Internal")] static extern void JS_NotifySubmitResult(bool ok);
    [DllImport("__Internal")] static extern void JS_NotifyBookOpen(string json);
    [DllImport("__Internal")] static extern void JS_NotifyCoverUrls(string urlsJson);
#else
    static void JS_NotifyStateChange(string _) {}
    static void JS_NotifyBookHighlight(string _) {}
    static void JS_NotifyCoverUrl(string _) {}
    static void JS_NotifyCoverLoading(bool _) {}
    static void JS_NotifySubmitResult(bool _) {}
    static void JS_NotifyBookOpen(string _) {}
    static void JS_NotifyCoverUrls(string _) {}
#endif

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    V2PlayerController.PlayerState _prevState;

    void Start()
    {
        _prevState = V2PlayerController.currentState;
        JS_NotifyStateChange(_prevState.ToString());
    }

    void OnEnable()
    {
        V2BookDisplay.OnBookHighlight += HandleBookHighlight;
        if (uiBridge != null)
        {
            uiBridge.OnCoverUrlChanged     += HandleCoverUrl;
            uiBridge.OnCoverLoadingChanged += HandleCoverLoading;
            uiBridge.OnSubmitCompleted     += HandleSubmitCompleted;
            uiBridge.OnBookOpenData        += HandleBookOpen;
            uiBridge.OnCoverUrlsChanged    += HandleCoverUrls;
        }
    }

    void OnDisable()
    {
        V2BookDisplay.OnBookHighlight -= HandleBookHighlight;
        if (uiBridge != null)
        {
            uiBridge.OnCoverUrlChanged     -= HandleCoverUrl;
            uiBridge.OnCoverLoadingChanged -= HandleCoverLoading;
            uiBridge.OnSubmitCompleted     -= HandleSubmitCompleted;
            uiBridge.OnBookOpenData        -= HandleBookOpen;
            uiBridge.OnCoverUrlsChanged    -= HandleCoverUrls;
        }
    }

    void Update()
    {
        var cur = V2PlayerController.currentState;
        if (cur != _prevState)
        {
            _prevState = cur;
            JS_NotifyStateChange(cur.ToString());
        }
    }

    // ── Event handlers (Unity → JS) ───────────────────────────────────────────

    void HandleBookHighlight(string title) => JS_NotifyBookHighlight(title ?? "");
    void HandleCoverUrl(string url)        => JS_NotifyCoverUrl(url ?? "");
    void HandleCoverLoading(bool loading)  => JS_NotifyCoverLoading(loading);
    void HandleSubmitCompleted(bool ok)    => JS_NotifySubmitResult(ok);
    void HandleBookOpen(string json)       => JS_NotifyBookOpen(json);
    void HandleCoverUrls(string urlsJson)  => JS_NotifyCoverUrls(urlsJson ?? "[]");

    // ── Inbound commands (JS → C#, called via SendMessage) ───────────────────

    public void ReceiveOpenPanel()
    {
        if (uiBridge != null) uiBridge.OpenPanel();
    }

    public void ReceiveClosePanel()
    {
        if (uiBridge != null) uiBridge.ClosePanel();
    }

    /// <summary>Expects JSON: {"title":"...","author":"..."}</summary>
    public void ReceiveFetchCover(string json)
    {
        if (uiBridge == null) return;
        var data = JsonUtility.FromJson<FetchCoverPayload>(json);
        if (data != null) uiBridge.FetchCover(data.title, data.author);
    }

    public void ReceiveDismissBook()
    {
        if (V2PlayerController.currentState == V2PlayerController.PlayerState.READING)
        {
            V2PlayerController.currentState      = V2PlayerController.PlayerState.OUT;
            V2PlayerController.timeOfTransition  = Time.time;
        }
    }

    /// <summary>Expects JSON: {"title":"...","author":"...","response":"...","isHandwritten":false,"wantsNarrated":false}</summary>
    public void ReceiveSubmit(string json)
    {
        if (uiBridge == null) return;
        var data = JsonUtility.FromJson<SubmitPayload>(json);
        if (data != null)
            uiBridge.SubmitBook(data.title, data.author, data.response, data.isHandwritten, data.wantsNarrated);
    }

    // ── JSON payload types ────────────────────────────────────────────────────

    [System.Serializable]
    class FetchCoverPayload
    {
        public string title;
        public string author;
    }

    [System.Serializable]
    class SubmitPayload
    {
        public string title;
        public string author;
        public string response;
        public bool   isHandwritten;
        public bool   wantsNarrated;
    }
}
