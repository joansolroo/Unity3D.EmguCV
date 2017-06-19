using System.Collections.Generic;
using UnityEngine;

using System;
using System.Drawing;
//EMGU
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

public class Calibration
{
    public class CameraCalibrationResult
    {
        // http://ksimek.github.io/2013/08/13/intrinsic/
        public struct Intrinsics
        {
            public Vector2 center;      // {cx,cy}, center of the image plane
            public Vector2 focalLength; // {fx,fy}, scale of the image in pixels

            public float fx { get { return focalLength.x; } }
            public float fy { get { return focalLength.y; } }
            public float cx { get { return center.x; } }
            public float cy { get { return center.y; } }

            public int width;
            public int height;
            // The output camera matrix(A)[fx 0 cx; 0 fy cy; 0 0 1]
            // the remaining cells are zero
            public Matrix4x4 IntrinsicMatrix
            {
                get
                {
                    Matrix4x4 intrinsicMatrix = new Matrix4x4();
                    intrinsicMatrix[0, 0] = ((this.focalLength.x) / width) * 2;
                    //intrinsicMatrix[0, 2] = (1-this.center.x) / width;
                    intrinsicMatrix[1, 1] = ((this.focalLength.y) / height) * 2;
                    //intrinsicMatrix[1, 2] = (-(this.center.y) / height);
                    intrinsicMatrix[2, 2] = 1;
                    return intrinsicMatrix;
                }
            }
            public Intrinsics(Matrix<double> cvMat, Size resolution)
            {
                DebugMatrix(cvMat);
                center = new Vector2(
                    (float)cvMat[0, 2],   //center x
                    (float)cvMat[1, 2]);  //center y
                focalLength = new Vector2(
                    (float)cvMat[0, 0],   //size x
                    (float)cvMat[1, 1]);  //size y
                width = resolution.Width;
                height = resolution.Height;
            }
            public Matrix4x4 ProjectionMatrix(float near, float far)
            {
                Matrix4x4 projection = this.IntrinsicMatrix;
                projection[2, 0] = 0; //TODO find why cx and cy are not valid
                projection[2, 1] = 0; //TODO find why cx and cy are not valid
                projection[2, 3] = -2 * (far * near) / (far - near);
                projection[2, 2] = -(far + near) / (far - near);
                projection[3, 2] = -1f;

                return projection;
            }
        }
        public struct Extrinsics
        {
            Vector3 _position;
            Vector3 _rotation;


            public Extrinsics(Vector3 __position, Vector3 __rotation)
            {
                // Changing handness of the position
                _position = __position;
                _position.z = __position.z;
                _position.y = -__position.y;

                Vector3 m = __rotation;
                float theta = (float)(Math.Sqrt(m.x * m.x + m.y * m.y + m.z * m.z) * 180 / Math.PI);
                Vector3 axis = new Vector3(-m.x, m.z, m.y);
                Quaternion rot = Quaternion.AngleAxis(theta, axis);
                Vector3 euler = rot.eulerAngles;

                _rotation = new Vector3(euler.x + 180, -euler.z, euler.y + 180);
                _position = Quaternion.Euler(_rotation) * _position;

            }

            public Vector3 Position
            {
                get
                {
                    return _position;
                }
            }

            public Vector3 Rotation
            {
                get
                {
                    return _rotation;
                }
            }

            public void ApplyToTransform(Transform transform)
            {
                transform.localPosition = _position;
                transform.localRotation = Quaternion.Euler(_rotation);
            }

        }
        public struct Distortion
        {
            float k1, k2;
            float p1, p2;

            float k3, k4, k5, k6; // additional optional distortion coefficients

            public Distortion(Emgu.CV.IInputOutputArray cvMat)
            {
                float[] coefficients = new float[8];
                Matrix<double> mat = (Matrix<double>)cvMat;
                k1 = (float)mat[0, 0];
                k2 = (float)mat[0, 1];
                p1 = (float)mat[0, 2];
                p2 = (float)mat[0, 3];

                k3 = k4 = k5 = k6 = 0;
                int length = mat.Height;
                if (length > 4)
                {
                    k3 = (float)mat[0, 4];
                    if (length > 5)
                    {
                        k4 = (float)mat[0, 5];
                        if (length > 6)
                        {
                            k5 = (float)mat[0, 6];
                            if (length > 7)
                            {
                                k6 = (float)mat[0, 7];
                            }
                        }
                    }
                }
            }
            new public string ToString()
            {
                return "distortion: [" + k1 + "," + k2 + "," + p1 + "," + p2 + "," + k3 + "," + k4 + "," + k5 + "," + k6 + "]";
            }
        }
        int width;
        int height;

