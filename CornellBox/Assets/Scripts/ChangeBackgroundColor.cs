using UnityEngine;
using DG.Tweening;

public class ChangeBackgroundColor : MonoBehaviour
{
    private Camera cam;
    public Material windowMaterial;
    public Color lightColor;
    public Color darkColor;
    public float inDuration;
    public float outDuration;

    void Start()
    {
        cam = Camera.main;
        cam.backgroundColor = darkColor;
        windowMaterial.color = darkColor;

        Sequence cycle = DOTween.Sequence();
        cycle.Append(DOTween.To(
            () => cam.backgroundColor,
            x => { cam.backgroundColor = x; windowMaterial.color = x; },
            lightColor, inDuration));
        cycle.Append(DOTween.To(
            () => cam.backgroundColor,
            x => { cam.backgroundColor = x; windowMaterial.color = x; },
            darkColor, outDuration));
        cycle.SetLoops(-1);
    }
}
