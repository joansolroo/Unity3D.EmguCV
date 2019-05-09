//----------------------------------------------------------------------------
//  Copyright (C) 2004-2016 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using Emgu.CV.CvEnum;
using UnityEngine;
using System;
using System.Drawing;
using System.Collections;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[RequireComponent(typeof(CheckerBoard))]
public class FindCheckerboard : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    private WebCamDevice[] devices;

    public int cameraCount = 0;
    [SerializeField] int GridX = 9;
    [SerializeField] int GridY = 6;

    CheckerBoard checkerBoard;
    public Camera _camera;
    // Use this for initialization
    void Start()
    {

        checkerBoard = GetComponent<CheckerBoard>();

        WebCamDevice[] devices = WebCamTexture.devices;
        int cameraCount = devices.Length;

        if (cameraCount == 0)
        {
            Image<Bgr, Byte> img = new Image<Bgr, byte>(640, 240);
            CvInvoke.PutText(img, String.Format("{0} camera found", devices.Length), new System.Drawing.Point(10, 60),
               Emgu.CV.CvEnum.FontFace.HersheyDuplex,
               1.0, new MCvScalar(0, 255, 0));
        }
        else
        {
            webcamTexture = new WebCamTexture(devices[0].name);
            webcamTexture.Play();
            CvInvoke.CheckLibraryLoaded();
        }
    }

    public Vector2[] uv;
    Vector3[] xyz;

    public bool subsampling = false;
    List<Vector2> subsampledUV = new List<Vector2>(); //subsampled uv
    List<Vector3> subsampledXYZ = new List<Vector3>(); //subsampled xyz


    [Header("Result")]

    public Matrix4x4 PerspectiveMatrixBefore;
    public Matrix4x4 PerspectiveMatrixAfter;
    public double error;
    Calibration.CameraCalibrationResult result; //All the info is contained here
    // Update is called once per frame
    void Update()
    {
        if (webcamTexture != null && webcamTexture.didUpdateThisFrame)
        {
            uv = Calibration.FindCheckerBoardCorners(webcamTexture, new Size(9, 6));
            xyz = checkerBoard.Corners;

            if (uv != null && uv.Length == 9 * 6)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Sample();
                }
            }
        }
       
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Calibrate(); 
        }
        
    }
    private void OnDrawGizmos()
    {
        for(int idx = 0; idx < allUV.Count; ++idx)
        {
            Vector2 p = allUV[idx];
            Gizmos.DrawSphere(Camera.main.ViewportToWorldPoint(new Vector3(p.x, p.y, 10)), 0.25f);
            Gizmos.DrawLine(Camera.main.ViewportToWorldPoint(new Vector3(p.x, p.y, 10)),allXYZ[idx]);
        }
        /*
        if (uv != null && uv.Length == 9 * 6)
        {
            Vector3[] cornersXYZ = checkerBoard.Corners;
            for (int x = 0; x < 9; ++x)
            {
                for (int y = 0; y < 6; ++y)
                {

                    Gizmos.color = new UnityEngine.Color(((float)x) / 9f, ((float)y) / 6f, 0);
                    int idx = x + y * 9;
                    Vector2 p = uv[idx];
                    Gizmos.DrawSphere(Camera.main.ViewportToWorldPoint(new Vector3(p.x, p.y, 10)), 0.25f);
                    Gizmos.DrawLine(Camera.main.ViewportToWorldPoint(new Vector3(p.x, p.y, 10)), cornersXYZ[idx]);
                }
            }
        }*/
    }

    void Calibrate()
    {
        string status;

        Vector2[] uvPoints = allUV.ToArray();
        Vector3[] xyzPoints = xyz;

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
        PerspectiveMatrixAfter = result.intrinsics.ProjectionMatrix(_camera.nearClipPlane, _camera.farClipPlane);

        Debug.Log("distortion:" + result.distortion.ToString());
        error = result.Error;

        if (_camera != null && PerspectiveMatrixAfter != Matrix4x4.identity)
        {
            result.extrinsics.ApplyToTransform(_camera.transform);
            _camera.projectionMatrix = PerspectiveMatrixAfter;
        }
    }
    List<Vector2> allUV = new List<Vector2>();
    List<Vector3> allXYZ = new List<Vector3>();
    void Sample()
    {
        foreach (Vector2 p in uv)
        {
            allUV.Add(p);
        }
        foreach (Vector3 p in xyz)
        {
            allXYZ.Add(p);
        }
    }
}

