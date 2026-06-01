using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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

    private const float MoveSpeed = 5f;

    void Start()
    {
        // Compute an off-screen start position just past the left edge of the viewport,
        // at the same world depth and height as bookEndPosition.
        if (Camera.main != null)
        {
            Transform parent = book.transform.parent;
            Vector3 worldEnd = parent != null
                ? parent.TransformPoint(bookEndPosition)
                : bookEndPosition;
            float depth = worldEnd.z - Camera.main.transform.position.z;
            float leftEdgeX  = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;
            float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)).x;
            float screenWidth = rightEdgeX - leftEdgeX;
            Vector3 worldStart = new Vector3(leftEdgeX - screenWidth, worldEnd.y, worldEnd.z);
            bookStartPosition = parent != null
                ? parent.InverseTransformPoint(worldStart)
                : worldStart;
        }

        book.transform.localPosition = bookStartPosition;
        bookBackgroundMaterial.color = new Color(0, 0, 0, 0);
        bookSpotlight.intensity = 0;
    }

    public void MoveBookIn()
    {
        float duration = Vector3.Distance(bookStartPosition, bookEndPosition) / MoveSpeed;

        bgAudioSource.PlayOneShot(pageFlippingSound);
        bookRenderer.material.mainTexture = bookTexture;

        book.transform.DOKill();
        book.transform.DOLocalMove(bookEndPosition, duration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                vocalAudioSource.PlayOneShot(bookResponseAudio);
                PlayerController.currentState = PlayerController.PlayerState.READING;
            });

        DOTween.To(() => bookBackgroundMaterial.color,
                   x => bookBackgroundMaterial.color = x,
                   new Color(0, 0, 0, 0.6f), duration);

        bookSpotlight.DOIntensity(3f, duration);

        if (bookResponseImage != null)
        {
            bookResponseImage.DOColor(new Color(0, 0, 0, 1f), duration);
            bookResponseImageShadow.DOColor(new Color(1, 1, 1, 1f), duration);
        }
    }

    public void MoveBookOut()
    {
        float duration = Vector3.Distance(bookStartPosition, bookEndPosition) / MoveSpeed;

        bookTitleTextbox.text = "";

        book.transform.DOKill();
        book.transform.DOLocalMove(bookStartPosition, duration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                vocalAudioSource.Stop();
                PlayerController.currentState = PlayerController.PlayerState.BROWSING;
            });

        DOTween.To(() => bookBackgroundMaterial.color,
                   x => bookBackgroundMaterial.color = x,
                   new Color(0, 0, 0, 0f), duration);

        bookSpotlight.DOIntensity(0f, duration);

        if (bookResponseImage != null)
        {
            bookResponseImage.DOColor(new Color(0, 0, 0, 0f), duration);
            bookResponseImageShadow.DOColor(new Color(1, 1, 1, 0f), duration);
        }
    }
}
