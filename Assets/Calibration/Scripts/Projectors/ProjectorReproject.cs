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
    // Use this for initialization
    void Start()
    {
        _projector = GetComponent<Projector>();
        UpdateProjectorFromCamera();
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
            {
                _projector.material.SetTexture("_Cookie", ProjectorView.targetTexture);
            }
            else {
                _projector.material.SetTexture("_Cookie", debugImage);
            }
        }
    }

}
