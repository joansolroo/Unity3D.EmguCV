using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnownObjectCalibrator : Calibrator {

    [SerializeField] Transform[] targetPoints;
    Vector3[] targetXYZ;

    int currentPoint = 0;
    private void Start()
    {
        targetXYZ = new Vector3[targetPoints.Length];
        for (int idx = 0; idx < targetPoints.Length; ++idx)
        {
            targetXYZ[idx] = targetPoints[idx].position;
        }
        Show();
    }

    [SerializeField] bool reset;
    [SerializeField] bool sample;
    [SerializeField] bool compute;

    Vector2 _uv;
    void LateUpdate()
    {
        _uv = SourceCamera.ScreenToViewportPoint(Input.mousePosition);
        {
            reset |= Input.GetKeyDown(KeyCode.Escape);
            sample |= Input.GetMouseButtonDown(0);
            compute |= Input.GetMouseButtonDown(1);
        }

        if (reset)
        {
            Hide();
            currentPoint = 0;
            Debug.Log("Reset");
            Clear();
            reset = false;
            Show();
        }
        else if (sample)
        {
            Debug.Log("Sample");
            Sample();
            sample = false;
        }
        if (compute)
        {
            Debug.Log("Compute");
            result = ComputeCalibration(SourceCamera.pixelWidth, SourceCamera.pixelHeight, near, far);
            compute = false;
        }
    }
    void Sample()
    {
        Hide();
       
        AddSample(_uv, targetXYZ[currentPoint]);
        currentPoint= (currentPoint+1)%targetXYZ.Length;
        Show();
    }

    void Show()
    {
        targetPoints[currentPoint].GetComponent<Renderer>().material.color = Color.red;
    }
    void Hide()
    {
        targetPoints[currentPoint].GetComponent<Renderer>().material.color = Color.white;
    }

    private void OnDrawGizmos()
    {
        
    }
}
