//----------------------------------------------------------------------------
//  Copyright (C) 2004-2016 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System.IO;
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
using Emgu.CV.Ocl;

public class HelloTexture : MonoBehaviour
{

   // Use this for initialization
   void Start()
   {      
      Mat img = new Mat(new Size(640, 240), DepthType.Cv8U, 3);
      img.SetTo(new MCvScalar());
      String openclStr = "None";
      if (CvInvoke.HaveOpenCL)
      {
         //StringBuilder builder = new StringBuilder();
         using (VectorOfOclPlatformInfo oclPlatformInfos = OclInvoke.GetPlatformsInfo())
         {
            if (oclPlatformInfos.Size > 0)
            {
               PlatformInfo platformInfo = oclPlatformInfos[0];
               openclStr = platformInfo.ToString();
            }
         }
      }

      CvInvoke.PutText(img, String.Format("Emgu CV for Unity {0}", Emgu.Util.Platform.OperationSystem), new System.Drawing.Point(10, 60), Emgu.CV.CvEnum.FontFace.HersheyDuplex,
                       1.0, new MCvScalar(0, 255, 0));

      CvInvoke.PutText(img, String.Format("OpenCL: {0}",openclStr), new System.Drawing.Point(10, 120), Emgu.CV.CvEnum.FontFace.HersheyDuplex,
                       1.0, new MCvScalar(0, 0, 255));

      Texture2D texture = TextureConvert.InputArrayToTexture2D(img, FlipType.Vertical);

      this.GetComponent<GUITexture>().texture = texture;
      this.GetComponent<GUITexture>().pixelInset = new Rect(-img.Width / 2, -img.Height / 2, img.Width, img.Height);
      
   }

   // Update is called once per frame
   void Update()
   {

   }
}
