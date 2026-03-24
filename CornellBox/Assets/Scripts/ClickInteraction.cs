using UnityEngine;
using DG.Tweening;

public class ClickInteraction : MonoBehaviour
{
    private Renderer _renderer;

    public BookDisplay bookDisplay;
    public string bookTitle;
    public Sprite bookResponseImageSprite;
    private AudioSource audioSource;
    public AudioClip bookResponseAudio;
    public AudioClip beepSound;

    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        audioSource = bookDisplay.gameObject.GetComponent<AudioSource>();
    }

    public void LightUp()
    {
        if (PlayerController.currentState == PlayerController.PlayerState.BROWSING)
        {
            _renderer.material.DOColor(new Color(0.6f, 0.6f, 0.6f, 1f), "_EmissionColor", 0.15f);
            bookDisplay.bookTitleTextbox.text = bookTitle;
            audioSource.PlayOneShot(beepSound);
        }
    }

    public void Dim()
    {
        if (PlayerController.currentState == PlayerController.PlayerState.BROWSING)
        {
            _renderer.material.DOColor(Color.black, "_EmissionColor", 0.15f);
            bookDisplay.bookTitleTextbox.text = "";
        }
    }

    public void PlayBookAnimation()
    {
        if (PlayerController.currentState == PlayerController.PlayerState.BROWSING)
        {
            PlayerController.timeOfTransition = Time.time;
            _renderer.material.DOColor(Color.black, "_EmissionColor", 0.15f);
            bookDisplay.bookResponseImage.sprite = bookResponseImageSprite;
            bookDisplay.bookResponseImageShadow.sprite = bookResponseImageSprite;
            bookDisplay.bookResponseAudio = bookResponseAudio;
            bookDisplay.bookTitleTextbox.text = bookTitle;
            bookDisplay.bookTexture = gameObject.GetComponent<Renderer>().material.GetTexture("_MainTex");
            PlayerController.currentState = PlayerController.PlayerState.IN;
        }
    }

    private void OnMouseEnter()
    {
        LightUp();
    }

    private void OnMouseExit()
    {
        Dim();
    }

    private void OnMouseDown()
    {
        PlayBookAnimation();
    }
}
