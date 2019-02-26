using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraGrid : MonoBehaviour
{

    Camera _camera;

    public int size = 10;

    private void OnDrawGizmos()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }
        Gizmos.color = Color.gray;
        for (int x = 0; x <= size - 1; ++x)
        {
            Vector3 from = _camera.ViewportToWorldPoint(new Vector3(x / (float)(size - 1), 0, 5));
            Vector3 to = _camera.ViewportToWorldPoint(new Vector3(x / (float)(size - 1), 1, 5));
            Gizmos.DrawLine(from, to);
        }
        for (int y = 0; y <= size - 1; ++y)
        {
            Vector3 from = _camera.ViewportToWorldPoint(new Vector3(0, y / (float)(size - 1), 5));
            Vector3 to = _camera.ViewportToWorldPoint(new Vector3(1, y / (float)(size - 1), 5));
            Gizmos.DrawLine(from, to);
        }
    }
}
