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
    private bool SampleSingle()
    {

        if (target.IsTracked())
        {
            int idx = xyz.Count % targetUV.Length;
            int pass = xyz.Count / targetUV.Length;

            AddSample(targetUV[idx], target.GetPosition());
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
        //Gizmos.DrawLine()
    }
    #endregion
}