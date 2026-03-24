using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState {BROWSING, IN, READING, OUT, TITLE};

    public static PlayerState currentState;
    public static PlayerState lastState;
    public static float timeOfTransition;

    public BookDisplay bookDisplay;
    public Toggle helpButton;
    public Transform bookRecList;

    void Start()
    {
        // currentState = PlayerState.TITLE;
        currentState = PlayerState.BROWSING;
        lastState = PlayerState.BROWSING;
    }

    public void ToTitleScreen()
    {
        if (currentState == PlayerState.BROWSING)
        {
            lastState = currentState;
            currentState = PlayerState.TITLE;
        } else if (currentState == PlayerState.TITLE)
        {
            currentState = PlayerState.BROWSING;
        }
    }

    void Update()
    {
        helpButton.interactable = (currentState == PlayerState.BROWSING || currentState == PlayerState.TITLE);

        if (currentState != lastState)
        {
            if (currentState == PlayerState.IN)
                bookDisplay.MoveBookIn();
            else if (currentState == PlayerState.OUT)
                bookDisplay.MoveBookOut();

            lastState = currentState;
        }

        if (currentState == PlayerState.READING && Input.GetMouseButtonDown(0))
        {
            currentState = PlayerState.OUT;
            timeOfTransition = Time.time;
        }
    }
}
