using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Projector))]
[RequireComponent(typeof(Camera))]
public class ProjectorReproject : MonoBehaviour
{
    Projector _projector;
    Camera _camera;

    [SerializeField] Texture debugImage;
    [SerializeField] Camera ProjectorView;

    [SerializeField] RenderTexture colorProjection;
    [SerializeField] RenderTexture depthProjection;
    // Use this for initialization
    void Start()
    {
        _projector = GetComponent<Projector>();
        UpdateProjectorFromCamera();
        if (ProjectorView != null)
        {
            int width = ProjectorView.pixelWidth;
            int height = ProjectorView.pixelHeight;
            colorProjection = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
            depthProjection = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);
            ProjectorView.SetTargetBuffers(colorProjection.colorBuffer, depthProjection.depthBuffer);
        }
    }

    void LateUpdate()
    {
        UpdateProjectorFromCamera();
        //Sample();
    }
    private void OnDrawGizmos()
    {
        UpdateProjectorFromCamera();
    }
    public Matrix4x4 cameraMVP;

    void UpdateProjectorFromCamera()
    {
        if (_camera == null) _camera = GetComponent<Camera>();
        if (_projector == null) _projector = GetComponent<Projector>();
        if (_projector != null)
        {
            _projector.aspectRatio = _camera.aspect;
            _projector.nearClipPlane = _camera.nearClipPlane;
            _projector.farClipPlane = _camera.farClipPlane;
            _projector.fieldOfView = _camera.fieldOfView;

            cameraMVP = _camera.projectionMatrix * _camera.worldToCameraMatrix;
            _projector.material.SetMatrix("custom_Projector", cameraMVP);

            if (ProjectorView != null)
            {/*
                int width = ProjectorView.pixelWidth;
                int height = ProjectorView.pixelHeight;
                RenderTexture colorProjection = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
                RenderTexture depthProjection = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);*/

                if (debugImage != null)
                {
                    _projector.material.SetTexture("_Cookie", debugImage);
                }
                else
                {
                    _projector.material.SetTexture("_Cookie", colorProjection);
                }
                _projector.material.SetTexture("_Depth", depthProjection);

                _projector.material.SetFloat("_DepthBias", ProjectorView.nearClipPlane / ProjectorView.farClipPlane / 4);
            }
            else
            {
                _projector.material.SetTexture("_Cookie", debugImage);
            }
        }
    }

}
