using UnityEngine;
using DG.Tweening;

public class ChangeIntensity : MonoBehaviour
{
    // Note: does not work with baked lighting.
    public float minIntensity = 0.6f;
    public float maxIntensity = 1.4f;
    public float pulseDuration = 8f;

    void Start()
    {
        Light pointLight = GetComponent<Light>();
        pointLight.intensity = minIntensity;
        pointLight.DOIntensity(maxIntensity, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}
