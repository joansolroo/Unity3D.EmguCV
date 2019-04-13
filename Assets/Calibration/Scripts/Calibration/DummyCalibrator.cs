using UnityEngine;

public class DummyCalibrator : Calibrator
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
    [SerializeField] Animation orbit;

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
        orbit.enabled = true;
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
            orbit.enabled = true;
        }
        else if (sample)
        {
            Debug.Log("Sample");
            Sample();
            orbit.enabled = false;
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
    //For debugging the dummy    
    [Header("Dummy")]
    [SerializeField] bool dummy = false;
    [SerializeField] float DummyNoise = 0;
    float sumNoise = 0;
    [SerializeField] float depth = 100;
    [SerializeField] float noiseFactor = 0.5f;

    private void Sample()
    {
        Clear();
        while (xyz.Count < 2 * targetUV.Length)
        {
            SampleSingle();
        }

        SubSample();
    }
    private void SampleSingle()
    {
        int idx = xyz.Count % targetUV.Length;
        int pass = xyz.Count / targetUV.Length;

        // For debug purposes
        {
            Vector3 noise = new Vector3(
                UnityEngine.Random.Range(-noiseFactor, noiseFactor),
                UnityEngine.Random.Range(-noiseFactor, noiseFactor),
                UnityEngine.Random.Range(-noiseFactor, noiseFactor));
            sumNoise += noise.magnitude;
            Vector3 pos = SourceCamera.ViewportToWorldPoint(
                new Vector3(0, 0, 20 + depth * pass)
                + (Vector3)targetUV[idx])
                + noise;
            DummyNoise = (sumNoise / xyz.Count);
            AddSample(targetUV[idx], pos);
        }
    }
    #endregion

    #region gizmos
    
    #endregion
}