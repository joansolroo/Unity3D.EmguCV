using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyCameraCalibration : MonoBehaviour
{

    Camera _camera;

    [SerializeField]
    bool compute = false;


    int count;
    [SerializeField]
    Vector2[] uv;
    [SerializeField]
    Vector3[] xyz;

    [SerializeField]
    int size = 4;
    float delta;

    [SerializeField]
    float near = 0.1f;
    [SerializeField]
    float far = 100;
    Calibration.CameraCalibrationResult result;

    [SerializeField]
    Camera otherCamera;

    [SerializeField]
    CheckerBoard board;
    // Use this for initialization
    void Start()
    {
        _camera = GetComponent<Camera>();
        CvInvoke.CheckLibraryLoaded();

    }

    public Matrix4x4 beforeMatrix;
    public Matrix4x4 afterMatrix;
    // Update is called once per frame
    void LateUpdate()
    {
        if (compute)
        {
            //            compute = false;
            Sample();
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
            result = Calibration.ComputeCameraCalibration(xyz, uv, new System.Drawing.Size(_camera.pixelWidth, _camera.pixelHeight), new Emgu.CV.Matrix<double>(3, 3), out status);
            Debug.Log(status);
            
            beforeMatrix = _camera.projectionMatrix;
            afterMatrix = result.intrinsics.ProjectionMatrix(near,far);

            Debug.Log("distortion:" + result.distortion.ToString());

        }
        if (otherCamera != null && afterMatrix!=null)
        {
            result.extrinsics.ApplyToTransform(otherCamera.transform);
            otherCamera.projectionMatrix = afterMatrix;
        }
    }
    private void OnDrawGizmos()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        //if (uv == null || uv.Length != count)
        {
            Sample();
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
            catch (Exception e)
            {
                Debug.Log("" + current + "/" + count);
            }
        }
    }

    private void Sample()
    {
        if (board != null) { SampleBoard(); }
        else { SampleScene(); }
    }
    private void SampleScene()
    {
        float delta = 1.0f / size;
        count = (size + 1) * (size + 1);
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
    private void SampleBoard()
    {
        //if (board != null)
        {
            Vector3[] corners = board.Corners;

            count = corners.Length;
            uv = new Vector2[count];
            xyz = new Vector3[count];

            for (int idx = 0; idx < corners.Length; ++idx)
            {
                xyz[idx] = corners[idx];
                Vector3 uvd = _camera.WorldToScreenPoint(corners[idx]);
                uv[idx] = uvd;
            }

        }

    }
}
