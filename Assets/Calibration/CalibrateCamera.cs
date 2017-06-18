using System.Collections.Generic;
using UnityEngine;

using System;
using System.Drawing;
//EMGU
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;

public class CalibrateCamera : MonoBehaviour
{
    /*
    #region Display and aquaring chess board info
    Image<Bgr, Byte> bgra; // image captured
    Image<Gray, Byte> bgr; // image for processing
    const int width = 9;//9 //width of chessboard no. squares in width - 1
    const int height = 6;//6 // heght of chess board no. squares in heigth - 1
    Size patternSize = new Size(width, height); //size of chess board to be detected
    PointF[] corners; //corners found from chessboard
    Bgr[] line_colour_array = new Bgr[width * height]; // just for displaying coloured lines of detected chessboard

    int frameCount = 1;
    static Image<Gray, Byte>[] Frame_array_buffer = new Image<Gray, byte>[100]; //number of images to calibrate camera over
    int frame_buffer_savepoint = 0;
    bool start_Flag = false;
    #endregion

    #region Current mode variables
    public enum Mode
    {
        Caluculating_Intrinsics,
        Calibrated,
        SavingFrames
    }
    Mode currentMode = Mode.SavingFrames;
    #endregion

    #region Getting the camera calibration
    MCvPoint3D32f[][] corners_object_list = new MCvPoint3D32f[Frame_array_buffer.Length][];
    PointF[][] corners_points_list = new PointF[Frame_array_buffer.Length][];

    IntrinsicCameraParameters IC = new IntrinsicCameraParameters();
    ExtrinsicCameraParameters[] EX_Param;

    #endregion

    private WebCamTexture webcamTexture;
    private Texture2D resultTexture;
    private Color32[] data;
    private byte[] bytes;
    private WebCamDevice[] devices;
    public int cameraCount = 0;
    private bool _textureResized = false;
    private Quaternion baseRotation;

    // Use this for initialization
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        int cameraCount = devices.Length;

        Frame_array_buffer = new Image<Gray, byte>[(int)frameCount];
        corners_object_list = new MCvPoint3D32f[Frame_array_buffer.Length][];
        corners_points_list = new PointF[Frame_array_buffer.Length][];
        frame_buffer_savepoint = 0;

        if (cameraCount == 0)
        {
            Image<Bgr, Byte> img = new Image<Bgr, byte>(640, 240);
            CvInvoke.PutText(img, String.Format("{0} camera found", devices.Length), new System.Drawing.Point(10, 60),
               Emgu.CV.CvEnum.FontFace.HersheyDuplex,
               1.0, new MCvScalar(0, 255, 0));
            Texture2D texture = TextureConvert.ImageToTexture2D(img, FlipType.Vertical);

            this.GetComponent<GUITexture>().texture = texture;
            this.GetComponent<GUITexture>().pixelInset = new Rect(-img.Width / 2, -img.Height / 2, img.Width, img.Height);
        }
        else
        {
            webcamTexture = new WebCamTexture(devices[0].name);

            baseRotation = transform.rotation;
            webcamTexture.Play();
            //data = new Color32[webcamTexture.width * webcamTexture.height];
            CvInvoke.CheckLibraryLoaded();
        }
        //fill line colour array
        System.Random R = new System.Random();
        for (int i = 0; i < line_colour_array.Length; i++)
        {
            line_colour_array[i] = new Bgr(R.Next(0, 255), R.Next(0, 255), R.Next(0, 255));
        }
    }

    private FlipType flip = FlipType.None;
    // Update is called once per frame
    void Update()
    {
        if (webcamTexture != null && webcamTexture.didUpdateThisFrame)
        {
            if (data == null || (data.Length != webcamTexture.width * webcamTexture.height))
            {
                data = new Color32[webcamTexture.width * webcamTexture.height];
            }
            webcamTexture.GetPixels32(data);

            if (bytes == null || bytes.Length != data.Length * 3)
            {
                bytes = new byte[data.Length * 3];
            }
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            GCHandle resultHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            using (Mat bgra = new Mat(new Size(webcamTexture.width, webcamTexture.height), DepthType.Cv8U, 4, handle.AddrOfPinnedObject(), webcamTexture.width * 4))
            using (Mat bgr = new Mat(webcamTexture.height, webcamTexture.width, DepthType.Cv8U, 3, resultHandle.AddrOfPinnedObject(), webcamTexture.width * 3))
            {
                CvInvoke.CvtColor(bgra, bgr, ColorConversion.Bgra2Bgr);

                #region do some image processing here

                CvInvoke.BitwiseNot(bgr, bgr);

                #endregion

                if (flip != FlipType.None)
                    CvInvoke.Flip(bgr, bgr, flip);
            }
            handle.Free();
            resultHandle.Free();
            if (resultTexture == null || resultTexture.width != webcamTexture.width ||
                resultTexture.height != webcamTexture.height)
            {
                resultTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
            }

            resultTexture.LoadRawTextureData(bytes);
            resultTexture.Apply();

            if (!_textureResized)
            {
                this.GetComponent<GUITexture>().pixelInset = new Rect(-webcamTexture.width / 2, -webcamTexture.height / 2, webcamTexture.width, webcamTexture.height);
                _textureResized = true;
            }
            transform.rotation = baseRotation * Quaternion.AngleAxis(webcamTexture.videoRotationAngle, Vector3.up);
            this.GetComponent<GUITexture>().texture = resultTexture;
            //count++;

        }

        //   if (currentMode != Mode.SavingFrames)
        //     currentMode = Mode.SavingFrames;

        //apply chess board detection
        if (currentMode == Mode.SavingFrames)
        {
            CvInvoke.FindChessboardCorners(bgr, patternSize, corners, Emgu.CV.CvEnum.CalibCbType.AdaptiveThresh);
            //we use this loop so we can show a colour image rather than a gray: //CameraCalibration.DrawChessboardCorners(Gray_Frame, patternSize, corners);

            if (corners != null) //chess board found
            {
                //make mesurments more accurate by using FindCornerSubPixel
                bgr.FindCornerSubPix(new PointF[1][] { corners }, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));

                //if go button has been pressed start aquiring frames else we will just display the points
                if (start_Flag)
                {
                    Frame_array_buffer[frame_buffer_savepoint] = bgr.Copy(); //store the image
                    frame_buffer_savepoint++;//increase buffer positon

                    //check the state of buffer
                    if (frame_buffer_savepoint == Frame_array_buffer.Length) currentMode = Mode.Caluculating_Intrinsics; //buffer full
                }

                //dram the results
                bgra.Draw(new CircleF(corners[0], 3), new Bgr(System.Drawing.Color.Yellow), 1);
                for (int i = 1; i < corners.Length; i++)
                {
                    bgra.Draw(new LineSegment2DF(corners[i - 1], corners[i]), line_colour_array[i], 2);
                    bgra.Draw(new CircleF(corners[i], 3), new Bgr(System.Drawing.Color.Yellow), 1);
                }
                //calibrate the delay bassed on size of buffer
                //if buffer small you want a big delay if big small delay
                Thread.Sleep(100);//allow the user to move the board to a different position
            }
            corners = null;
        }
        if (currentMode == Mode.Caluculating_Intrinsics)
        {
            //we can do this in the loop above to increase speed
            for (int k = 0; k < Frame_array_buffer.Length; k++)
            {

                CvInvoke.FindChessboardCorners(Frame_array_buffer[k], patternSize, corners_points_list[k], Emgu.CV.CvEnum.CalibCbType.AdaptiveThresh);
                //for accuracy
                bgr.FindCornerSubPix(corners_points_list, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));

                //Fill our objects list with the real world mesurments for the intrinsic calculations
                List<MCvPoint3D32f> object_list = new List<MCvPoint3D32f>();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        object_list.Add(new MCvPoint3D32f(j * 20.0F, i * 20.0F, 0.0F));
                    }
                }
                corners_object_list[k] = object_list.ToArray();
            }

        }
        if (currentMode == Mode.Calibrated)
        {
            //display the original image
            Sub_PicturBox.Image = bgra.ToBitmap();
            //calculate the camera intrinsics
            Matrix<float> Map1, Map2;
            IC.InitUndistortMap(bgra.Width, bgra.Height, out Map1, out Map2);

            //remap the image to the particular intrinsics
            //In the current version of EMGU any pixel that is not corrected is set to transparent allowing the original image to be displayed if the same
            //image is mapped backed, in the future this should be controllable through the flag '0'
            Image<Bgr, Byte> temp = bgra.CopyBlank();
            CvInvoke.Remap(bgra, temp, Map1, Map2, 0, new MCvScalar(0));
            bgra = temp.Copy();

            //set up to allow another calculation
            start_Flag = false;
        }
        Main_Picturebox.Image = bgra.ToBitmap();
    }*/
}