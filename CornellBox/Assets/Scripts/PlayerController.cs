using System.Collections;
using System.Collections.Generic;
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

    IEnumerator Twinkle()
    {
        Transform randomBook = bookRecList.GetChild(Random.Range(0, bookRecList.childCount));
        randomBook.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.6f, 0.6f, 0.6f, 1));
        yield return new WaitForSeconds(0.5f);
        randomBook.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
    }

    void Update()
    {
/*        if (currentState == PlayerState.BROWSING && (Time.time - timeOfTransition) > 10) 
        {
            StartCoroutine(Twinkle());
            timeOfTransition = Time.time; // reset timer
        }*/

            // Debug.Log(currentState);
        if (currentState != PlayerState.BROWSING && currentState != PlayerState.TITLE)
        {
            helpButton.interactable = false;
        }
        else
        {
            helpButton.interactable = true;
        }

        if (currentState == PlayerState.IN)
        {
            bookDisplay.MoveBookIn();
        } 
        else if (currentState == PlayerState.OUT)
        {
            bookDisplay.MoveBookOut();
        }
        else if (currentState == PlayerState.READING)
        {
            if (Input.GetMouseButtonDown(0))
            {
                currentState = PlayerState.OUT;
                timeOfTransition = Time.time; 
            }
        } 
    }
}
