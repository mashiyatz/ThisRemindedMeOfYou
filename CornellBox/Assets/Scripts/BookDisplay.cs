using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEditor.Experimental.GraphView;

public class BookDisplay : MonoBehaviour
{
    public Texture bookTexture;
    public Image bookResponseImage;
    public Image bookResponseImageShadow;
    public GameObject book;
    public Material bookBackgroundMaterial;
    public Light bookSpotlight;
    public Vector3 bookStartPosition;
    public Vector3 bookEndPosition;
    public MeshRenderer bookRenderer;
    public TextMeshProUGUI bookTitleTextbox;
    public AudioClip bookResponseAudio;
    public AudioSource vocalAudioSource;
    public AudioSource bgAudioSource;

    public AudioClip pageFlippingSound;
    private Boolean isPlaying = false;

    void Start()
    {
        book.transform.localPosition = bookStartPosition;
        bookBackgroundMaterial.color = new Color(0, 0, 0, 0);
        bookSpotlight.intensity = 0;
    }

    void Update()
    {
        bookBackgroundMaterial.color = Color.Lerp(
            new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 0.6f),
            (5 - Mathf.Abs(bookEndPosition.x - book.transform.localPosition.x)) / 5
        );

        bookSpotlight.intensity = Mathf.Lerp(0, 3, 5 - Mathf.Abs(book.transform.localPosition.x) / 5);

        if (bookResponseImage)
        {
            bookResponseImage.color = Color.Lerp(
                new Color(0, 0, 0, 0),
                new Color(0, 0, 0, 1f),
                (5 - Mathf.Abs(bookEndPosition.x - book.transform.localPosition.x)) / 5
            );
            bookResponseImageShadow.color = Color.Lerp(
                new Color(1, 1, 1, 0),
                new Color(1, 1, 1, 1f),
                (5 - Mathf.Abs(bookEndPosition.x - book.transform.localPosition.x)) / 5
            );
        }

        if (vocalAudioSource.isPlaying && !isPlaying) { isPlaying = true; Debug.Log("Audio playing."); }
        else if (!vocalAudioSource.isPlaying && isPlaying) { isPlaying = false; Debug.Log("Audio stopped."); }
        
    }

    public void MoveBookIn()
    {
        if (book.transform.localPosition != bookEndPosition)
        {
            if (book.transform.localPosition == bookStartPosition) bgAudioSource.PlayOneShot(pageFlippingSound);
            bookRenderer.material.mainTexture = bookTexture;
            book.transform.localPosition =
                Vector3.MoveTowards(
                    book.transform.localPosition,
                    bookEndPosition,
                    Time.deltaTime* 5
                    );
        }
        else
        {
            vocalAudioSource.PlayOneShot(bookResponseAudio);
            PlayerController.currentState = PlayerController.PlayerState.READING;
        }
    }

    public void MoveBookOut()
    {
        if (book.transform.localPosition != bookStartPosition)
        {
            bookTitleTextbox.text = "";
            book.transform.localPosition =
                Vector3.MoveTowards(
                    book.transform.localPosition,
                    bookStartPosition,
                    Time.deltaTime * 5
                    );
        }
        else
        {
            vocalAudioSource.Stop();
            PlayerController.currentState = PlayerController.PlayerState.BROWSING;
        }
    }
}
