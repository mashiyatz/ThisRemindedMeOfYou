using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Reactive bridge between Unity C# services and the SolidJS browser UI.
/// All reactive properties expose a backing field and a corresponding OnChanged event.
/// WebGLBridge.cs subscribes to these events and forwards changes to the browser via jslib.
/// </summary>
public class V2UIBridge : MonoBehaviour
{
    // ── Inspector refs ────────────────────────────────────────────────────────
    [Header("Services")]
    public BookCoverService  coverService;
    public SupabaseService   supabaseService;
    public V2BookLoader      bookLoader;

    // ── Reactive state (C# → TS) ──────────────────────────────────────────────

    public string PlayerState      => _playerState;
    public string HoveredTitle     => _hoveredTitle;
    public bool   CoverLoading     => _coverLoading;
    public string CoverUrl         => _coverUrl;
    public bool   Submitting       => _submitting;
    public string SubmitError      => _submitError;

    public event Action<string> OnPlayerStateChanged;
    public event Action<string> OnHoveredTitleChanged;
    public event Action<bool>   OnCoverLoadingChanged;
    public event Action<string> OnCoverUrlChanged;
    public event Action<bool>   OnSubmittingChanged;
    public event Action<string> OnSubmitErrorChanged;
    // true = success, false = failure — fired once per SubmitBook call when the request completes
    public event Action<bool>   OnSubmitCompleted;
    // JSON payload fired when a book is interacted with
    public event Action<string> OnBookOpenData;

    // ── Private fields ────────────────────────────────────────────────────────

    private string    _playerState  = "BROWSING";
    private string    _hoveredTitle = "";
    private bool      _coverLoading;
    private string    _coverUrl     = "";
    private bool      _submitting;
    private string    _submitError  = "";

