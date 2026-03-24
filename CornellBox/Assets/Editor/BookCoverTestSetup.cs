using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public static class BookCoverTestSetup
{
    [MenuItem("Tools/Setup BookCoverTest Scene")]
    public static void Setup()
    {
        // Remove existing UI if present
        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);
        var existingES = GameObject.Find("EventSystem");
        if (existingES != null) Object.DestroyImmediate(existingES);
        var existingCtrl = GameObject.Find("BookCoverFetcher");
        if (existingCtrl != null) Object.DestroyImmediate(existingCtrl);

        // ── EventSystem ──────────────────────────────────────────────
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ── Canvas ───────────────────────────────────────────────────
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Background ───────────────────────────────────────────────
        var bg = CreateImage(canvasGo, "Background", new Color(0.10f, 0.10f, 0.12f, 1f));
        SetRect(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── Left panel ───────────────────────────────────────────────
        var panel = new GameObject("ControlPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0.38f, 1f);
        panelRect.offsetMin = new Vector2(40f, 40f);
        panelRect.offsetMax = new Vector2(-20f, -40f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.15f, 0.18f, 1f);
        var panelVlg = panel.AddComponent<VerticalLayoutGroup>();
        panelVlg.padding = new RectOffset(30, 30, 40, 40);
        panelVlg.spacing = 18f;
        panelVlg.childForceExpandWidth = true;
        panelVlg.childForceExpandHeight = false;
        panelVlg.childControlWidth = true;
        panelVlg.childControlHeight = true;

        // ── Title ────────────────────────────────────────────────────
        CreateLabel(panel, "TitleLabel", "Book Title");
        var titleInput = CreateInputField(panel, "TitleInput", "e.g. The Road");

        // ── Author ───────────────────────────────────────────────────
        CreateLabel(panel, "AuthorLabel", "Author");
        var authorInput = CreateInputField(panel, "AuthorInput", "e.g. Cormac McCarthy");

        // ── Fetch button ─────────────────────────────────────────────
        var fetchBtn = CreateButton(panel, "FetchButton", "Fetch Cover");

        // ── Status text ──────────────────────────────────────────────
        var statusText = CreateStatusText(panel, "StatusText");

        // ── Cover image (right side) ─────────────────────────────────
        var coverGo = new GameObject("CoverImage");
        coverGo.transform.SetParent(canvasGo.transform, false);
        var coverRect = coverGo.AddComponent<RectTransform>();
        coverRect.anchorMin = new Vector2(0.40f, 0.05f);
        coverRect.anchorMax = new Vector2(0.88f, 0.95f);
        coverRect.offsetMin = Vector2.zero;
        coverRect.offsetMax = Vector2.zero;
        var rawImg = coverGo.AddComponent<RawImage>();
        rawImg.color = new Color(1f, 1f, 1f, 0.08f); // faint placeholder tint
        coverGo.SetActive(false); // hidden until a cover loads

        // ── Title text (top right) ────────────────────────────────────
        var appTitle = new GameObject("AppTitle");
        appTitle.transform.SetParent(canvasGo.transform, false);
        var appTitleRect = appTitle.AddComponent<RectTransform>();
        appTitleRect.anchorMin = new Vector2(0.38f, 0.88f);
        appTitleRect.anchorMax = new Vector2(1f, 1f);
        appTitleRect.offsetMin = new Vector2(20f, 0f);
        appTitleRect.offsetMax = new Vector2(-40f, -20f);
        var appTitleTmp = appTitle.AddComponent<TextMeshProUGUI>();
        appTitleTmp.text = "Book Cover Viewer";
        appTitleTmp.fontSize = 36f;
        appTitleTmp.alignment = TextAlignmentOptions.TopLeft;
        appTitleTmp.color = new Color(0.9f, 0.85f, 0.75f, 1f);
        appTitleTmp.fontStyle = FontStyles.Bold;

        // ── Controller ───────────────────────────────────────────────
        var ctrlGo = new GameObject("BookCoverFetcher");
        var fetcher = ctrlGo.AddComponent<BookCoverFetcher>();
        fetcher.titleInput  = titleInput;
        fetcher.authorInput = authorInput;
        fetcher.fetchButton = fetchBtn;
        fetcher.coverImage  = rawImg;
        fetcher.statusText  = statusText;
        fetcher.imageSize   = "large";

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[BookCoverTestSetup] Scene wired successfully.");
        Selection.activeGameObject = ctrlGo;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
                        Vector2 offsetMin, Vector2 offsetMax)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.offsetMin = offsetMin; r.offsetMax = offsetMax;
    }

    static GameObject CreateImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static void CreateLabel(GameObject parent, string name, string labelText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 20f;
        tmp.color = new Color(0.75f, 0.75f, 0.80f, 1f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;
        le.flexibleWidth = 1f;
    }

    static TMP_InputField CreateInputField(GameObject parent, string name, string placeholder)
    {
        // Root
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.26f, 1f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 52f;
        le.flexibleWidth = 1f;

        // Text Area
        var area = new GameObject("Text Area");
        area.transform.SetParent(go.transform, false);
        var areaRect = area.AddComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero; areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = new Vector2(10f, 4f); areaRect.offsetMax = new Vector2(-10f, -4f);
        area.AddComponent<RectMask2D>();

        // Placeholder
        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(area.transform, false);
        var phRect = phGo.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;
        var phTmp = phGo.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 18f;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        phTmp.fontStyle = FontStyles.Italic;

        // Text
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(area.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
        var txtTmp = txtGo.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize = 18f;
        txtTmp.color = Color.white;

        // TMP_InputField
        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = areaRect;
        inputField.textComponent = txtTmp;
        inputField.placeholder = phTmp;
        inputField.fontAsset = txtTmp.font;

        return inputField;
    }

    static Button CreateButton(GameObject parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.30f, 0.50f, 0.85f, 1f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
        le.flexibleWidth = 1f;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.40f, 0.60f, 0.95f, 1f);
        cb.pressedColor     = new Color(0.20f, 0.40f, 0.75f, 1f);
        btn.colors = cb;
        btn.targetGraphic = img;

        // Label
        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(go.transform, false);
        var lblRect = lblGo.AddComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero; lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = Vector2.zero; lblRect.offsetMax = Vector2.zero;
        var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 22f;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color = Color.white;
        lblTmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    static TextMeshProUGUI CreateStatusText(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = 17f;
        tmp.color = new Color(0.85f, 0.75f, 0.35f, 1f);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 60f;
        le.flexibleWidth = 1f;
        return tmp;
    }
}
