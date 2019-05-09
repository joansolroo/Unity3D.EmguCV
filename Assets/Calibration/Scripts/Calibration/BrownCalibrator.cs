using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownCalibrator : MonoBehaviour {

    [SerializeField] Camera targetCamera;
    [SerializeField] CheckerBoard checkerBoard;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3[] checkerboardCorners = checkerBoard.Corners;
        foreach(Vector3 c in checkerboardCorners)
        {
            Gizmos.DrawSphere(c,2f);
            //targetCamera.WorldToViewportPoint(checkerboardCorners);
        }
    }
}
