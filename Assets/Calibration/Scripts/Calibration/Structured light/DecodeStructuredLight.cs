using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecodeStructuredLight : MonoBehaviour {

    [Header("Links Setup")]
    [SerializeField] Camera _camera;
    [SerializeField] Shader s;
    [SerializeField] Material screenShader;

    [SerializeField] RenderTexture renderTexture2;
    [SerializeField] RenderTexture renderTexture1;
    [SerializeField] EncodeStructuredLight encoder;

    private void Start()
    {
        Check();
    }

    void Check()
    {
        if (screenShader == null)
        {
            screenShader = new Material(s);
        }
        Reset();

    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, screenShader);
    }

    void FlipBuffer()
    {
        RenderTexture tmp = renderTexture1;
        renderTexture1 = renderTexture2;
        renderTexture2 = tmp;
    }
    private void Reset()
    {
        if (renderTexture1 == null || renderTexture1.width != _camera.pixelWidth || renderTexture1.height != _camera.pixelHeight)
        {
            renderTexture1 = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            renderTexture1.wrapMode = TextureWrapMode.Clamp;
        }
        if (renderTexture2 == null || renderTexture2.width != _camera.pixelWidth || renderTexture2.height != _camera.pixelHeight)
        {
            renderTexture2 = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            renderTexture2.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
