using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Projector))]
[RequireComponent(typeof(Camera))]
public class EncodeStructuredLight : MonoBehaviour
{

    Camera _camera;
    Projector _projector;

    [Header("Links Setup")]
    [SerializeField] Shader s;
    [SerializeField] Material screenShader;
    [SerializeField] RenderTexture renderTexture;

    [Header("Encode Setup")]
    [SerializeField] [Range(1, 64)] public int GridX = 1;
    [SerializeField] [Range(1, 64)] public int GridY = 1;
    [SerializeField] [Range(0, 1)] public float border = 0.001f;
    [SerializeField] float showTime = 1;

    [Header("Encode Progression")]
    [SerializeField] public int currentX;
    [SerializeField] public int currentY;
    [SerializeField] public bool flip = false;
    [SerializeField] public float t = 0;
    [SerializeField] public int frame = 0;
    [SerializeField] public bool isNewFrame;
    int LastFrame = -1;
    // Use this for initialization


    void Start()
    {
        Check();
    }



    // Update is called once per frame
    [ExecuteInEditMode]
    void Update()
    {
        if (!Application.isPlaying)
        {
            OnValidate();
        }
       
        // Time progress
        if(showTime>0)
         {
             t += Time.deltaTime;
             if (t > showTime)
             {
                 t -= showTime;
                 ++frame;
             }
         }
        frame %= 2 * GridY + 2 * GridY;
        isNewFrame = frame != LastFrame;
        
        // Set frame
        if(isNewFrame)
        {
            int subFrame = 0;

            if (frame < GridX)
            {
                subFrame = frame+1;
                currentX = subFrame;
                currentY = 1;
                flip = false;
            }
            else if (frame < 2 * GridX)
            {
                subFrame = frame+1 - GridX;
                currentX = subFrame;
                currentY = 1;
                flip = true;
            }
            else if (frame < 2 * GridX+ GridY)
            {
                subFrame = frame+1 - 2*GridX;
                currentX = 1;
                currentY = subFrame;
                flip = false;
            }
            else if (frame < 2 * GridX + 2*GridY)
            {
                subFrame = frame+1 - 2 * GridX-GridY;
                currentX = 1;
                currentY = subFrame;
                flip = true;
            }
            screenShader.SetFloat("_GridX", currentX);
            screenShader.SetFloat("_GridY", currentY);
            screenShader.SetInt("_Flip", flip?1:0);
            LastFrame = frame;
        }

    }

    private void OnValidate()
    {
        Check();
    }
    void Check()
    {
        if (screenShader == null)
        {
            screenShader = new Material(s);
        }
        if (this.enabled)
        {
            _camera = GetComponent<Camera>();
            _projector = GetComponent<Projector>();
            _projector.aspectRatio = _camera.aspect;
            _projector.nearClipPlane = _camera.nearClipPlane;
            _projector.farClipPlane = _camera.farClipPlane;
            _projector.fieldOfView = _camera.fieldOfView;

            screenShader.SetFloat("_GridX", GridX);
            screenShader.SetFloat("_GridY", GridY);
            screenShader.SetFloat("_Border", border);

            if (renderTexture == null || renderTexture.width != _camera.pixelWidth || renderTexture.height != _camera.pixelHeight)
            {
                renderTexture = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, RenderTextureFormat.ARGB32);
                renderTexture.wrapMode = TextureWrapMode.Clamp;
            }
            _projector.material.SetTexture("_ShadowTex", renderTexture);

        }
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (renderTexture != null)
        {
            Graphics.Blit(source, renderTexture, screenShader);
        }
        Graphics.Blit(source, destination, screenShader);
    }
}
