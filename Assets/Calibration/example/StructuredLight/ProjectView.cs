using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Projector))]
[RequireComponent(typeof(Camera))]
public class ProjectView : MonoBehaviour {

    Camera _camera;
    Projector _projector;

    [SerializeField] Shader s;
    [SerializeField] Material screenShader;
    [SerializeField] RenderTexture renderTexture;

    [SerializeField] [Range(1, 64)] int GridX = 1;
    [SerializeField] [Range(1, 64)] int GridY = 1;
    [SerializeField] [Range(0, 1)] float border =0.001f;
    // Use this for initialization


    void Start () {
        OnValidate();
    }

    // Update is called once per frame
    [ExecuteInEditMode]
    void Update () {
        OnValidate();
        screenShader.SetFloat("_GridX", GridX);
        screenShader.SetFloat("_GridY", GridY);
    }

    void OnValidate()
    {
        _camera = GetComponent<Camera>();
        _projector = GetComponent<Projector>();
        _projector.aspectRatio = _camera.aspect;
        _projector.nearClipPlane = _camera.nearClipPlane;
        _projector.farClipPlane = _camera.farClipPlane;
        _projector.fieldOfView = _camera.fieldOfView;

        screenShader = new Material(s);
        screenShader.SetFloat("_GridX", GridX);
        screenShader.SetFloat("_GridY", GridY);
        screenShader.SetFloat("_Border", border);

        if (renderTexture == null || renderTexture.width != _camera.pixelWidth || renderTexture.height != _camera.pixelHeight)
        {
            renderTexture = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, RenderTextureFormat.ARGB32);
        }
        _projector.material.SetTexture("_ShadowTex", renderTexture);


    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (renderTexture != null)
        {
            Graphics.Blit(source, renderTexture, screenShader);
        }
        Graphics.Blit(source, destination, screenShader);
    }
}
