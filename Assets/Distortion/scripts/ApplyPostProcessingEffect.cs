using System;
using UnityEngine;

public class ApplyPostProcessingEffect: MonoBehaviour
{
    public Shader s;
    public RenderTexture renderTexture;
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Material m = new Material(s);
        
        if (renderTexture != null)
        {
            Graphics.Blit(source, renderTexture, m);
        }
        else
        {
            Graphics.Blit(source, destination, m);

        }
    }
}