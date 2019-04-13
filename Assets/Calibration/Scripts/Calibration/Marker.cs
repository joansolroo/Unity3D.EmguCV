using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITrackeable
{
    Vector3 GetPosition();
    Quaternion GetOrientation();
    bool IsTracked();
}

public class Marker : MonoBehaviour,ITrackeable
{
    public bool tracked = true;

    public Quaternion GetOrientation()
    {
        return this.transform.rotation;
    }

    public Vector3 GetPosition()
    {
        return this.transform.position;
    }

    public bool IsTracked()
    {
        return tracked;
    }
}