    private V2PlayerController.PlayerState _prevState;
    private Texture2D _fetchedCover;
    private BookEntry _pendingEntry;
    private Texture2D _pendingCover;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        _prevState = V2PlayerController.currentState;
    }

    void OnEnable()
    {
        V2BookDisplay.OnBookHighlight += HandleBookHighlight;
        V2BookDisplay.OnBookInteract  += HandleBookInteract;
        if (coverService != null)
        {
            coverService.OnCoverFetched += HandleCoverFetched;
            coverService.OnBusyChanged  += HandleCoverBusy;
        }
    }

    void OnDisable()
    {
        V2BookDisplay.OnBookHighlight -= HandleBookHighlight;
        V2BookDisplay.OnBookInteract  -= HandleBookInteract;
        if (coverService != null)
        {
            coverService.OnCoverFetched -= HandleCoverFetched;
            coverService.OnBusyChanged  -= HandleCoverBusy;
        }
    }

    void Update()
    {
        // Mirror V2PlayerController.currentState to the reactive string property
        var cur = V2PlayerController.currentState;
        if (cur != _prevState)
        {
            _prevState = cur;
            SetPlayerState(cur.ToString());
        }
    }

    // ── Commands (TS → C#) ────────────────────────────────────────────────────

    public void OpenPanel()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        _fetchedCover = null;
        SetCoverUrl("");
        SetCoverLoading(false);
        SetSubmitError("");
        V2PlayerController.currentState = V2PlayerController.PlayerState.SUBMITTING;
    }

    public void ClosePanel()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif
        BookEntry  spawnEntry = _pendingEntry;
        Texture2D  spawnCover = _pendingCover;
        _pendingEntry = null;
        _pendingCover = null;

        V2PlayerController.currentState = V2PlayerController.PlayerState.BROWSING;

        if (spawnEntry != null && bookLoader != null)
            bookLoader.SpawnBookWithTexture(spawnEntry, spawnCover);
    }

    public void FetchCover(string title, string author)
    {
        if (coverService == null) return;
        _fetchedCover = null;
        SetCoverUrl("");
        coverService.FetchCover(title, author);
    }

    public void SubmitBook(string title, string author, string response, bool isHandwritten, bool wantsNarrated)
    {
        bool fetchSucceeded = _fetchedCover != null;
        var cover = fetchSucceeded ? _fetchedCover
                                   : BookCoverTextureComposer.GenerateSolidCoverWithText(title, author);

        var entry = new BookEntry
        {
            title         = title,
            author        = author,
            submittedAt   = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            responseText  = response,
            isHandwritten = isHandwritten,
            wantsNarrated = wantsNarrated,
            coverImageUrl = fetchSucceeded ? (coverService?.LastFetchedUrl ?? "") : "",
        };

        // Supabase write is handled by the web layer — Unity only prepares the scene spawn.
        OnSubmitCompleted?.Invoke(true);

        _pendingEntry = entry;
        _pendingCover = cover;
    }

    public string GetRoomId() => RoomConfig.RoomId;

    public string FormatDate(string lang)
    {
        var now = DateTime.Now;
        switch (lang)
        {
            case "ko": return now.ToString("yyyy년 M월 d일");
            case "es": return now.ToString("d 'de' MMMM 'de' yyyy", new CultureInfo("es-ES"));
            default:   return now.ToString("MMMM d, yyyy",           new CultureInfo("en-US"));
        }
    }

    /// <summary>
    /// Called from TSX after cover is loaded; sets the background image on a VisualElement.
    /// Pass the element's .ve reference from TypeScript.
    /// </summary>
    public void ApplyCoverTexture(VisualElement ve)
    {
        if (ve == null || _fetchedCover == null) return;
        ve.style.backgroundImage = new StyleBackground(_fetchedCover);
    }

    // ── Cover service callbacks ───────────────────────────────────────────────

    private void HandleCoverFetched(Texture2D tex)
    {
        _fetchedCover = tex;
        SetCoverUrl(coverService?.LastFetchedUrl ?? "");
    }

    private void HandleCoverBusy(bool busy)
    {
        SetCoverLoading(busy);

        // If not busy and no cover was fetched, generate fallback so TSX sees a URL change
        if (!busy && _fetchedCover == null)
        {
            // Title isn't known here — TSX will call SubmitBook with the right title later.
            // Fallback generation happens in SubmitBook when cover is still null.
        }
    }

    // ── Book highlight + interact ─────────────────────────────────────────────

    private void HandleBookHighlight(string title) => SetHoveredTitle(title ?? "");

    private void HandleBookInteract(V2BookInteraction interaction)
    {
        var payload = new BookOpenPayload
        {
            title        = interaction.bookTitle        ?? "",
            author       = interaction.bookAuthor       ?? "",
            responseText = interaction.bookResponseText ?? "",
            imageUrl     = interaction.responseImageUrl ?? "",
            audioUrl     = interaction.responseAudioUrl ?? "",
        };
        OnBookOpenData?.Invoke(JsonUtility.ToJson(payload));
    }

    [System.Serializable]
    private class BookOpenPayload
    {
        public string title;
        public string author;
        public string responseText;
        public string imageUrl;
        public string audioUrl;
    }

    // ── Reactive setters ──────────────────────────────────────────────────────

    private void SetPlayerState(string v)
    {
        if (_playerState == v) return;
        _playerState = v;
        OnPlayerStateChanged?.Invoke(v);
    }

    private void SetHoveredTitle(string v)
    {
        if (_hoveredTitle == v) return;
        _hoveredTitle = v;
        OnHoveredTitleChanged?.Invoke(v);
    }

    private void SetCoverLoading(bool v)
    {
        if (_coverLoading == v) return;
        _coverLoading = v;
        OnCoverLoadingChanged?.Invoke(v);
    }

    private void SetCoverUrl(string v)
    {
        _coverUrl = v ?? "";
        OnCoverUrlChanged?.Invoke(_coverUrl);
    }

    private void SetSubmitting(bool v)
    {
        if (_submitting == v) return;
        _submitting = v;
        OnSubmittingChanged?.Invoke(v);
    }

    private void SetSubmitError(string v)
    {
        _submitError = v ?? "";
        OnSubmitErrorChanged?.Invoke(_submitError);
    }

}