        Intrinsics _intrinsics; // fx,cx,fy,cy
        Extrinsics _extrinsics; // position, rotation [,scale?]
        Distortion _distortion; // k1,k2,p1,p2
        double error;

        public Intrinsics intrinsics
        {
            get
            {
                return _intrinsics;
            }
        }
        public Extrinsics extrinsics
        {
            get
            {
                return _extrinsics;
            }
        }

        public Distortion distortion
        {
            get
            {
                return _distortion;
            }

            set
            {
                _distortion = value;
            }
        }

        public CameraCalibrationResult(int _width, int _height, Extrinsics __extrinsics, Intrinsics __intrinsics, Distortion __distortion, double _error)
        {
            width = _width;
            height = _height;

            this._extrinsics = __extrinsics;
            this._intrinsics = __intrinsics;
            this._distortion = __distortion;

            error = _error;
        }
    }
    public class ProjectorCalibrationResult : CameraCalibrationResult
    {
        Matrix4x4 projection;
        float near;
        float far;

        public ProjectorCalibrationResult(int _width, int _height, Extrinsics _extrinsics, Intrinsics _intrinsics, Distortion _distortion, double _error) :
            base(_width, _height, _extrinsics, _intrinsics, _distortion, _error)

        {
        }
    }

    // Original method http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html
    //// An example of the code working can be found in VVVV
    //// vvvv git https://github.com/elliotwoods/VVVV.Nodes.EmguCV/blob/master/src/CameraCalibration/CalibrateCamera.cs
    //// to see it working in vvvv, check "{vvvv}\packs\{vvvv.imagepack}\nodes\modules\Image\OpenCV\CalibrateProjector (CV.Transform).v4p"
    //// The general idea is to take the camera calibration and use it for a projector, by adding near/far planes
    //// The code might be outdated, since they changed the EmguCV interface

