using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LensDistortion : MonoBehaviour
{
    public Camera _camera;
    [Range(-0.0005f,0.0005f)]public float t1, t2;
    [Range(-5e-6f, 5e-6f)] public float k1;
    [Range(-5e-12f, 5e-12f)] public float k2;
    [Range(-5e-18f, 5e-18f)] public float k5;
    public Shader s;
    Material m;

    private void OnValidate()
    {

        s = Shader.Find("Effect/LensDistortion");
    }
    private void Start()
    {
        _camera = GetComponent<Camera>();
        m = new Material(s);
    }
    private void Update()
    {
        m.SetFloat("k1", k1);
        m.SetFloat("k2", k2);
        m.SetFloat("k3", t1);
        m.SetFloat("k4", t2);
        m.SetFloat("k5", k5);
        m.SetVector("resolution", new Vector2(_camera.pixelWidth, _camera.pixelHeight));
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, m);
    }
}
