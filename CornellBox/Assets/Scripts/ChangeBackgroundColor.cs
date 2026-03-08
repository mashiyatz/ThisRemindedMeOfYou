using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeBackgroundColor : MonoBehaviour
{
    private Camera cam;
    public Material windowMaterial;
    public Color lightColor;
    public Color darkColor;
    public float inDuration;
    public float outDuration;

    private bool lightUp;
    [SerializeField] private float counter;

    void Start()
    {
        cam = Camera.main;
        cam.backgroundColor = darkColor;
        windowMaterial.color = darkColor;
        lightUp = true;
        counter = 0;
    }

    void Update()
    {
        if (lightUp) {
            counter += Time.deltaTime / inDuration;
            if (counter >= 1f) lightUp = false;
        }
        else {
            counter -= Time.deltaTime / outDuration;
            if (counter <= 0f) lightUp = true;
        }

        Mathf.Clamp(counter, 0, 1);
        cam.backgroundColor = Color.Lerp(darkColor, lightColor, counter);
        windowMaterial.color = Color.Lerp(darkColor, lightColor, counter);

    }
}
