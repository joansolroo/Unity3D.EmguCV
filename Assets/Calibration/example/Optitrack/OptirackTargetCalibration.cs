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
    [SerializeField]
    bool update = false;
    [SerializeField]
    bool compute = false;

    [SerializeField]
    Vector2[] targetUV; //fixed UV points to sample

    [SerializeField]
    List<Vector2> uv = new List<Vector2>(); //sampled uv
    [SerializeField]
    List<Vector3> xyz = new List<Vector3>(); //sampled xyz

    [SerializeField]
    float near = 0.1f;
    [SerializeField]
    float far = 100;
    Calibration.CameraCalibrationResult result;

    [SerializeField] OptitrackRigidBody target;

    // Use this for initialization
    void Start()
    {
        _camera = GetComponent<Camera>();
        CvInvoke.CheckLibraryLoaded();
    }

    public Matrix4x4 beforeMatrix;
    public Matrix4x4 afterMatrix;

    public double error;

    // Update is called once per frame
    void LateUpdate()
    {
        if (update)
        {
            bool reset = Input.GetKeyDown(KeyCode.Escape);
            bool sample = Input.GetKeyDown(KeyCode.Space);
            compute = Input.GetKeyDown(KeyCode.Return);

            if (reset)
            {
                uv.Clear();
                xyz.Clear();
            }
            else if (target.tracked && sample)
            {
                Sample();
            }
        }
        if (compute)
        {
            string status;

            //Compute calibration

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
            result = Calibration.ComputeCameraCalibration(xyz.ToArray(), uv.ToArray(), new System.Drawing.Size(_camera.pixelWidth, _camera.pixelHeight), new Emgu.CV.Matrix<double>(3, 3), out status, true, true, flags);
            Debug.Log(status);

            beforeMatrix = _camera.projectionMatrix;
            afterMatrix = result.intrinsics.ProjectionMatrix(near, far);

            Debug.Log("distortion:" + result.distortion.ToString());
            error = result.Error;

            if (_camera != null && afterMatrix != Matrix4x4.identity)
            {
                result.extrinsics.ApplyToTransform(_camera.transform);
                _camera.projectionMatrix = afterMatrix;
            }

        }
    }
    private void OnDrawGizmosSelected()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }


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
        Gizmos.color = Color.gray;
        for (int p = 0; p < 3; ++p)
        {
            float r = 1;
            if (p == 1) r = 0.75f;
            if (p == 2) r = 0.9f;
            Vector3 point00 = _camera.ViewportToWorldPoint(new Vector3(1 - r, 1 - r, 20));
            Vector3 point01 = _camera.ViewportToWorldPoint(new Vector3(1 - r, r, 20));
            Vector3 point10 = _camera.ViewportToWorldPoint(new Vector3(r, 1 - r, 20));
            Vector3 point11 = _camera.ViewportToWorldPoint(new Vector3(r, r, 20));
            Gizmos.DrawLine(point00, point01);
            Gizmos.DrawLine(point00, point10);
            Gizmos.DrawLine(point11, point01);
            Gizmos.DrawLine(point11, point10);
        }

        for (int idx = 0; idx < targetUV.Length; ++idx)
        {
            Gizmos.color = new Color(targetUV[idx].x, targetUV[idx].y, 0);

            Vector3 point = _camera.ViewportToWorldPoint(new Vector3(0, 0, 20) + (Vector3)targetUV[idx]);

            Gizmos.DrawLine(_camera.transform.position, point);
            Gizmos.DrawSphere(point, 0.1f);
        }
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

    private void Sample()
    {
        int idx = xyz.Count % targetUV.Length;

        uv.Add(targetUV[idx]);
        xyz.Add(target.transform.position);
    }

}