    // Returns CalibrationCameraResult with the calibration parameters if callibration was possible, null otherwise
    public static CameraCalibrationResult ComputeCameraCalibration(
        Vector3[] inObjectPoints,                   // N Points in space(x,y,z)
                                                    // * when computing intrinsics, use the checkerboard as reference frame (that means the corners won't move)
                                                    // * when computing extrinsics, you can use the checkerboard corners in world coordinates for global position estimation
        Vector2[] inImagePoints,                    // N*S points on the image plane(u,v) matching the N points in space, where
                                                    // * for intrinsic computation, S = number of samples 
                                                    // * for extrinsic computation, S = 1
        Size sensorSize,                            // Size of the image, used only to initialize intrinsic camera matrix
        Matrix<double> IntrinsicMatrix,                        // The output camera matrix(A)[fx 0 cx; 0 fy cy; 0 0 1]. If CV_CALIB_USE_INTRINSIC_GUESS and / or CV_CALIB_FIX_ASPECT_RATION are specified, some or all of fx, fy, cx, cy must be initialized
        out string status,                          // OK if everything went well, verbse error description otherwise
        bool intrinsicGuess = true,                 // If intrinsicGuess is true, the intrinsic matrix will be initialized with default values
        bool normalizedImageCoordinates = true,     // if true, the image coordinates are normalized between 0-1
        CalibType flags = CalibType.UseIntrinsicGuess)   // Different flags:
                                                         // * If Emgu.CV.CvEnum.CalibType == CV_CALIB_USE_INTRINSIC_GUESS and/or CV_CALIB_FIX_ASPECT_RATIO are specified, some or all of fx, fy, cx, cy must be initialized before calling the function
                                                         // * if you use FIX_ASPECT_RATIO and FIX_FOCAL_LEGNTH options, these values needs to be set in the intrinsic parameters before the CalibrateCamera function is called. Otherwise 0 values are used as default.  
                                                         //            flags |= CalibType.UseIntrinsicGuess;   // uses the intrinsicMatrix as initial estimation, or generates an initial estimation using imageSize
                                                         //            flags |= CalibType.FixFocalLength;      // if (CV_CALIB_USE_INTRINSIC_GUESS) then: {fx,fy} are constant
                                                         //            flags |= CalibType.FixAspectRatio;      // if (CV_CALIB_USE_INTRINSIC_GUESS) then: fy is a free variable, fx/fy stays constant
                                                         //            flags |= CalibType.FixPrincipalPoint;   // if (CV_CALIB_USE_INTRINSIC_GUESS) then: {cx,cy} are constant
                                                         //            flags |= (CalibType.FixK1               //  Given CalibType.FixK{i}: if (CV_CALIB_USE_INTRINSIC_GUESS) then: K{i} = distortionCoefficents[i], else:k ki = 0
                                                         //                    | CalibType.FixK2
                                                         //                    | CalibType.FixK3
                                                         //                    | CalibType.FixK4
                                                         //                    | CalibType.FixK5
                                                         //                    | CalibType.FixK6);
                                                         //            flags |= CalibType.ZeroTangentDist;     // tangential distortion is zero: {P1,P2} = {0,0}
                                                         //            flags |= CalibType.RationalModel;       // enable K4,k5,k6, disabled by default           
    {
        int nPointsPerImage = inObjectPoints.Length;
        if (nPointsPerImage == 0)
        {
            status = "Insufficient points";
            return null;
        }
        int nImages = inImagePoints.Length / nPointsPerImage;

        Debug.Log("point/images" + nPointsPerImage + "/" + nImages);


        //Intrinsics: an inout intrisic matrix, and depending on the calibrationType, the distortion Coefficients
        if (intrinsicGuess)
        {

            IntrinsicMatrix = new Matrix<double>(3, 3);
            // NOTE: A possible cause of failure is that this matrix might be transposed (given how openCV handles indexes)
            IntrinsicMatrix[0, 0] = sensorSize.Width;
            IntrinsicMatrix[1, 1] = sensorSize.Height;
            IntrinsicMatrix[0, 2] = sensorSize.Width / 2.0d;
            IntrinsicMatrix[1, 2] = sensorSize.Height / 2.0d;
            IntrinsicMatrix[2, 2] = 1;

        }
        Emgu.CV.IInputOutputArray distortionCoeffs = new Matrix<double>(1, 8); // The output 4x1 or 1x4 vector of distortion coefficients[k1, k2, p1, p2]

        // Matching world points (3D) to image points (2D), with the accompaining size of the image in pixels 
        MCvPoint3D32f[][] objectPoints = new MCvPoint3D32f[nImages][];  //The joint matrix of object points, 3xN or Nx3, where N is the total number of points in all views
        PointF[][] imagePoints = new PointF[nImages][];                 //The joint matrix of corresponding image points, 2xN or Nx2, where N is the total number of points in all views

        for (int i = 0; i < nImages; i++)
        {
            objectPoints[i] = new MCvPoint3D32f[nPointsPerImage];
            imagePoints[i] = new PointF[nPointsPerImage];

            for (int j = 0; j < nPointsPerImage; j++)
            {
                objectPoints[i][j].X = inObjectPoints[j].x;
                objectPoints[i][j].Y = inObjectPoints[j].y;
                objectPoints[i][j].Z = inObjectPoints[j].z;

                if (normalizedImageCoordinates)
                {
                    imagePoints[i][j].X = inImagePoints[i * nPointsPerImage + j].x * (sensorSize.Width - 1);
                    imagePoints[i][j].Y = (1 - inImagePoints[i * nPointsPerImage + j].y) * (sensorSize.Height - 1);
                }
                else
                {
                    imagePoints[i][j].X = inImagePoints[i * nPointsPerImage + j].x;
                    imagePoints[i][j].Y = (1 - inImagePoints[i * nPointsPerImage + j].y);
                }

            }
        }

        //Extrinsics: they are decomposed in position and orientation
        Mat[] rotationVectors;   //The output 3xM or Mx3 array of rotation vectors(compact representation of rotation matrices, see cvRodrigues2).
        Mat[] translationVectors; //The output 3xM or Mx3 array of translation vectors

        // When to end: 10 iterations
        Emgu.CV.Structure.MCvTermCriteria termCriteria = new Emgu.CV.Structure.MCvTermCriteria(10); //The termination criteria

        try
        {
            // To make this method work it was necessary to patch it (see below)
            double reprojectionError = CalibrateCamera(
                objectPoints,
                imagePoints,
                sensorSize,
                IntrinsicMatrix,
                distortionCoeffs,
                flags,
                termCriteria,
                out rotationVectors,
                out translationVectors);


            var rotation = new Matrix<double>(rotationVectors[0].Rows, rotationVectors[0].Cols, rotationVectors[0].DataPointer);

            CameraCalibrationResult calibration = new CameraCalibrationResult(
                 sensorSize.Width, sensorSize.Height,
                 new CameraCalibrationResult.Extrinsics(MatToVector3(translationVectors[0]), MatToVector3(rotationVectors[0])),
                 new CameraCalibrationResult.Intrinsics(IntrinsicMatrix, sensorSize),
                 new CameraCalibrationResult.Distortion(distortionCoeffs),
                 reprojectionError
                 );
            DebugMatrix(IntrinsicMatrix);
            status = "OK! " + reprojectionError;


            return calibration;


        }
        catch (Exception e)
        {   // Error 
            status = e.Message;
            return null;
        }
    }
    static Vector3 MatToVector3(Mat m)
    {
        double[] data = new double[3];
        m.CopyTo<double>(data);
        return new Vector3(
            (float)data[0],
            (float)data[1],
            (float)data[2]);
    }

