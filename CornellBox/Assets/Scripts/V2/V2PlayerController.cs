using UnityEngine;
using DG.Tweening;
using Cinemachine;

public class V2PlayerController : MonoBehaviour
{
    public enum PlayerState { BROWSING, IN, READING, OUT, SUBMITTING }

    public static PlayerState currentState;
    public static float timeOfTransition;
    public static float lastInteractionTime;

    public V2BookDisplay bookDisplay;

    private PlayerState _previousState;

    private const float HighlightTimeout = 15f;

    // Reduced-motion frame caps. SUBMITTING drops to near-idle so the wasm loop
    // frees the browser main thread for the HTML form's keystroke handling
    // (safe because WebGLBridge.ReceiveClosePanel restores the rate
    // synchronously — nothing waits on a 1fps frame to leave the form).
    public const int ReducedFps = 15;
    public const int ReducedSubmittingFps = 1;

    void Start()
    {
        currentState = PlayerState.BROWSING;
        _previousState = PlayerState.BROWSING;
        lastInteractionTime = Time.time;

        if (MotionConfig.Reduced)
        {
            // E-ink mode: cap the render loop near the panel's refresh ability,
            // collapse all tweens to ~instant (OnComplete callbacks still fire —
            // they drive the state machine), and stop the camera's Perlin drift
            // so static scenes produce no redraws.
            Application.targetFrameRate = ReducedFps;

            // Init must come first: DOTween initializes lazily on the first
            // tween and applies DOTweenSettings.timeScale (1) over any value
            // set before that point.
            DOTween.Init();
            DOTween.timeScale = 100f;

            foreach (var vcam in FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include))
            {
                var noise = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (noise != null) noise.m_AmplitudeGain = 0f;
            }
        }
    }

    void Update()
    {
        if (currentState != _previousState)
        {
            lastInteractionTime = Time.time;

            if (currentState == PlayerState.IN)
                bookDisplay.AnimateIn();
            else if (currentState == PlayerState.OUT)
                bookDisplay.AnimateOut();

            if (MotionConfig.Reduced)
                Application.targetFrameRate = currentState == PlayerState.SUBMITTING
                    ? ReducedSubmittingFps
                    : ReducedFps;

            _previousState = currentState;
        }

        if (currentState == PlayerState.READING && Input.GetMouseButtonDown(0))
        {
            currentState = PlayerState.OUT;
            timeOfTransition = Time.time;
        }

        // Inactivity timeout — dim book highlights after HighlightTimeout seconds
        if (currentState == PlayerState.BROWSING)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                lastInteractionTime = Time.time;
            else if (Time.time - lastInteractionTime > HighlightTimeout)
                V2BookInteraction.DimAll();
        }
    }
}
