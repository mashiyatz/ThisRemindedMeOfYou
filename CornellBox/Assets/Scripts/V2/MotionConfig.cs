using UnityEngine;

/// <summary>
/// Global "reduced motion" flag (e-ink / kiosk mode).
/// WebGL: reads window.__reducedMotion, which the WebGL template computes in
/// its head (?motion=off|on param, else prefers-reduced-motion) before the
/// Unity loader runs — so this is safe to call synchronously from any Start().
/// Editor/standalone: reads optional "reducedMotion" from StreamingAssets/room_config.json.
/// </summary>
public static class MotionConfig
{
    private static bool _resolved;
    private static bool _reduced;

    public static bool Reduced
    {
        get
        {
            if (!_resolved)
            {
                _reduced = Resolve();
                _resolved = true;
            }
            return _reduced;
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int JS_GetReducedMotion();

    private static bool Resolve()
    {
        try { return JS_GetReducedMotion() != 0; }
        catch { return false; }
    }
#else
    private static bool Resolve()
    {
        try
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "room_config.json");
            var cfg = JsonUtility.FromJson<MotionConfigData>(System.IO.File.ReadAllText(path));
            return cfg != null && cfg.reducedMotion;
        }
        catch { return false; }
    }

    [System.Serializable]
    private class MotionConfigData
    {
        public bool reducedMotion;
    }
#endif
}
