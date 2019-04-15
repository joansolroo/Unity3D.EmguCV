using UnityEngine;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using System;
using System.Collections;
using System.Collections.Generic;

public class Calibrator : MonoBehaviour {


    [Header("Target properties (REQUIRED)")]
    [SerializeField] protected float near = 0.1f;
    [SerializeField] protected float far = 1000;
    [SerializeField] protected Camera targetCamera;
    [SerializeField] protected Camera SourceCamera;

    [Header("Subsampling properties")]
    [SerializeField] protected bool subsampling = false;
    [SerializeField] [Range(1, 10)] protected int subdivision = 4;
    protected float subsamplingDelta;

    [Header("Samples")]
    [SerializeField] protected List<Vector2> uv = new List<Vector2>(); //sampled uv
    [SerializeField] protected List<Vector3> xyz = new List<Vector3>(); //sampled xyz
    protected List<Vector2>  subsampledUV = new List<Vector2>(); //subsampled uv
    protected List<Vector3> subsampledXYZ = new List<Vector3>(); //subsampled xyz

    [Header("Sampling")]
    [SerializeField] protected bool generateUVGrid = false;
    [SerializeField] [Range(2, 10)] protected int samplingWidth = 3;
    [SerializeField] [Range(2, 10)] protected int samplingHeight = 3;
    [SerializeField] protected float MaxUVRadius = 0.9f;

    [Header("Result")]
    public Matrix4x4 PerspectiveMatrixBefore;
    public Matrix4x4 PerspectiveMatrixAfter;
    public double error;
    protected Calibration.CameraCalibrationResult result; //All the info is contained here

    private void Awake()
    {
        CvInvoke.CheckLibraryLoaded();
    }
    // Erases all the samples
    public void Clear()
    {
        uv.Clear();
        xyz.Clear();

        subsampledUV.Clear();
        subsampledXYZ.Clear();
    }
    // Adds a sample to the problem
    public void AddSample(Vector2 uvPoint, Vector3 xyzPoint)
    {
        uv.Add(uvPoint);
        xyz.Add(xyzPoint);
    }

    // Interpolates the existing samples using Bilinear Interpolation
    // For this to work correctly, the samples of each groups need to be coplanar
    // and each group needs the same amount of samples
    public void SubSample(int groups = 1)
    {
        if (subsampling)
        {
            subsampledUV = new List<Vector2>();
            subsampledXYZ = new List<Vector3>();
            subsamplingDelta = 1f / subdivision;
            int groupSize = xyz.Count / groups;
            for (int g = 0; g < groups; ++g)
            {
                for (int idx = groupSize * g; idx < groupSize * (g + 1); ++idx)
                {
                    // sampling coordinates
                    int x = (idx % (samplingWidth * samplingHeight)) / samplingHeight;
                    int y = (idx % (samplingWidth * samplingHeight)) % samplingHeight;
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
                                    Vector2 interpolatedUV = Math3d.QuadLerp(uv[idx00], uv[idx10], uv[idx11], uv[idx01], u, v);
                                    Vector3 interpolatedXYZ = Math3d.QuadLerp(xyz[idx00], xyz[idx10], xyz[idx11], xyz[idx01], u, v);

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
        }
    }

    public Calibration.CameraCalibrationResult ComputeCalibration(int pixelWidth, int pixelHeight, float near, float far)
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
        result = Calibration.ComputeCameraCalibration(xyzPoints, uvPoints, new System.Drawing.Size(pixelWidth, pixelHeight), new Emgu.CV.Matrix<double>(3, 3), out status, true, true, flags);
        Debug.Log(status);

        Debug.Log("distortion:" + result.distortion.ToString());
        error = result.Error;

        PerspectiveMatrixAfter = result.intrinsics.ProjectionMatrix(near, far);
        if (targetCamera != null && PerspectiveMatrixAfter != Matrix4x4.identity)
        {
            PerspectiveMatrixBefore = targetCamera.projectionMatrix;
            result.extrinsics.ApplyToTransform(targetCamera.transform);
            targetCamera.projectionMatrix = PerspectiveMatrixAfter;

        }
        return result;
    }

    
    #region Gizmos
    
    #endregion
}

