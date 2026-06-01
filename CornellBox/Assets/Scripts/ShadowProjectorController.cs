using UnityEngine;

/// Attach to the Directional Light. Auto-finds any Renderer on the
/// "KioskShadow" layer, renders it as a black silhouette into a
/// RenderTexture, and projects that shadow onto the scene via a Projector
/// so transparent surfaces receive it.
[ExecuteAlways]
public class ShadowProjectorController : MonoBehaviour
{
    public float projectorSize = 8f;
    public float farClip = 30f;
    public int resolution = 512;

    Camera _cam;
    Projector _proj;
    RenderTexture _rt;
    Material _mat;
    GameObject _camGO;
    Renderer _caster;

    void OnEnable() => Rebuild();
    void OnDisable() => Cleanup();

    void Rebuild()
    {
        if (!gameObject.activeInHierarchy) return;

        // Auto-find the first renderer on the KioskShadow layer
        _caster = null;
        int layer = LayerMask.NameToLayer("KioskShadow");
        if (layer >= 0)
        {
            foreach (var r in FindObjectsByType<Renderer>())
                if (r.gameObject.layer == layer) { _caster = r; break; }
        }

        _rt = new RenderTexture(resolution, resolution, 16)
            { hideFlags = HideFlags.HideAndDontSave };

        _camGO = new GameObject("__ShadowCam")
            { hideFlags = HideFlags.HideAndDontSave };
        _camGO.transform.SetParent(transform, false);

        _cam = _camGO.AddComponent<Camera>();
        _cam.orthographic     = true;
        _cam.orthographicSize = projectorSize;
        _cam.nearClipPlane    = 0.01f;
        _cam.farClipPlane     = farClip;
        _cam.clearFlags       = CameraClearFlags.SolidColor;
        _cam.backgroundColor  = Color.white;
        _cam.cullingMask      = _caster != null ? (1 << _caster.gameObject.layer) : 0;
        _cam.targetTexture    = _rt;
        _cam.enabled          = false;

        // Reuse existing Projector if present (avoids DestroyImmediate race)
        _proj = gameObject.GetComponent<Projector>()
              ?? gameObject.AddComponent<Projector>();
        _proj.orthographic     = true;
        _proj.orthographicSize = projectorSize;
        _proj.nearClipPlane    = 0.01f;
        _proj.farClipPlane     = farClip;
        _proj.aspectRatio      = 1f;
        _proj.ignoreLayers     = _caster != null ? (1 << _caster.gameObject.layer) : 0;

        _mat = new Material(Shader.Find("Custom/ShadowProjector"))
            { hideFlags = HideFlags.HideAndDontSave };
        _mat.SetTexture("_ShadowTex", _rt);
        _proj.material = _mat;
    }

    void Update()
    {
        var sil = Shader.Find("Hidden/ShadowSilhouette");
        if (_cam != null && sil != null)
            _cam.RenderWithShader(sil, "");
    }

    void Cleanup()
    {
        if (_camGO != null) DestroyImmediate(_camGO);
        if (_rt   != null) { _rt.Release(); DestroyImmediate(_rt); }
        if (_mat  != null) DestroyImmediate(_mat);
        var p = GetComponent<Projector>();
        if (p != null) DestroyImmediate(p);
        _cam = null; _proj = null; _rt = null; _mat = null; _camGO = null; _caster = null;
    }
}
