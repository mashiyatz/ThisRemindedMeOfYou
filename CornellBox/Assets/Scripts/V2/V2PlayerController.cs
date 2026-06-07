using UnityEngine;

public class V2PlayerController : MonoBehaviour
{
    public enum PlayerState { BROWSING, IN, READING, OUT, SUBMITTING }

    public static PlayerState currentState;
    public static float timeOfTransition;
    public static float lastInteractionTime;

    public V2BookDisplay bookDisplay;

    private PlayerState _previousState;

    private const float HighlightTimeout = 15f;

    void Start()
    {
        currentState = PlayerState.BROWSING;
        _previousState = PlayerState.BROWSING;
        lastInteractionTime = Time.time;
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
