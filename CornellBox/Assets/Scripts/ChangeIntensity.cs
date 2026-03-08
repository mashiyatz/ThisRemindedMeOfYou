using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeIntensity : MonoBehaviour
{
    private Light pointLight;
    private bool addIntensity;
    // Doesn't work if the lighting is baked.


    void Start()
    {
        pointLight = this.GetComponent<Light>();
        pointLight.intensity = 0.6f;
        addIntensity = true;
    }

    void Update()
    {
        if (addIntensity)
        {
            pointLight.intensity += 0.1f * Time.deltaTime;
            if (pointLight.intensity >= 1.4f) addIntensity = false;
        }
        else {
            pointLight.intensity -= 0.1f * Time.deltaTime;
            if (pointLight.intensity <= 0.6f) addIntensity = true;
        }
    }
}
