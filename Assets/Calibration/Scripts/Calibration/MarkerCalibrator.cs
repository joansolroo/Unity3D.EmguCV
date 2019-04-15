using UnityEngine;



public class MarkerCalibrator : Calibrator
{

    [Header("KeyBinds")]

    [SerializeField] KeyCode resetKey = KeyCode.Escape;
    [SerializeField] KeyCode sampleKey = KeyCode.Space;
    [SerializeField] KeyCode computeKey = KeyCode.Return;

    [Header("Setup")]
    [SerializeField] bool update = false;
    [SerializeField] bool reset = false;
    [SerializeField] bool sample = false;
    [SerializeField] bool compute = false;

    [Header("Target properties (REQUIRED)")]
    [SerializeField] Marker target;
    [SerializeField] protected Vector2[] targetUV = new Vector2[0]; //fixed UV points to sample
    [SerializeField] bool fake = false;
    [SerializeField] float noise = 0.1f; //noise
   

    // Use this for initialization
    void Start()
    {
        SourceCamera = GetComponent<Camera>();

        Clear();

        if (update)
        {
            if (generateUVGrid)
            {
                targetUV = new Vector2[samplingWidth * samplingHeight];

                for (int x = 0; x < samplingWidth; ++x)
                {
                    for (int y = 0; y < samplingHeight; ++y)
                    {
                        int idx = (y) + (x) * samplingHeight;
                        targetUV[idx] = new Vector2(Mathf.Lerp(1 - MaxUVRadius, MaxUVRadius, (x) / (samplingWidth - 1f)), Mathf.Lerp(1 - MaxUVRadius, MaxUVRadius, (y) / (samplingHeight - 1f)));
                    }
                }
            }
        }
    }

    private void OnValidate()
    {
        Start();
    }
    // Update is called once per frame
    void LateUpdate()
    {
        if (fake)
        {
            int pass = xyz.Count / targetUV.Length;
           
            Vector2 __uv = currentUV();
            Vector3 fakeXYZ = SourceCamera.ViewportToWorldPoint(new Vector3(__uv.x, __uv.y, 40+10*pass)) + new Vector3(Random.Range(-noise, noise), Random.Range(-noise, noise), Random.Range(-noise, noise));
            target.transform.LookAt(SourceCamera.transform.position+ new Vector3(Random.Range(-noise,noise), Random.Range(-noise, noise), Random.Range(-noise, noise)));
            target.transform.position = fakeXYZ;
        }
        if (update)
        {
            reset |= Input.GetKeyDown(resetKey);
            sample |= Input.GetKeyDown(sampleKey);
            compute |= Input.GetKeyDown(computeKey);
        }

        if (reset)
        {
            Debug.Log("Reset");
            Clear();
            reset = false;
        }
        else if (sample)
        {
            Debug.Log("Sample");
            Sample();
            sample = false;
        }
        if (compute)
        {
            Debug.Log("Compute");
            result = ComputeCalibration(SourceCamera.pixelWidth, SourceCamera.pixelHeight, near, far);
            compute = false;
        }
    }

    #region Sampling

    private void Sample()
    {

        if (SampleSingle())
        {
            SubSample();
        }
    }
    Vector2 currentUV()
    {
        int idx = xyz.Count % targetUV.Length;
        int pass = xyz.Count / targetUV.Length;

        return targetUV[idx];

    }
    private bool SampleSingle()
    {

        if (target.IsTracked())
        {

            AddSample(currentUV(), target.GetPosition());
        }
        return target.IsTracked();
    }
    #endregion

    #region gizmos
    private void OnDrawGizmos()
    {
        if (SourceCamera == null)
        {
            SourceCamera = GetComponent<Camera>();
        }

        Gizmos.color = Color.gray;

        if (targetUV.Length > 0)
        {
            float d = 20;
            Gizmos.color = target.IsTracked() ? Color.white : Color.red;
            int currentIdx = xyz.Count % targetUV.Length;
            Vector2 currentTarget = targetUV[currentIdx];
            Vector3 point00 = SourceCamera.ViewportToWorldPoint(new Vector3(0, currentTarget.y, d));
            Vector3 point01 = SourceCamera.ViewportToWorldPoint(new Vector3(1, currentTarget.y, d));
            Vector3 point10 = SourceCamera.ViewportToWorldPoint(new Vector3(currentTarget.x, 0, d));
            Vector3 point11 = SourceCamera.ViewportToWorldPoint(new Vector3(currentTarget.x, 1, d));
            Gizmos.DrawLine(point00, point01);
            Gizmos.DrawLine(point10, point11);
        }
    }
    //Gizmos.DrawLine()


    void OnDrawGizmosSelected()
    {
        if (targetUV.Length == samplingWidth * samplingHeight)
        {
            if (subsampling)
            {
                SubSample();
                for (int x = 0; x < samplingWidth - 1; ++x)
                {
                    for (int y = 0; y < samplingHeight - 1; ++y)
                    {
                        int idx00 = (y) + (x) * samplingHeight;
                        int idx10 = (y) + (x + 1) * samplingHeight;
                        int idx01 = (y + 1) + (x) * samplingHeight;
                        int idx11 = (y + 1) + (x + 1) * samplingHeight;


                        Vector3 p00 = (Vector3)targetUV[idx00];
                        Vector3 p10 = (Vector3)targetUV[idx10];
                        Vector3 p01 = (Vector3)targetUV[idx01];
                        Vector3 p11 = (Vector3)targetUV[idx11];

                        Gizmos.color = Color.gray;
                        Vector3 p00w = SourceCamera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p00);
                        Vector3 p10w = SourceCamera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p10);
                        Vector3 p01w = SourceCamera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p01);
                        Vector3 p11w = SourceCamera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p11);

                        Gizmos.DrawLine(p00w, p10w);
                        Gizmos.DrawLine(p00w, p01w);
                        Gizmos.DrawLine(p11w, p10w);
                        Gizmos.DrawLine(p11w, p01w);
                    }
                }
                for (int idx = 0; idx < subsampledXYZ.Count; ++idx)
                {
                    // Gizmos.color = Color.white;
                    Gizmos.color = new Color(subsampledUV[idx].x, subsampledUV[idx].y, 0);
                    Gizmos.DrawLine(SourceCamera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)subsampledUV[idx]), subsampledXYZ[idx]);
                    Gizmos.DrawSphere(subsampledXYZ[idx], 0.125f);
                }
            }
            else
            {

                for (int current = 0; current < xyz.Count; ++current)
                {
                    try
                    {
                        Gizmos.color = new Color(uv[current].x, uv[current].y, 0);
                        Gizmos.DrawWireSphere(xyz[current], 0.1f);

                        Gizmos.color = new Color(uv[current].x, uv[current].y, 0, 0.5f);
                        Gizmos.DrawLine(transform.position, xyz[current]);
                    }
                    catch (System.Exception)
                    {
                        Debug.Log("" + current + "/" + xyz.Count);
                    }
                }
            }
        }
        OnDrawGizmos();
    }

    
}


#endregion