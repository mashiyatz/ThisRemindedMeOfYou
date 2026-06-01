using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Builds a UV-atlas-compatible Texture2D from a plain book cover image.
///
/// UV atlas layout (from inspecting the BookRecTextures):
///   Top ~38%  — page edges (horizontal stripes)   UV y ≈ 0.62–1.0
///   Bottom ~60% — cover area                       UV y ≈ 0.02–0.61
///     LEFT half  = back cover
///     CENTER     = spine (thin strip)
///     RIGHT half = front cover artwork
///
/// Strategy:
///   1. Find the largest UV island (vertex-index connectivity) — this is the
///      combined front+back+spine region; all page islands are smaller.
///   2. Split that island at its X midpoint → right half = front cover.
///   3. Fill the atlas with cream (pages), then fill the combined region with
///      a color sampled from the cover's left edge (spine/back color), then
///      blit the cover image into the right-half (front cover) sub-region.
/// </summary>
public static class BookCoverTextureComposer
{
    // Neutral cream color for the page-edge regions.
    private static readonly Color PageColor = new Color(0.85f, 0.82f, 0.75f);

    // ── Public API ───────────────────────────────────────────────────────────

    private static readonly Color32[] FallbackPalette =
    {
        new Color32(26,  41,  64, 255),
        new Color32(44,  64,  56, 255),
        new Color32(88,  30,  30, 255),
        new Color32(42,  46,  66, 255),
        new Color32(58,  42,  30, 255),
        new Color32(26,  27,  44, 255),
        new Color32(58,  38,  64, 255),
    };

