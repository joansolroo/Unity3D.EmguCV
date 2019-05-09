using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckerBoard : MonoBehaviour
{

    [SerializeField]
    int width = 10;
    [SerializeField]
    int height = 7;

    [SerializeField]
    Vector3[] corners;

    public Vector3[] Corners
    {
        get
        {
            Vector3[] worldCorners = new Vector3[corners.Length];
            for (int c = 0; c < corners.Length; ++c) {
                worldCorners[c] =  this.transform.TransformPoint(corners[c]);
            }
            return worldCorners;
        }
    }
    private void Awake()
    {
        Generate();        
    }
    private void OnValidate()
    {
        Generate();
    }
    void Generate() { 
        int cornerCount = (width - 1) * (height - 1);
        if (corners == null || corners.Length != cornerCount)
        {
            corners = new Vector3[cornerCount];
        }
        int idx = 0;
        for (int x = 1; x < width; ++x)
        {
            for (int y = 1; y < height; ++y)
            {
                float u = ((float)x) / width;
                float v = ((float)y) / height;

                Vector3 corner = new Vector3(u - 0.5f, 0, v - 0.5f);
                corners[idx++] = corner;
            }
        }
    }

    private void OnDrawGizmos()
    {
        {
            Vector3 scale = new Vector3(1.0f / width, 0, 1.0f / height);
            int idx = 0;
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    float u = ((float)x + 0.5f) / width - 0.5f;
                    float v = ((float)y + 0.5f) / height - 0.5f;
                    Vector3 center = new Vector3(u, 0, v);

                    if ((idx++) % 2 == 0)
                    {
                        Gizmos.color = Color.black;
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                    }
                    GizmosDrawRectangle(this.transform, center, scale * 0.5f);
                    GizmosDrawRectangle(this.transform, center, scale * 0.99f);
                    GizmosDrawRectangle(this.transform, center, scale * 0.9f, true);

                    Gizmos.color = Color.red;
                    GizmosDrawRectangle(this.transform, center, scale);
                }
            }
        }
        {
           /* int cornerCount = (width - 1) * (height - 1);
            if (corners == null || corners.Length != cornerCount)
            {
                corners = new Vector3[cornerCount];
            }*/
            int idx = 0;
            for (int x = 1; x < width; ++x)
            {
                for (int y = 1; y < height; ++y)
                {
                    float u = ((float)x) / width;
                    float v = ((float)y) / height;

                    Vector3 corner = this.transform.TransformPoint(corners[idx++]);// this.transform.TransformPoint(new Vector3(u - 0.5f, 0, v - 0.5f));
                   // corners[idx++] = corner;
                    Gizmos.color = new Color(u, v, 0);
                    Gizmos.DrawSphere(corner, 0.1f);
                    Gizmos.DrawWireSphere(corner, 0.15f);
                }
            }
        }
        Gizmos.color = Color.red;
        GizmosDrawRectangle(transform, Vector3.zero, Vector3.one);
    }

    void GizmosDrawRectangle(Transform transform, Vector3 center, Vector3 scale, bool drawDiagonals = false)
    {
        Vector3[,] corners = new Vector3[2, 2];
        corners[0, 0] = this.transform.TransformPoint(new Vector3(-0.5f * scale.x, 0, -0.5f * scale.z) + center);
        corners[1, 0] = this.transform.TransformPoint(new Vector3(0.5f * scale.x, 0, -0.5f * scale.z) + center);
        corners[1, 1] = this.transform.TransformPoint(new Vector3(0.5f * scale.x, 0, 0.5f * scale.z) + center);
        corners[0, 1] = this.transform.TransformPoint(new Vector3(-0.5f * scale.x, 0, 0.5f * scale.z) + center);

        Gizmos.DrawLine(corners[0, 0], corners[0, 1]);
        Gizmos.DrawLine(corners[0, 0], corners[1, 0]);
        Gizmos.DrawLine(corners[1, 1], corners[1, 0]);
        Gizmos.DrawLine(corners[1, 1], corners[0, 1]);
        if (drawDiagonals)
        {
            Gizmos.DrawLine(corners[1, 1], corners[0, 0]);
            Gizmos.DrawLine(corners[1, 0], corners[0, 1]);
        }
    }
}

