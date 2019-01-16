using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptirackTargetCalibration : MonoBehaviour
{

    Camera _camera;

    [Header("Setup")]
    [SerializeField] bool update = false;
    [SerializeField] bool reset = false;
    [SerializeField] bool sample = false;
    [SerializeField] bool dummy = false;

    [SerializeField] bool compute = false;

    [Header("Target properties")]
    [SerializeField] OptitrackRigidBody target;
    [SerializeField]
    float near = 0.1f;
    [SerializeField]
    float far = 100;

    [Header("Subsampling properties")]
    [SerializeField]
    bool subsampling = false;
    [SerializeField]
    float radius = 0.9f;
    [SerializeField]
    int samplingWidth = 3;
    [SerializeField]
    int samplingHeight = 3;
    [SerializeField]
    float subsamplingDelta = 0.125f;

    [Header("Samples")]
    [SerializeField]
    Vector2[] targetUV; //fixed UV points to sample
    [SerializeField]
    List<Vector2> uv = new List<Vector2>(); //sampled uv
    [SerializeField]
    List<Vector3> xyz = new List<Vector3>(); //sampled xyz

    List<Vector2> subsampledUV = new List<Vector2>(); //subsampled uv
    List<Vector3> subsampledXYZ = new List<Vector3>(); //subsampled xyz


    [Header("Result")]
   
    public Matrix4x4 PerspectiveMatrixBefore;
    public Matrix4x4 PerspectiveMatrixAfter;
    public double error;
    Calibration.CameraCalibrationResult result; //All the info is contained here

    // Use this for initialization
    void Start()
    {
        _camera = GetComponent<Camera>();
        CvInvoke.CheckLibraryLoaded();

        if (update)
        {
            targetUV = new Vector2[samplingWidth * samplingHeight];

            for (int x = 0; x < samplingWidth; ++x)
            {
                for (int y = 0; y < samplingHeight; ++y)
                {
                    int idx = (y) + (x) * samplingWidth;
                    targetUV[idx] = new Vector2(Mathf.Lerp(1 - radius, radius, (x) / (samplingWidth - 1f)), Mathf.Lerp(1 - radius, radius, (y) / (samplingHeight - 1f)));
                }
            }
            if (dummy)
            {
                while (xyz.Count < 2 * targetUV.Length)
                {
                    Sample();
                }
            }

        }



    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (update)
        {
            bool reset = Input.GetKeyDown(KeyCode.Escape);
            bool sample = Input.GetKeyDown(KeyCode.Space);
            compute = Input.GetKeyDown(KeyCode.Return);
        }
        if (reset)
        {
            uv.Clear();
            xyz.Clear();
            reset = false;
        }
        else if (sample)
        {
            Sample();
            sample = false;
        }
        if (compute)
        {
            Calibrate();
        }
    }
    void Calibrate()
    {
        string status;

        Vector2[] uvPoints = subsampling ? subsampledUV.ToArray() : uv.ToArray();
        Vector3[] xyzPoints = subsampling ? subsampledXYZ.ToArray() : xyz.ToArray();

        if (subsampling)
        {
            SubSample();
            uvPoints = subsampledUV.ToArray();
            xyzPoints = subsampledXYZ.ToArray();

        }
        else
        {
            uvPoints = uv.ToArray();
            xyzPoints = xyz.ToArray();
        }

        //Compute calibration
        Debug.Log("Calibration requiested: #uv:" + uvPoints.Length + "#xyz:" + xyzPoints.Length);
        Emgu.CV.CvEnum.CalibType flags = Emgu.CV.CvEnum.CalibType.UseIntrinsicGuess;   // uses the intrinsicMatrix as initial estimation, or generates an initial estimation using imageSize
                                                                                       //flags |= CalibType.FixFocalLength;      // if (CV_CALIB_USE_INTRINSIC_GUESS) then: {fx,fy} are constant
                                                                                       // flags |= CalibType.FixAspectRatio;      // if (CV_CALIB_USE_INTRINSIC_GUESS) then: fy is a free variable, fx/fy stays constant
                                                                                       //flags |= CalibType.FixPrincipalPoint;   // if (CV_CALIB_USE_INTRINSIC_GUESS) then: {cx,cy} are constant
                                                                                       /*flags |= (CalibType.FixK1               //  Given CalibType.FixK{i}: if (CV_CALIB_USE_INTRINSIC_GUESS) then: K{i} = distortionCoefficents[i], else:k ki = 0
                                                                                                | CalibType.FixK2
                                                                                                | CalibType.FixK3
                                                                                                | CalibType.FixK4
                                                                                                | CalibType.FixK5
                                                                                                | CalibType.FixK6);
                                                                                      // flags |= CalibType.FixIntrinsic;
                                                                                       flags |= CalibType.ZeroTangentDist;     // tangential distortion is zero: {P1,P2} = {0,0}
                                                                                       */
        result = Calibration.ComputeCameraCalibration(xyzPoints, uvPoints, new System.Drawing.Size(_camera.pixelWidth, _camera.pixelHeight), new Emgu.CV.Matrix<double>(3, 3), out status, true, true, flags);
        Debug.Log(status);

        PerspectiveMatrixBefore = _camera.projectionMatrix;
        PerspectiveMatrixAfter = result.intrinsics.ProjectionMatrix(near, far);

        Debug.Log("distortion:" + result.distortion.ToString());
        error = result.Error;

        if (_camera != null && PerspectiveMatrixAfter != Matrix4x4.identity)
        {
            result.extrinsics.ApplyToTransform(_camera.transform);
            _camera.projectionMatrix = PerspectiveMatrixAfter;
        }
    }
    #region Sampling
    //For debugging the dummy
    [SerializeField] float DummyNoise = 0;
    float sumNoise = 0;
    private void Sample()
    {
        int idx = xyz.Count % targetUV.Length;
        int pass = xyz.Count / targetUV.Length;

        // For debug purposes
        if (dummy)
        {
            float depth = 100;
            float noiseFactor = 0.5f;
            Vector3 noise = new Vector3(UnityEngine.Random.Range(-noiseFactor, noiseFactor), UnityEngine.Random.Range(-noiseFactor, noiseFactor), UnityEngine.Random.Range(-noiseFactor, noiseFactor));
            sumNoise += noise.magnitude;
            Vector3 pos = _camera.ViewportToWorldPoint(
                new Vector3(0, 0, 20 + depth * pass)
                + (Vector3)targetUV[idx])
                + noise;
            uv.Add(targetUV[idx]);
            xyz.Add(pos);
            DummyNoise = (sumNoise / xyz.Count);
        }
        else if (target.tracked)
        {
            uv.Add(targetUV[idx]);
            xyz.Add(target.transform.position);
        }
        SubSample();
    }
    private void SubSample()
    {
        subsampledUV = new List<Vector2>();
        subsampledXYZ = new List<Vector3>();

        for (int idx = 0; idx < xyz.Count; ++idx)
        {
            // sampling coordinates
            int x = (idx % (samplingWidth * samplingHeight)) / samplingWidth;
            int y = (idx % (samplingWidth * samplingHeight)) % samplingWidth;
            int pass = idx / (samplingWidth * samplingHeight);

            // If not on the bottom nor right edges, then interpolate towards the bottom-right
            if (x < samplingWidth - 1 && y < samplingHeight - 1)
            {
                int idx00 = (y) + (x) * samplingWidth + pass * (samplingWidth * samplingHeight);
                int idx10 = (y) + (x + 1) * samplingWidth + pass * (samplingWidth * samplingHeight);
                int idx01 = (y + 1) + (x) * samplingWidth + pass * (samplingWidth * samplingHeight);
                int idx11 = (y + 1) + (x + 1) * samplingWidth + pass * (samplingWidth * samplingHeight);

                if (idx10 < xyz.Count && idx01 < xyz.Count && idx11 < xyz.Count)
                {
                    for (float u = 0; u <= 1; u += subsamplingDelta)
                    {
                        for (float v = 0; v <= 1; v += subsamplingDelta)
                        {
                            Vector2 interpolatedUV = QuadLerp(uv[idx00], uv[idx10], uv[idx11], uv[idx01], u, v);
                            Vector3 interpolatedXYZ = QuadLerp(xyz[idx00], xyz[idx10], xyz[idx11], xyz[idx01], u, v);

                            subsampledUV.Add(interpolatedUV);
                            subsampledXYZ.Add(interpolatedXYZ);
                        }
                    }
                }

            }
            else //no interpolation for the last edge
            {
                subsampledUV.Add(uv[idx]);
                subsampledXYZ.Add(xyz[idx]);
            }
        }

    }
    #endregion
    #region Gizmos
    private void OnDrawGizmos()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        Gizmos.color = Color.gray;

        if (targetUV.Length > 0)
        {
            Gizmos.color = target.tracked ? Color.white : Color.red;
            int currentIdx = xyz.Count % targetUV.Length;
            Vector2 currentTarget = targetUV[currentIdx];
            Vector3 point00 = _camera.ViewportToWorldPoint(new Vector3(0, currentTarget.y, 20));
            Vector3 point01 = _camera.ViewportToWorldPoint(new Vector3(1, currentTarget.y, 20));
            Vector3 point10 = _camera.ViewportToWorldPoint(new Vector3(currentTarget.x, 0, 20));
            Vector3 point11 = _camera.ViewportToWorldPoint(new Vector3(currentTarget.x, 1, 20));
            Gizmos.DrawLine(point00, point01);
            Gizmos.DrawLine(point10, point11);
        }
        //Gizmos.DrawLine()
    }
    private void OnDrawGizmosSelected()
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
                        int idx00 = (y) + (x) * samplingWidth;
                        int idx10 = (y) + (x + 1) * samplingWidth;
                        int idx01 = (y + 1) + (x) * samplingWidth;
                        int idx11 = (y + 1) + (x + 1) * samplingWidth;


                        Vector3 p00 = (Vector3)targetUV[idx00];
                        Vector3 p10 = (Vector3)targetUV[idx10];
                        Vector3 p01 = (Vector3)targetUV[idx01];
                        Vector3 p11 = (Vector3)targetUV[idx11];

                        Gizmos.color = Color.white;
                        Vector3 p00w = _camera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p00);
                        Vector3 p10w = _camera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p10);
                        Vector3 p01w = _camera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p01);
                        Vector3 p11w = _camera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)p11);

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
                    Gizmos.DrawLine(_camera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)subsampledUV[idx]), subsampledXYZ[idx]);
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
                    catch (Exception)
                    {
                        Debug.Log("" + current + "/" + xyz.Count);
                    }
                }
            }
        }
    }
    #endregion

    // TAKEN FROM https://forum.unity.com/threads/vector-bilinear-interpolation-of-a-square-grid.205644/#post-1389342
    // Given a (u,v) coordinate that defines a 2D local position inside a planar quadrilateral, find the
    // absolute 3D (x,y,z) coordinate at that location.
    //
    //  0 <----u----> 1
    //  a ----------- b    0
    //  |             |   /|\
    //  |             |    |
    //  |             |    v
    //  |  *(u,v)     |    |
    //  |             |   \|/
    //  d------------ c    1
    //
    // a, b, c, and d are the vertices of the quadrilateral. They are assumed to exist in the
    // same plane in 3D space, but this function will allow for some non-planar error.
    //
    // Variables u and v are the two-dimensional local coordinates inside the quadrilateral.
    // To find a point that is inside the quadrilateral, both u and v must be between 0 and 1 inclusive.  
    // For example, if you send this function u=0, v=0, then it will return coordinate "a".  
    // Similarly, coordinate u=1, v=1 will return vector "c". Any values between 0 and 1
    // will return a coordinate that is bi-linearly interpolated between the four vertices.

    public Vector3 QuadLerp(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float v)
    {
        Vector3 abu = Vector3.Lerp(a, b, u);
        Vector3 dcu = Vector3.Lerp(d, c, u);
        return Vector3.Lerp(abu, dcu, v);
    }
}