    /// <summary>Deterministic solid-color cover from title hash, at 256×400.</summary>
    public static Texture2D GenerateSolidCover(string title)
    {
        int hash = 0;
        foreach (char c in title) hash = (hash * 31 + c) & 0xFFFF;
        var col = FallbackPalette[Mathf.Abs(hash) % FallbackPalette.Length];

        var tex = new Texture2D(256, 400, TextureFormat.RGB24, false);
        var pixels = new Color32[256 * 400];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Solid-color cover with title and author text composited over it.
    /// Falls back to plain solid cover if TMP font assets are not found.
    /// </summary>
    public static Texture2D GenerateSolidCoverWithText(string title, string author)
    {
        Texture2D cover = GenerateSolidCover(title);

        var titleFont  = Resources.Load<TMP_FontAsset>("Fonts/IMFellEnglish-ItalicSDF");
        var authorFont = Resources.Load<TMP_FontAsset>("Fonts/Lora-RegularSDF");
        if (titleFont == null || authorFont == null)
        {
            Debug.LogWarning("[BookCoverTextureComposer] TMP font assets not found — skipping text overlay.");
            return cover;
        }

        int W = cover.width, H = cover.height;

        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        // World-space canvas: 1 world unit = 1 pixel at this camera size
        var canvasGO = new GameObject("_BkTextCanvas") { layer = 31 };
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRt = canvasGO.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(W, H);
        canvasRt.localPosition = Vector3.zero;

        // Title
        var titleGO = new GameObject("_title") { layer = 31 };
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.font = titleFont;
        titleTMP.text = title;
        titleTMP.fontSize = 24f;
        titleTMP.color = new Color(1f, 0.97f, 0.9f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.textWrappingMode = TextWrappingModes.Normal;
        var titleRect = titleTMP.rectTransform;
        titleRect.anchorMin = new Vector2(0.1f, 0.45f);
        titleRect.anchorMax = new Vector2(0.9f, 0.80f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        // Author
        var authorGO = new GameObject("_author") { layer = 31 };
        authorGO.transform.SetParent(canvasGO.transform, false);
        var authorTMP = authorGO.AddComponent<TextMeshProUGUI>();
        authorTMP.font = authorFont;
        authorTMP.text = author.ToUpper();
        authorTMP.fontSize = 11f;
        authorTMP.color = new Color(1f, 0.97f, 0.9f, 0.72f);
        authorTMP.alignment = TextAlignmentOptions.Center;
        authorTMP.characterSpacing = 8f;
        var authorRect = authorTMP.rectTransform;
        authorRect.anchorMin = new Vector2(0.1f, 0.35f);
        authorRect.anchorMax = new Vector2(0.9f, 0.48f);
        authorRect.offsetMin = authorRect.offsetMax = Vector2.zero;

        // Off-screen orthographic camera
        var camGO = new GameObject("_BkTextCam");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = H * 0.5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.cullingMask = 1 << 31;
        cam.targetTexture = rt;

        cam.Render();

        // Alpha-composite text pixels over solid cover
        RenderTexture.active = rt;
        var textTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        textTex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        textTex.Apply();
        RenderTexture.active = null;

        Color[] basePixels = cover.GetPixels();
        Color[] textPixels = textTex.GetPixels();
        for (int i = 0; i < basePixels.Length; i++)
        {
            float a = textPixels[i].a;
            basePixels[i] = Color.Lerp(basePixels[i], textPixels[i], a);
        }
        cover.SetPixels(basePixels);
        cover.Apply();

        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(canvasGO);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(textTex);

        return cover;
    }

    /// <summary>One-shot: detect UV layout and build the atlas.</summary>
    public static Texture2D BuildAtlas(Texture2D cover, Mesh mesh, int atlasSize = 1024)
    {
        var islands = FindUVIslands(mesh);

        if (islands.Count == 0)
        {
            Debug.LogWarning("[BookCoverTextureComposer] No UV islands found — applying cover directly.");
            return cover;
        }

        // Largest island = combined front+back+spine cover region.
        Rect combinedRect = islands[0].uvRect;

        // Right half of the combined region = front cover.
        float splitX = combinedRect.x + combinedRect.width * 0.55f;
        Rect frontRect = Rect.MinMaxRect(splitX, combinedRect.yMin,
                                          combinedRect.xMax, combinedRect.yMax);

        Color fillColor = SampleSpineColor(cover);
        return Compose(cover, combinedRect, frontRect, fillColor, atlasSize);
    }

    /// <summary>
    /// Returns all UV islands sorted by UV area descending.
    /// Uses vertex-index connectivity: two triangles sharing a vertex index
    /// are in the same island. This is intentionally simple — it correctly
    /// groups the continuous outer cover (front+back+spine) as one island and
    /// leaves the page-edge strips as smaller separate islands.
    /// </summary>
    public static List<(List<int> triIndices, Rect uvRect, float area)> FindUVIslands(Mesh mesh)
    {
        Vector2[] uvs  = mesh.uv;
        int[]     tris = mesh.triangles;
        int triCount   = tris.Length / 3;

        // Build edge → list of triangles that share that edge.
        var edgeToTris = new Dictionary<long, List<int>>(triCount * 3);
        for (int t = 0; t < triCount; t++)
        {
            AddEdge(edgeToTris, tris[t * 3],     tris[t * 3 + 1], t);
            AddEdge(edgeToTris, tris[t * 3 + 1], tris[t * 3 + 2], t);
            AddEdge(edgeToTris, tris[t * 3 + 2], tris[t * 3],     t);
        }

        var visited = new bool[triCount];
        var result  = new List<(List<int>, Rect, float)>();

        for (int start = 0; start < triCount; start++)
        {
            if (visited[start]) continue;

            var component = new List<int>();
            var queue     = new Queue<int>();
            visited[start] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int t = queue.Dequeue();
                component.Add(t);

                int i0 = tris[t * 3], i1 = tris[t * 3 + 1], i2 = tris[t * 3 + 2];
                EnqueueNeighbours(edgeToTris, visited, queue, i0, i1);
                EnqueueNeighbours(edgeToTris, visited, queue, i1, i2);
                EnqueueNeighbours(edgeToTris, visited, queue, i2, i0);
            }

            float totalArea = 0f;
            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;

            foreach (int t in component)
            {
                Vector2 a = uvs[tris[t * 3]];
                Vector2 b = uvs[tris[t * 3 + 1]];
                Vector2 c = uvs[tris[t * 3 + 2]];

                totalArea += Mathf.Abs((b.x - a.x) * (c.y - a.y) -
                                       (c.x - a.x) * (b.y - a.y)) * 0.5f;

                minU = Mathf.Min(minU, a.x, b.x, c.x);
                maxU = Mathf.Max(maxU, a.x, b.x, c.x);
                minV = Mathf.Min(minV, a.y, b.y, c.y);
                maxV = Mathf.Max(maxV, a.y, b.y, c.y);
            }

            result.Add((component, Rect.MinMaxRect(minU, minV, maxU, maxV), totalArea));
        }

        result.Sort((a, b) => b.Item3.CompareTo(a.Item3));
        return result;
    }

    /// <summary>
    /// Composites the cover image and fill color into a blank atlas.
    /// </summary>
    public static Texture2D Compose(Texture2D cover,
                                    Rect combinedRect, Rect frontRect,
                                    Color fillColor, int atlasSize = 1024)
    {
        var atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);

        // 1. Fill entire atlas with cream (page-edge color).
        var fill = new Color[atlasSize * atlasSize];
        for (int i = 0; i < fill.Length; i++) fill[i] = PageColor;
        atlas.SetPixels(fill);

        // 2. Fill combined cover region (front+back+spine) with the sampled color.
        int cx = Mathf.RoundToInt(combinedRect.x * atlasSize);
        int cy = Mathf.RoundToInt(combinedRect.y * atlasSize);
        int cw = Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(combinedRect.width  * atlasSize)), atlasSize - cx);
        int ch = Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(combinedRect.height * atlasSize)), atlasSize - cy);
        var coverFill = new Color[cw * ch];
        for (int i = 0; i < coverFill.Length; i++) coverFill[i] = fillColor;
        atlas.SetPixels(cx, cy, cw, ch, coverFill);

        // 3. Blit resized cover image into the front cover sub-region.
        int px = Mathf.RoundToInt(frontRect.x * atlasSize);
        int py = Mathf.RoundToInt(frontRect.y * atlasSize);
        int pw = Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(frontRect.width  * atlasSize)), atlasSize - px);
        int ph = Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(frontRect.height * atlasSize)), atlasSize - py);
        atlas.SetPixels(px, py, pw, ph, ResizeBilinear(cover, pw, ph));

        atlas.Apply();
        return atlas;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Averages the leftmost 5% of the cover's columns to approximate spine color.
    /// </summary>
    private static Color SampleSpineColor(Texture2D cover)
    {
        int sampleWidth = Mathf.Max(1, cover.width / 20);
        Color sum = Color.clear;
        int count = 0;
        for (int y = 0; y < cover.height; y++)
        for (int x = 0; x < sampleWidth; x++)
        {
            sum += cover.GetPixel(x, y);
            count++;
        }
        return count > 0 ? sum / count : Color.gray;
    }

    private static Color[] ResizeBilinear(Texture2D src, int newWidth, int newHeight)
    {
        var result = new Color[newWidth * newHeight];
        for (int y = 0; y < newHeight; y++)
        for (int x = 0; x < newWidth; x++)
        {
            float u = newWidth  > 1 ? (float)x / (newWidth  - 1) : 0f;
            float v = newHeight > 1 ? (float)y / (newHeight - 1) : 0f;
            result[y * newWidth + x] = src.GetPixelBilinear(u, v);
        }
        return result;
    }

    private static long EdgeKey(int a, int b) =>
        a < b ? ((long)a << 32) | (uint)b
              : ((long)b << 32) | (uint)a;

    private static void AddEdge(Dictionary<long, List<int>> map, int a, int b, int triIndex)
    {
        long key = EdgeKey(a, b);
        if (!map.TryGetValue(key, out var list))
        {
            list = new List<int>(2);
            map[key] = list;
        }
        list.Add(triIndex);
    }

    private static void EnqueueNeighbours(Dictionary<long, List<int>> edgeToTris,
                                           bool[] visited, Queue<int> queue, int a, int b)
    {
        long key = EdgeKey(a, b);
        if (!edgeToTris.TryGetValue(key, out var list)) return;
        foreach (int t in list)
        {
            if (!visited[t])
            {
                visited[t] = true;
                queue.Enqueue(t);
            }
        }
    }
}
