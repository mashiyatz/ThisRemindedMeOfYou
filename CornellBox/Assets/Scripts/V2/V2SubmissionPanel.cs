using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class V2SubmissionPanel : MonoBehaviour
{
    // ── Services ──────────────────────────────────────────────
    [Header("Services")]
    public BookCoverService coverService;
    public V2BookLoader bookLoader;
    public SupabaseService supabaseService;

    // ── Room HUD ──────────────────────────────────────────────
    [Header("Room HUD")]
    [SerializeField] private GameObject roomHUD;
    [SerializeField] private Button openPanelBtn;
    [SerializeField] private TextMeshProUGUI openPanelBtnText;
    [SerializeField] private Button langBtnEN, langBtnKO, langBtnES;
    [SerializeField] private TextMeshProUGUI langBtnENText, langBtnKOText, langBtnESText;

    // ── Panel root ────────────────────────────────────────────
    [Header("Panel")]
    [SerializeField] private CanvasGroup screenOverlay;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private RectTransform panelRect;

    // ── Form ──────────────────────────────────────────────────
    [Header("Form")]
    [SerializeField] private GameObject stepForm;
    [SerializeField] private TextMeshProUGUI formHeading;
    [SerializeField] private TextMeshProUGUI dateLabel;
    [SerializeField] private Button langPillBtn;
    [SerializeField] private TextMeshProUGUI langPillText;

    [SerializeField] private RawImage coverRawImage;
    [SerializeField] private GameObject coverPlaceholder;
    [SerializeField] private GameObject coverLoader;
    [SerializeField] private Image[] dotImages;

    [SerializeField] private Button findCoverBtn;
    [SerializeField] private TextMeshProUGUI findCoverBtnText;

    [SerializeField] private TextMeshProUGUI labelTitle;
    [SerializeField] private TMP_InputField fTitle;
    [SerializeField] private Image titleBorderLine;
    [SerializeField] private TextMeshProUGUI errTitle;

    [SerializeField] private TextMeshProUGUI labelAuthor;
    [SerializeField] private TMP_InputField fAuthor;
    [SerializeField] private Image authorBorderLine;
    [SerializeField] private TextMeshProUGUI errAuthor;

    [SerializeField] private TextMeshProUGUI labelResponse;
    [SerializeField] private TMP_InputField fResponse;

    [SerializeField] private Toggle cHandwritten;
    [SerializeField] private TextMeshProUGUI checkHandLabel;
    [SerializeField] private Toggle cNarrated;
    [SerializeField] private TextMeshProUGUI checkNarrLabel;

    [SerializeField] private Button cancelBtn;
    [SerializeField] private TextMeshProUGUI cancelBtnText;
    [SerializeField] private Button submitBtn;
    [SerializeField] private TextMeshProUGUI submitBtnText;

    // ── Confirm overlay ───────────────────────────────────────
    [Header("Confirm Overlay")]
    [SerializeField] private GameObject confirmOverlay;
    [SerializeField] private TextMeshProUGUI confirmHeading;
    [SerializeField] private TextMeshProUGUI confirmBody;
    [SerializeField] private Button confirmYes;
    [SerializeField] private TextMeshProUGUI confirmYesText;
    [SerializeField] private Button confirmNo;
    [SerializeField] private TextMeshProUGUI confirmNoText;

    // ── Thanks step ───────────────────────────────────────────
    [Header("Thanks Step")]
    [SerializeField] private GameObject stepThanks;
    [SerializeField] private TextMeshProUGUI thanksHeading;
    [SerializeField] private TextMeshProUGUI thanksBody;
    [SerializeField] private Button thanksCloseBtn;
    [SerializeField] private TextMeshProUGUI thanksCloseBtnText;

    // ── Fonts ─────────────────────────────────────────────────
    [Header("Fonts — assign TMP Font Assets from Assets/Fonts/")]
    public TMP_FontAsset fontLoraReg;
    public TMP_FontAsset fontLoraItal;
    public TMP_FontAsset fontFellReg;
    public TMP_FontAsset fontFellItal;
    public TMP_FontAsset fontNoto;

    // ── Animation ─────────────────────────────────────────────
    [Header("Animation")]
    public float fadeDuration = 0.35f;

    // ── State ─────────────────────────────────────────────────
    private string    _lang         = "en";
    private Texture2D _fetchedCover;
    private bool      _promptedOnce;
    private Coroutine _dotCoroutine;
    private BookEntry  _pendingEntry;
    private Texture2D  _pendingCover;

    // ── Colors ────────────────────────────────────────────────
    private static readonly Color AccentColor     = ParseHex("7A3320");
    private static readonly Color AccentWarmColor = ParseHex("A84830");
    private static readonly Color LangMutedColor  = new Color(90f/255f, 80f/255f, 64f/255f, 0.55f);
    private static readonly Color RuleStrongColor = new Color(110f/255f, 90f/255f, 65f/255f, 0.28f);

    // ── Lifecycle ─────────────────────────────────────────────
    void Start()
    {
        screenOverlay.gameObject.SetActive(false);
        screenOverlay.alpha = 0f;
        screenOverlay.blocksRaycasts = false;
        screenOverlay.interactable = false;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        confirmOverlay.SetActive(false);
        stepThanks.SetActive(false);

        if (openPanelBtnText != null)
            Strings["en"].LeaveBook = openPanelBtnText.text;

        WireEvents();
        SetRoomLang("en");
    }

    void OnDisable()
    {
        StopDotAnim();
        UnwireEvents();
    }

    // ── Public ────────────────────────────────────────────────
    public void OpenPanel()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        _fetchedCover = null;
        _promptedOnce = false;
        ResetCover();
        ClearFields();
        SelectLang(_lang);
        ShowStep(stepForm);

        roomHUD.SetActive(false);
        screenOverlay.gameObject.SetActive(true);
        screenOverlay.alpha = 0f;
        screenOverlay.blocksRaycasts = true;
        screenOverlay.interactable = true;
        panelGroup.alpha = 0f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
        panelRect.localScale = Vector3.one * 0.985f;

        DOTween.Kill(screenOverlay);
        DOTween.Kill(panelGroup);
        DOTween.Kill(panelRect);

        screenOverlay.DOFade(1f, fadeDuration);
        panelGroup.DOFade(1f, fadeDuration);
        panelRect.DOScale(1f, fadeDuration).SetEase(Ease.OutCubic);

        V2PlayerController.currentState = V2PlayerController.PlayerState.SUBMITTING;
    }

    public void ClosePanel()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif
        StopDotAnim();
        confirmOverlay.SetActive(false);

        BookEntry  spawnEntry = _pendingEntry;
        Texture2D  spawnCover = _pendingCover;
        _pendingEntry = null;
        _pendingCover = null;

        screenOverlay.blocksRaycasts = false;
        screenOverlay.interactable = false;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        DOTween.Kill(screenOverlay);
        DOTween.Kill(panelGroup);

        panelGroup.DOFade(0f, fadeDuration);
        screenOverlay.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            screenOverlay.gameObject.SetActive(false);
            roomHUD.SetActive(true);
            V2PlayerController.currentState = V2PlayerController.PlayerState.BROWSING;

            if (spawnEntry != null)
                bookLoader.SpawnBookWithTexture(spawnEntry, spawnCover);
        });
    }

    // ── Language ──────────────────────────────────────────────
    public void SetRoomLang(string lang)
    {
        if (openPanelBtnText != null) openPanelBtnText.text = Strings[lang].LeaveBook;
        SetLangBtnActive(langBtnENText, lang == "en");
        SetLangBtnActive(langBtnKOText, lang == "ko");
        SetLangBtnActive(langBtnESText, lang == "es");
        SelectLang(lang);
    }

    private static void SetLangBtnActive(TextMeshProUGUI text, bool active)
    {
        if (text == null) return;
        text.color     = active ? AccentColor : LangMutedColor;
        text.fontStyle = active ? FontStyles.Italic : FontStyles.Normal;
    }

    private void CycleLang()
    {
        string next = _lang == "en" ? "ko" : (_lang == "ko" ? "es" : "en");
        SetRoomLang(next);
    }

    private void SelectLang(string lang)
    {
        _lang = lang;
        var s = Strings[lang];

        formHeading.text        = s.Heading;
        if (langPillText     != null) langPillText.text     = s.Pill;
        if (findCoverBtnText != null) findCoverBtnText.text = s.FindCover;
        labelTitle.text         = s.LabelTitle;
        labelAuthor.text        = s.LabelAuthor;
        labelResponse.text      = s.LabelResponse;
        checkHandLabel.text     = s.CheckHand;
        checkNarrLabel.text     = s.CheckNarr;
        cancelBtnText.text      = s.Cancel;
        submitBtnText.text      = s.Submit;
        confirmHeading.text     = s.ConfirmHead;
        confirmBody.text        = s.ConfirmBody;
        confirmYesText.text     = s.ConfirmYes;
        confirmNoText.text      = s.ConfirmNo;
        thanksHeading.text      = s.ThanksHead;
        thanksBody.text         = s.ThanksBody;
        thanksCloseBtnText.text = s.ThanksClose;
        if (dateLabel != null) dateLabel.text = FormatDate(lang);

        SetPlaceholder(fTitle,    s.PhTitle);
        SetPlaceholder(fAuthor,   s.PhAuthor);
        SetPlaceholder(fResponse, s.PhResponse);

        ApplyFonts(lang);
    }

    private static void SetPlaceholder(TMP_InputField field, string text)
    {
        if (field?.placeholder is TextMeshProUGUI ph) ph.text = text;
    }

    private static string FormatDate(string lang)
    {
        var now = DateTime.Now;
        switch (lang)
        {
            case "ko": return now.ToString("yyyy년 M월 d일");
            case "es": return now.ToString("d 'de' MMMM 'de' yyyy", new CultureInfo("es-ES"));
            default:   return now.ToString("MMMM d, yyyy", new CultureInfo("en-US"));
        }
    }

    // ── Fonts ─────────────────────────────────────────────────
    private void ApplyFonts(string lang)
    {
        bool ko = lang == "ko";
        bool es = lang == "es";
        var body    = ko ? fontNoto : fontLoraReg;
        var display = ko ? fontNoto : (es ? fontLoraItal : fontFellItal);
        var heading = ko ? fontNoto : fontFellReg;
        var italic  = ko ? fontNoto : fontLoraItal;
        var phTitle = ko ? fontNoto : (es ? fontLoraItal : fontFellItal);
        var phBody  = ko ? fontNoto : fontLoraItal;

        SetFont(formHeading,        display);
        SetFont(confirmHeading,     heading);
        SetFont(thanksHeading,      heading);
        SetFont(dateLabel,          italic);
        SetFont(confirmBody,        italic);
        SetFont(thanksBody,         italic);
        SetFont(cancelBtnText,      italic);
        SetFont(submitBtnText,      italic);
        SetFont(confirmYesText,     italic);
        SetFont(confirmNoText,      italic);
        SetFont(thanksCloseBtnText, italic);
        SetFont(checkHandLabel,     italic);
        SetFont(checkNarrLabel,     italic);
        SetFont(findCoverBtnText,   italic);

        SetInputFont(fTitle,    body, phTitle);
        SetInputFont(fAuthor,   body, phTitle);
        SetInputFont(fResponse, body, phBody);
    }

    private static void SetFont(TextMeshProUGUI text, TMP_FontAsset font)
    {
        if (text != null && font != null) text.font = font;
    }

    private static void SetInputFont(TMP_InputField field, TMP_FontAsset body, TMP_FontAsset placeholder)
    {
        if (field == null) return;
        if (field.textComponent != null && body != null) field.textComponent.font = body;
        if (field.placeholder is TextMeshProUGUI ph && placeholder != null) ph.font = placeholder;
    }

    // ── Setup ─────────────────────────────────────────────────
    private void WireEvents()
    {
        openPanelBtn?.onClick.AddListener(OpenPanel);
        langBtnEN?.onClick.AddListener(() => SetRoomLang("en"));
        langBtnKO?.onClick.AddListener(() => SetRoomLang("ko"));
        langBtnES?.onClick.AddListener(() => SetRoomLang("es"));
        langPillBtn?.onClick.AddListener(CycleLang);

        findCoverBtn?.onClick.AddListener(OnFindCover);
        cancelBtn?.onClick.AddListener(ClosePanel);
        submitBtn?.onClick.AddListener(HandleSubmit);
        confirmYes?.onClick.AddListener(ConfirmSubmit);
        confirmNo?.onClick.AddListener(DismissConfirm);
        thanksCloseBtn?.onClick.AddListener(ClosePanel);

        fTitle?.onValueChanged.AddListener(_ => ClearFieldError(titleBorderLine,  errTitle));
        fAuthor?.onValueChanged.AddListener(_ => ClearFieldError(authorBorderLine, errAuthor));

        coverService.OnCoverFetched += HandleCoverFetched;
        coverService.OnBusyChanged  += HandleBusyChanged;
    }

    private void UnwireEvents()
    {
        openPanelBtn?.onClick.RemoveListener(OpenPanel);
        langBtnEN?.onClick.RemoveAllListeners();
        langBtnKO?.onClick.RemoveAllListeners();
        langBtnES?.onClick.RemoveAllListeners();
        langPillBtn?.onClick.RemoveAllListeners();
        findCoverBtn?.onClick.RemoveAllListeners();
        cancelBtn?.onClick.RemoveAllListeners();
        submitBtn?.onClick.RemoveAllListeners();
        confirmYes?.onClick.RemoveAllListeners();
        confirmNo?.onClick.RemoveAllListeners();
        thanksCloseBtn?.onClick.RemoveAllListeners();
        fTitle?.onValueChanged.RemoveAllListeners();
        fAuthor?.onValueChanged.RemoveAllListeners();

        if (coverService != null)
        {
            coverService.OnCoverFetched -= HandleCoverFetched;
            coverService.OnBusyChanged  -= HandleBusyChanged;
        }
    }

    // ── Step navigation ───────────────────────────────────────
    private void ShowStep(GameObject step)
    {
        stepForm.SetActive(step == stepForm);
        stepThanks.SetActive(step == stepThanks);
    }

    // ── Cover ─────────────────────────────────────────────────
    private void OnFindCover()
    {
        string title  = fTitle.text.Trim();
        string author = fAuthor.text.Trim();

        if (string.IsNullOrEmpty(title))
        {
            ShowFieldError(titleBorderLine, errTitle, Strings[_lang].ErrTitle);
            return;
        }
        if (string.IsNullOrEmpty(author))
        {
            ShowFieldError(authorBorderLine, errAuthor, Strings[_lang].ErrAuthor);
            return;
        }

        coverService.FetchCover(title, author);
    }

    private void HandleCoverFetched(Texture2D tex)
    {
        _fetchedCover = tex;
        ShowCoverTexture(tex);
    }

    private void HandleBusyChanged(bool busy)
    {
        if (findCoverBtn != null) findCoverBtn.interactable = !busy;
        if (busy)
        {
            coverPlaceholder.SetActive(false);
            coverLoader.SetActive(true);
            StartDotAnim();
        }
        else
        {
            StopDotAnim();
            coverLoader.SetActive(false);

            if (_fetchedCover == null)
            {
                var fallback = BookCoverTextureComposer.GenerateSolidCover(fTitle.text.Trim());
                _fetchedCover = fallback;
                ShowCoverTexture(fallback);
            }
        }
    }

    private void ShowCoverTexture(Texture2D tex)
    {
        coverPlaceholder.SetActive(false);
        coverLoader.SetActive(false);
        coverRawImage.texture = tex;
        coverRawImage.gameObject.SetActive(true);
    }

    private void ResetCover()
    {
        _fetchedCover = null;
        if (coverRawImage != null)
        {
            coverRawImage.texture = null;
            coverRawImage.gameObject.SetActive(false);
        }
        coverPlaceholder.SetActive(true);
        coverLoader.SetActive(false);
        StopDotAnim();
    }

    private void ClearFields()
    {
        fTitle.text    = "";
        fAuthor.text   = "";
        fResponse.text = "";
        cHandwritten.isOn = false;
        cNarrated.isOn    = false;
        errTitle.gameObject.SetActive(false);
        errAuthor.gameObject.SetActive(false);
        if (titleBorderLine  != null) titleBorderLine.color  = RuleStrongColor;
        if (authorBorderLine != null) authorBorderLine.color = RuleStrongColor;
    }

    // ── Dot animation ─────────────────────────────────────────
    private void StartDotAnim()
    {
        StopDotAnim();
        _dotCoroutine = StartCoroutine(DotAnimLoop());
    }

    private void StopDotAnim()
    {
        if (_dotCoroutine != null) { StopCoroutine(_dotCoroutine); _dotCoroutine = null; }
        if (dotImages == null) return;
        foreach (var d in dotImages) { var c = d.color; c.a = 0.25f; d.color = c; }
    }

    private IEnumerator DotAnimLoop()
    {
        int frame = 0;
        while (true)
        {
            for (int i = 0; i < dotImages.Length; i++)
            {
                var c = dotImages[i].color;
                c.a = (i == frame % dotImages.Length) ? 1f : 0.25f;
                dotImages[i].color = c;
            }
            frame++;
            yield return new WaitForSeconds(0.2f);
        }
    }

    // ── Submission ────────────────────────────────────────────
    private void HandleSubmit()
    {
        string title  = fTitle.text.Trim();
        string author = fAuthor.text.Trim();

        bool valid = true;
        if (string.IsNullOrEmpty(title))
        {
            ShowFieldError(titleBorderLine, errTitle, Strings[_lang].ErrTitle);
            valid = false;
        }
        if (string.IsNullOrEmpty(author))
        {
            ShowFieldError(authorBorderLine, errAuthor, Strings[_lang].ErrAuthor);
            valid = false;
        }
        if (!valid) return;

        string response    = fResponse.text.Trim();
        bool isHandwritten = cHandwritten.isOn;

        if (string.IsNullOrEmpty(response) && !isHandwritten && !_promptedOnce)
        {
            _promptedOnce = true;
            confirmOverlay.SetActive(true);
            return;
        }

        DoSubmit(title, author, response, isHandwritten);
    }

    private void ConfirmSubmit()
    {
        confirmOverlay.SetActive(false);
        DoSubmit(
            fTitle.text.Trim(),
            fAuthor.text.Trim(),
            fResponse.text.Trim(),
            cHandwritten.isOn);
    }

    private void DismissConfirm()
    {
        confirmOverlay.SetActive(false);
        fResponse.Select();
    }

    private void DoSubmit(string title, string author, string response, bool isHandwritten)
    {
        var cover = _fetchedCover ?? BookCoverTextureComposer.GenerateSolidCover(title);

        var entry = new BookEntry
        {
            title         = title,
            author        = author,
            submittedAt   = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            responseText  = response,
            isHandwritten = isHandwritten,
            wantsNarrated = cNarrated.isOn,
            coverImageUrl = coverService?.LastFetchedUrl ?? "",
        };

        if (supabaseService != null)
            StartCoroutine(supabaseService.InsertBook(RoomConfig.RoomId, entry, _ => { }));

        _pendingEntry = entry;
        _pendingCover = cover;
        ShowStep(stepThanks);
    }

    // ── Field error helpers ───────────────────────────────────
    private static void ShowFieldError(Image borderLine, TextMeshProUGUI errLabel, string msg)
    {
        if (borderLine != null) borderLine.color = AccentWarmColor;
        errLabel.text = msg;
        errLabel.gameObject.SetActive(true);
    }

    private static void ClearFieldError(Image borderLine, TextMeshProUGUI errLabel)
    {
        if (borderLine != null) borderLine.color = RuleStrongColor;
        errLabel.gameObject.SetActive(false);
    }

    // ── Utilities ─────────────────────────────────────────────
    private static Color ParseHex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    // ═══════════════════════════════════════════════════════════
    // i18n strings
    // ═══════════════════════════════════════════════════════════
    private class Loc
    {
        public string LeaveBook;
        public string Pill, Heading;
        public string LabelTitle, LabelAuthor, LabelResponse;
        public string PhTitle, PhAuthor, PhResponse;
        public string FindCover;
        public string CheckHand, CheckNarr;
        public string Cancel, Submit;
        public string ErrTitle, ErrAuthor;
        public string ConfirmHead, ConfirmBody, ConfirmYes, ConfirmNo;
        public string ThanksHead, ThanksBody, ThanksClose;
    }

    private static readonly Dictionary<string, Loc> Strings = new Dictionary<string, Loc>
    {
        ["en"] = new Loc
        {
            LeaveBook     = "Leave a Book →",
            Pill          = "EN",
            Heading       = "What book would you like to share?",
            LabelTitle    = "TITLE",
            LabelAuthor   = "AUTHOR",
            LabelResponse = "YOUR WORDS",
            PhTitle       = "The name of the book",
            PhAuthor      = "Who wrote it",
            PhResponse    = "What does this book remind you of? There are no right answers here.",
            FindCover     = "find cover",
            CheckHand     = "I'm submitting a handwritten response",
            CheckNarr     = "I'd like this response to be narrated",
            Cancel        = "PERHAPS LATER",
            Submit        = "SHARE THIS BOOK",
            ErrTitle      = "A title is needed",
            ErrAuthor     = "And the author's name",
            ConfirmHead   = "Shall we leave some space for your words?",
            ConfirmBody   = "A response isn't required — but if something came to mind, we'd love to hold it alongside your book.",
            ConfirmYes    = "SHARE IT AS IS",
            ConfirmNo     = "LET ME ADD SOMETHING",
            ThanksHead    = "Thank you for leaving a mark.",
            ThanksBody    = "Your book will find its place on the shelf.",
            ThanksClose   = "RETURN TO THE ROOM",
        },
        ["ko"] = new Loc
        {
            LeaveBook     = "책 남기기 →",
            Pill          = "한국어",
            Heading       = "나누고 싶은 책이 있나요?",
            LabelTitle    = "제목",
            LabelAuthor   = "저자",
            LabelResponse = "당신의 이야기",
            PhTitle       = "책 제목을 입력해 주세요",
            PhAuthor      = "저자 이름을 입력해 주세요",
            PhResponse    = "이 책이 어떤 기억을 떠올리게 하나요? 정답은 없어요.",
            FindCover     = "표지 찾기",
            CheckHand     = "손으로 쓴 답변을 제출합니다",
            CheckNarr     = "이 답변을 낭독해 주셨으면 합니다",
            Cancel        = "나중에",
            Submit        = "이 책 나누기",
            ErrTitle      = "제목을 입력해 주세요",
            ErrAuthor     = "저자 이름도 입력해 주세요",
            ConfirmHead   = "당신의 이야기를 남겨 볼까요?",
            ConfirmBody   = "꼭 쓰지 않아도 괜찮아요 — 하지만 떠오른 것이 있다면, 책과 함께 담고 싶어요.",
            ConfirmYes    = "이대로 나누기",
            ConfirmNo     = "내용을 추가할게요",
            ThanksHead    = "흔적을 남겨 주셔서 감사해요.",
            ThanksBody    = "당신의 책이 책장에 자리를 찾을 거예요.",
            ThanksClose   = "방으로 돌아가기",
        },
        ["es"] = new Loc
        {
            LeaveBook     = "Dejar un libro →",
            Pill          = "ES",
            Heading       = "¿Qué libro te gustaría compartir?",
            LabelTitle    = "TÍTULO",
            LabelAuthor   = "AUTOR / AUTORA",
            LabelResponse = "TUS PALABRAS",
            PhTitle       = "El nombre del libro",
            PhAuthor      = "¿Quién lo escribió?",
            PhResponse    = "¿De qué te recuerda este libro? No hay respuestas correctas.",
            FindCover     = "buscar portada",
            CheckHand     = "Estoy enviando una respuesta escrita a mano",
            CheckNarr     = "Me gustaría que esta respuesta sea narrada",
            Cancel        = "QUIZÁS DESPUÉS",
            Submit        = "COMPARTIR ESTE LIBRO",
            ErrTitle      = "Se necesita un título",
            ErrAuthor     = "Y el nombre del autor",
            ConfirmHead   = "¿Dejamos espacio para tus palabras?",
            ConfirmBody   = "No es necesaria una respuesta — pero si algo vino a la mente, nos encantaría guardarlo junto a tu libro.",
            ConfirmYes    = "COMPARTIR ASÍ",
            ConfirmNo     = "DÉJAME AGREGAR ALGO",
            ThanksHead    = "Gracias por dejar una huella.",
            ThanksBody    = "Tu libro encontrará su lugar en el estante.",
            ThanksClose   = "VOLVER A LA HABITACIÓN",
        },
    };
}
