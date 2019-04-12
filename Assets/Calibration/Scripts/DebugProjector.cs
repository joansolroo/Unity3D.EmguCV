using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugProjector : MonoBehaviour
{

    Camera _camera;

    int count;
    Vector2[] uv;
    Vector3[] xyz;


    [SerializeField] [Range(2, 20)] int GridSize = 4;
    float delta;

    // Use this for initialization
    void Start()
    {
        _camera = GetComponent<Camera>();

    }

    private void OnValidate()
    {
        Start();
    }
    // Update is called once per frame

    private void OnDrawGizmos()
    {
        if (this.enabled)
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
            {
                SampleScene();
            }

            for (int current = 0; current < count; ++current)
            {
                try
                {
                    Gizmos.color = new Color(uv[current].x, uv[current].y, 0);
                    Gizmos.DrawWireSphere(xyz[current], 0.1f);

                    Gizmos.color = new Color(uv[current].x, uv[current].y, 0, 0.5f);
                    Gizmos.DrawLine(transform.position, xyz[current]);
                }
                catch (Exception)
                {
                    Debug.Log("" + current + "/" + count);
                }
            }
        }
    }

    private void SampleScene()
    {
        if (transform.hasChanged)
        {
            float delta = 1.0f / GridSize;
            count = (GridSize + 1) * (GridSize + 1);
            uv = new Vector2[count];
            xyz = new Vector3[count];

            int current = 0;
            for (float u = 0; u < 1 + delta; u += delta)
            {
                for (float v = 0; v < 1 + delta; v += delta)
                {
                    Ray ray = _camera.ViewportPointToRay(new Vector3(u, v, 1));
                    RaycastHit hit;
                    Physics.Raycast(ray, out hit);

                    uv[current] = new Vector2(u, v);
                    xyz[current] = hit.point;
                    ++current;
                }
            }
        }
    }

}