    public ProjectorCalibrationResult ToProjectorCalibration(CameraCalibrationResult cameraCalibration)
    {
        //TODO
        return null;
    }

    static void DebugMatrix(Matrix<float> matrix)
    {
        string m = "|";
        for (int x = 0; x < matrix.Cols; ++x)
        {
            for (int y = 0; y < matrix.Rows; ++y)
            {
                m += matrix[x, y];
                if (y < matrix.Cols - 1)
                {
                    m += "\t";
                }
                else
                {
                    m += "|\n|";
                }
            }
        }
        Debug.Log(m);
    }

    static void DebugMatrix(Matrix<double> matrix)
    {
        string m = "|";
        for (int x = 0; x < matrix.Cols; ++x)
        {
            for (int y = 0; y < matrix.Rows; ++y)
            {
                m += matrix[x, y];
                if (y < matrix.Cols - 1)
                {
                    m += "\t";
                }
                else
                {
                    m += "|\n|";
                }
            }
        }
        Debug.Log(m);
    }


    /// OH GOD this is here because a nice guy indicated there was a bug in the EmguCV code, and this is the patched version 
    /// https://stackoverflow.com/questions/33127581/how-do-i-access-the-rotation-and-translation-vectors-after-camera-calibration-in
    /// 
    /// <summary>
    /// Estimates intrinsic camera parameters and extrinsic parameters for each of the views
    /// </summary>
    /// <param name="objectPoints">The 3D location of the object points. The first index is the index of image, second index is the index of the point</param>
    /// <param name="imagePoints">The 2D image location of the points. The first index is the index of the image, second index is the index of the point</param>
    /// <param name="imageSize">The size of the image, used only to initialize intrinsic camera matrix</param>
    /// <param name="rotationVectors">The output 3xM or Mx3 array of rotation vectors (compact representation of rotation matrices, see cvRodrigues2). </param>
    /// <param name="translationVectors">The output 3xM or Mx3 array of translation vectors</param>/// <param name="calibrationType">cCalibration type</param>
    /// <param name="termCriteria">The termination criteria</param>
    /// <param name="cameraMatrix">The output camera matrix (A) [fx 0 cx; 0 fy cy; 0 0 1]. If CV_CALIB_USE_INTRINSIC_GUESS and/or CV_CALIB_FIX_ASPECT_RATION are specified, some or all of fx, fy, cx, cy must be initialized</param>
    /// <param name="distortionCoeffs">The output 4x1 or 1x4 vector of distortion coefficients [k1, k2, p1, p2]</param>
    /// <returns>The final reprojection error</returns>
    public static double CalibrateCamera(
       MCvPoint3D32f[][] objectPoints,
       PointF[][] imagePoints,
       Size imageSize,
       IInputOutputArray cameraMatrix,
       IInputOutputArray distortionCoeffs,
       Emgu.CV.CvEnum.CalibType calibrationType,
       MCvTermCriteria termCriteria,
       out Mat[] rotationVectors,
       out Mat[] translationVectors)
    {
        System.Diagnostics.Debug.Assert(objectPoints.Length == imagePoints.Length, "The number of images for objects points should be equal to the number of images for image points");
        int imageCount = objectPoints.Length;

        using (VectorOfVectorOfPoint3D32F vvObjPts = new VectorOfVectorOfPoint3D32F(objectPoints))
        using (VectorOfVectorOfPointF vvImgPts = new VectorOfVectorOfPointF(imagePoints))
        {
            double reprojectionError;
            using (VectorOfMat rVecs = new VectorOfMat())
            using (VectorOfMat tVecs = new VectorOfMat())
            {
                reprojectionError = CvInvoke.CalibrateCamera(
                    vvObjPts,
                    vvImgPts,
                    imageSize,
                    cameraMatrix,
                    distortionCoeffs,
                    rVecs,
                    tVecs,
                    calibrationType,
                    termCriteria);

                rotationVectors = new Mat[imageCount];
                translationVectors = new Mat[imageCount];
                for (int i = 0; i < imageCount; i++)
                {
                    rotationVectors[i] = new Mat();
                    using (Mat matR = rVecs[i])
                        matR.CopyTo(rotationVectors[i]);
                    translationVectors[i] = new Mat();
                    using (Mat matT = tVecs[i])
                        matT.CopyTo(translationVectors[i]);
                }
            }
            return reprojectionError;
        }
    }
}