using UnityEngine;

public class V2PlayerController : MonoBehaviour
{
    public enum PlayerState { BROWSING, IN, READING, OUT, SUBMITTING }

    public static PlayerState currentState;
    public static float timeOfTransition;

    public V2BookDisplay bookDisplay;

    private PlayerState _previousState;

    void Start()
    {
        currentState = PlayerState.BROWSING;
        _previousState = PlayerState.BROWSING;
    }

    void Update()
    {
        if (currentState != _previousState)
        {
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
    }
}
