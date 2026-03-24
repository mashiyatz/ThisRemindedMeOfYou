using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fetches a book cover image from bookcover.longitood.com by title + author.
///
/// Scene setup:
///   - Attach this component to any GameObject.
///   - Assign TitleInput, AuthorInput, FetchButton, CoverImage, and StatusText in the Inspector.
///   - CoverImage should be a UI RawImage.
/// </summary>
public class BookCoverFetcher : MonoBehaviour
{
    private const string ApiBase = "https://bookcover.longitood.com/bookcover";

    [Header("UI References")]
    public TMP_InputField titleInput;
    public TMP_InputField authorInput;
    public Button fetchButton;
    public RawImage coverImage;
    public TMP_Text statusText;

    [Header("Settings")]
    [Tooltip("small, medium, or large")]
    public string imageSize = "large";

    [Header("Callbacks")]
    public System.Action<Texture2D> OnCoverFetched;

    private void Start()
    {
        fetchButton.onClick.AddListener(OnFetchClicked);
        coverImage.gameObject.SetActive(false);
    }

    private void OnFetchClicked()
    {
        string title = titleInput.text.Trim();
        string author = authorInput.text.Trim();

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(author))
        {
            SetStatus("Please enter both a book title and author name.");
            return;
        }

        StartCoroutine(FetchCoverCoroutine(title, author));
    }

    private IEnumerator FetchCoverCoroutine(string title, string author)
    {
        fetchButton.interactable = false;
        coverImage.gameObject.SetActive(false);
        SetStatus("Looking up book cover...");

        string url = $"{ApiBase}?book_title={Uri.EscapeDataString(title)}&author_name={Uri.EscapeDataString(author)}&image_size={imageSize}";

        using (UnityWebRequest apiRequest = UnityWebRequest.Get(url))
        {
            yield return apiRequest.SendWebRequest();

            if (apiRequest.result != UnityWebRequest.Result.Success)
            {
                SetStatus($"Request failed: {apiRequest.error}");
                fetchButton.interactable = true;
                yield break;
            }

            string json = apiRequest.downloadHandler.text;

            string imageUrl = ParseImageUrl(json);
            if (string.IsNullOrEmpty(imageUrl))
            {
                SetStatus("Book not found.");
                fetchButton.interactable = true;
                yield break;
            }

            SetStatus("Downloading cover...");
            yield return StartCoroutine(DownloadImageCoroutine(imageUrl));
        }

        fetchButton.interactable = true;
    }

    private IEnumerator DownloadImageCoroutine(string imageUrl)
    {
        using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return imageRequest.SendWebRequest();

            if (imageRequest.result != UnityWebRequest.Result.Success)
            {
                SetStatus($"Image download failed: {imageRequest.error}");
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
            coverImage.texture = texture;
            coverImage.gameObject.SetActive(true);
            SetStatus(string.Empty);
            OnCoverFetched?.Invoke(texture);
        }
    }

    /// <summary>
    /// Parses {"url":"..."} without requiring a JSON library.
    /// </summary>
    private static string ParseImageUrl(string json)
    {
        // Response is simply: {"url":"https://..."}
        const string key = "\"url\":\"";
        int start = json.IndexOf(key, StringComparison.Ordinal);
        if (start < 0) return null;

        start += key.Length;
        int end = json.IndexOf('"', start);
        return end < 0 ? null : json.Substring(start, end - start);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
