using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayTVAnimation : MonoBehaviour
{
    public Texture[] animationTextureList;
    public Renderer _tvScreenRenderer;
    private int fps = 4;
    
    void Start()
    {
        _tvScreenRenderer.material.SetTexture("_EmissionMap", animationTextureList[0]);
    }


    void Update()
    {
        int index = (int)(Time.time * fps);
        index %= animationTextureList.Length;
        _tvScreenRenderer.material.SetTexture("_EmissionMap", animationTextureList[index]);
    }
}
