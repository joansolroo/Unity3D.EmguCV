using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastCamera : MonoBehaviour
{
    [SerializeField] Camera otherCamera;
    [SerializeField] int size = 4;
    [SerializeField] float maxRange = 100;
    [SerializeField] float coverture = 0;
    [SerializeField] float lengthAvg = 0;
    [SerializeField] float lengthDev = 0;
    [SerializeField] int intersectionCount = 0;
    float delta;
    public int count = 0;
    Vector2[] uv;
    Vector3[] xyz;
    Ray[] rays;
    Camera _camera;

    [SerializeField] bool drawRays = false;
    [SerializeField] bool drawHits = true;
    [SerializeField] float hitRadius = 0.1f;
    [SerializeField] Color failColor = new Color(1, 1, 1, 0.25f);
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnGUI()
    {
        GUI.color = Color.red;
        GUI.Label(new Rect(0, 0, 100, 50), "Coverture:" + (coverture * 100).ToString("0.0") + "%");
        GUI.Label(new Rect(0, 30, 100, 50), "Length:" + (lengthAvg).ToString("0.0") + "cm");
        GUI.Label(new Rect(0, 60, 100, 50), "Deviation:" + (lengthDev).ToString("0.0") + "cm");
    }
    public bool compute = false;
    private void OnDrawGizmos()
    {

        
            Compute();
        

    }

    public int current = 0;
    public int centerRay = 0;
    void Compute()
    {
        Awake();
        float delta = 1.0f / size;
        int lastCount = count;
        count = (size + 1) * (size + 1);
        if (rays == null || rays.Length != lastCount)
        {
            uv = new Vector2[count];
            xyz = new Vector3[count];
            rays = new Ray[count];
        }
        int hits = 0;
        current = 0;
        Vector3 origin = this.transform.position;
        List<float> lengths = new List<float>();
        lengthAvg = 0;

        for (int x = 0; x <= size; ++x)
        {
            for (int y = 0;y <= size; ++y)
            {
                Vector2 uv = new Vector2(x, y) / size;
                Ray ray = _camera.ViewportPointToRay(new Vector3(uv.x,uv.y, 1));

               
                //uv[current] = new Vector2(u, v);
                //xyz[current] = hit.point;
                float length = 0;

                Gizmos.color = new Color(uv.x, uv.y, 0, 0.5f);
                if (x == (size) / 2 && y == (size) / 2)
                {
                    centerRay = current;
                    Gizmos.color = new Color(1, 1, 1, 1);
                }
                bool hit = CastRay(ray, 0, ref length);

                if (hit)
                {
                    ++hits;
                    lengths.Add(length);
                    lengthAvg += length;
                }

                ++current;
            }
        }
        coverture = ((float)hits) / count;
        lengthAvg = lengthAvg / hits;
        lengthDev = 0;
        foreach (float l in lengths)
        {
            lengthDev += Mathf.Abs(l - lengthAvg);
        }

        intersectionCount = 0;
        Vector3 centroidIntersection = Vector3.zero;
        Vector3 avgCentroid = Vector3.zero;
        for (int r1 = 0; r1 < rays.Length; ++r1)
        {
            Ray ray1 = rays[r1];
            if (ray1.origin.sqrMagnitude > 0)
            {
                for (int r2 = r1 + 1; r2 < rays.Length; ++r2)
                {
                    Ray ray2 = rays[r2];
                    if (ray2.origin.sqrMagnitude > 0)
                    {
                        Vector3 intersection;
                        if (Math3d.LineLineIntersection(out intersection, ray1.origin, ray1.direction, ray2.origin, ray2.direction))
                        {
                     //       Gizmos.DrawSphere(intersection, 1);
                            centroidIntersection += intersection;
                            ++intersectionCount;
                        }
                    }
                }
                avgCentroid += ray1.origin;
            }
        }
        if (compute && intersectionCount > 0)
        {
            centroidIntersection /= intersectionCount;
            if (otherCamera != null)
            {
                otherCamera.CopyFrom(_camera);
                otherCamera.transform.position = centroidIntersection;
                Debug.Log("" + centerRay + ": " + rays[centerRay].origin);
                otherCamera.transform.rotation = Quaternion.LookRotation(rays[centerRay].origin- centroidIntersection, Vector3.down);
            }
        }
        lengthDev /= hits;
    }

    bool CastRay(Ray ray, int currentBounces, ref float length)
    {
        bool result = false;
        if (currentBounces < 5)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Gizmos.color = Color.white;
                //  Gizmos.DrawLine(hit.point, hit.point + hit.normal);
                Color color = Gizmos.color;
                //color.a = color.a / 4;
                Gizmos.color = color;

                //Rr = Ri - 2 N(Ri.N)
                Vector3 Ri = (hit.point - ray.origin).normalized;
                Vector3 reflection = Ri - 2 * hit.normal * (Vector3.Dot(Ri, hit.normal));
                //   Gizmos.DrawLine(hit.point, hit.point + reflection);
                if (hit.transform.gameObject.GetComponent<ProjectionTarget>() != null)
                {
                    //if (drawHits)
                    {
                        Gizmos.DrawSphere(hit.point, hitRadius);
                    }
                    Gizmos.DrawLine(ray.origin, hit.point);
                    length += Vector3.Distance(ray.origin, hit.point);
                    Color currentColor = Gizmos.color;
                    Gizmos.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentColor.a * 0.25f);
                    Gizmos.DrawLine(hit.point, (ray.origin - hit.point).normalized * length + hit.point);
                    rays[current] = new Ray(hit.point, (ray.origin - hit.point).normalized);
                    result = true;
                }
                else if (hit.transform.gameObject.GetComponent<Mirror>() != null)
                {
                    length += Vector3.Distance(ray.origin, hit.point);
                    result = CastRay(new Ray(hit.point, reflection), ++currentBounces, ref length);

                    Gizmos.color = result ? Gizmos.color : failColor;
                    if (result || drawRays)
                    {
                        Gizmos.DrawLine(ray.origin, hit.point);
                    }
                    if (result || drawHits)
                    {
                        Gizmos.DrawSphere(hit.point, hitRadius);
                    }
                }
                else
                {
                    Gizmos.color = failColor;
                    if (drawRays)
                    {
                        Gizmos.DrawLine(ray.origin, hit.point);
                    }
                    if (drawHits)
                    {
                        Gizmos.DrawSphere(hit.point, hitRadius);
                    }
                    rays[current] = new Ray(Vector3.zero, Vector3.zero);
                }
            }
            else
            {
                if (drawRays)
                {
                    Gizmos.color = failColor;
                    Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * maxRange);
                }
            }
        }
        return result;
    }
}
