using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class V2BookDisplay : MonoBehaviour
{
    public static Action<string> OnBookHighlight;
    public static Action<V2BookInteraction> OnBookInteract;

    [HideInInspector] public Texture bookTexture;

    public Image bookResponseImage;
    public Image bookResponseBackground;
    public Vector3 bookEndPosition;
    public TextMeshProUGUI bookTitleTextbox;
    public TextMeshProUGUI responseTextBox;
    public AudioSource bgAudioSource;
    public AudioClip pageFlippingSound;
    public float fadeDuration = 0.4f;

    [Header("Response Text")]
    public float responseMinFontSize = 14f;
    public float scrollSpeed = 25f;

    [Header("Bobbing")]
    public float bobHeight   = 0.018f;
    public float bobDuration = 1.9f;
    public float tiltDegrees = 1.2f;

    private MeshRenderer  _bookRenderer;
    private Vector3       _startPosition;
    private float         _responseMaxFontSize;
    private RectTransform _responseViewport;
    private float         _scrollOffset;
    private float         _maxScrollOffset;
    private bool          _isScrollable;
    private Coroutine     _scrollRoutine;

    private Vector3 _bobBasePosition;
    private Vector3 _bobBaseRotation;
    private Tween   _bobPosTween;
    private Tween   _bobRotTween;

    void Start()
    {
        _startPosition = transform.position;
        _bookRenderer  = GetComponentInChildren<MeshRenderer>();

        if (bookResponseImage != null)
        {
            bookResponseImage.color = new Color(1, 1, 1, 0);
            bookResponseImage.preserveAspect = true;
        }
        if (bookResponseBackground != null)
            bookResponseBackground.color = new Color(0, 0, 0, 0);

        if (responseTextBox != null)
        {
            responseTextBox.text = "";
            _responseMaxFontSize = responseTextBox.fontSize;
            _responseViewport    = responseTextBox.rectTransform.parent as RectTransform;
        }
    }

    void OnEnable()
    {
        OnBookHighlight += HandleBookHighlight;
        OnBookInteract  += HandleBookInteract;
    }

    void OnDisable()
    {
        OnBookHighlight -= HandleBookHighlight;
        OnBookInteract  -= HandleBookInteract;
    }

    void Update()
    {
        if (!_isScrollable) return;
        if (V2PlayerController.currentState != V2PlayerController.PlayerState.READING) return;

        float delta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(delta) > 0.001f)
        {
            // Scrolling down (negative delta) increases offset, moving content up
            _scrollOffset = Mathf.Clamp(_scrollOffset - delta * scrollSpeed, 0f, _maxScrollOffset);
            ApplyScrollPosition();
        }
    }

    private void HandleBookHighlight(string title)
    {
        if (bookTitleTextbox != null) bookTitleTextbox.text = title;
    }

    private void HandleBookInteract(V2BookInteraction interaction)
    {
        Renderer rend = interaction.GetComponentInChildren<Renderer>();
        if (rend != null) bookTexture = rend.material.mainTexture;

        bool hasSprite = interaction.bookResponseImageSprite != null;

        if (bookResponseImage != null)
        {
            bookResponseImage.sprite = interaction.bookResponseImageSprite;
            bookResponseImage.gameObject.SetActive(hasSprite);
        }

        if (bookTitleTextbox != null)
            bookTitleTextbox.text = interaction.bookTitle;

        if (responseTextBox != null)
        {
            responseTextBox.text = interaction.bookResponseText ?? "";
            bool showText = !hasSprite && !string.IsNullOrEmpty(interaction.bookResponseText);
            responseTextBox.gameObject.SetActive(showText);

            if (showText)
            {
                if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
                _scrollRoutine = StartCoroutine(FitAndSetupScroll());
            }
        }
    }

    // ── Two-tier overflow handler ─────────────────────────────────────────────

    private IEnumerator FitAndSetupScroll()
    {
        _isScrollable = false;
        _scrollOffset = 0f;

        if (_responseViewport == null) yield break;

        var   rt        = responseTextBox.rectTransform;
        float viewportH = _responseViewport.rect.height;

        // Tier 1: enable auto-sizing — TMP shrinks font down to responseMinFontSize
        responseTextBox.enableAutoSizing = true;
        responseTextBox.fontSizeMin      = responseMinFontSize;
        responseTextBox.fontSizeMax      = _responseMaxFontSize;
        responseTextBox.overflowMode     = TextOverflowModes.Overflow;

        // Give the auto-sizer the correct vertical bounds to work within
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportH);
        ApplyScrollPosition();

        Canvas.ForceUpdateCanvases();
        yield return null;

        float contentH = responseTextBox.preferredHeight;

        if (contentH > viewportH + 2f)
        {
            // Tier 2: expand content rect and enable scrolling
            _isScrollable    = true;
            _maxScrollOffset = contentH - viewportH;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);
        }

        ApplyScrollPosition();
    }

    private void ApplyScrollPosition()
    {
        if (responseTextBox == null) return;
        // pivot.y = 1 (top): positive anchoredPosition.y moves the rect upward,
        // revealing content further down — equivalent to scrolling down.
        var pos = responseTextBox.rectTransform.anchoredPosition;
        pos.y = _scrollOffset;
        responseTextBox.rectTransform.anchoredPosition = pos;
    }

    // ── Bobbing ───────────────────────────────────────────────────────────────

    private void StartBobbing()
    {
        _bobPosTween?.Kill();
        _bobRotTween?.Kill();

        _bobPosTween = transform
            .DOMove(_bobBasePosition + Vector3.up * bobHeight, bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        _bobRotTween = transform
            .DORotate(_bobBaseRotation + new Vector3(0f, 0f, tiltDegrees), bobDuration * 1.15f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    public void AnimateIn()
    {
        KillTweens();

        bgAudioSource.PlayOneShot(pageFlippingSound);
        _bookRenderer.material.mainTexture = bookTexture;

        bookResponseImage?.DOFade(1f, fadeDuration);
        bookResponseBackground?.DOFade(0.6f, fadeDuration * 0.8f);

        transform.DOMove(bookEndPosition, fadeDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                _bobBasePosition = transform.position;
                _bobBaseRotation = transform.eulerAngles;
                V2PlayerController.currentState = V2PlayerController.PlayerState.READING;
                StartBobbing();
            });
    }

    public void AnimateOut()
    {
        KillTweens();

        bookResponseImage?.DOFade(0f, fadeDuration);
        bookResponseBackground?.DOFade(0f, fadeDuration);

        transform.DOMove(_startPosition, fadeDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                if (bookTitleTextbox != null) bookTitleTextbox.text = "";
                if (responseTextBox != null) responseTextBox.text = "";
                if (V2PlayerController.currentState == V2PlayerController.PlayerState.OUT)
                    V2PlayerController.currentState = V2PlayerController.PlayerState.BROWSING;
            });
    }

    private void KillTweens()
    {
        transform.DOKill();
        DOTween.Kill(bookResponseImage);
        DOTween.Kill(bookResponseBackground);
        _bobPosTween?.Kill();
        _bobRotTween?.Kill();
        _bobPosTween = null;
        _bobRotTween = null;
    }
}
