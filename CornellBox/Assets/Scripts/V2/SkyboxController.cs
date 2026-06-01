using DG.Tweening;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    public Material[] phases;
    public float timeBetweenPhases;
    private float timer = 0;
    private int index = 0;

    void Start()
    {
        RenderSettings.skybox = phases[0];
    }

    void Update()
    {
        timer += Time.deltaTime;
        RenderSettings.skybox.Lerp(phases[index], phases[(index + 1 >= phases.Length) ? 0: index + 1], timer / timeBetweenPhases);
        if (timer >= timeBetweenPhases)
        {
            index = Mathf.Min(index + 1, phases.Length);
            if (index >= phases.Length) index = 0;
            timer = 0;
        }
    }
}
