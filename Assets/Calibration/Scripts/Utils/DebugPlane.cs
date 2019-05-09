using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPlane : MonoBehaviour {

    [SerializeField] int radius = 5;
    [SerializeField] int space = 1;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        for (int x = -radius; x <= radius; ++x)
        {
            for (int z = -radius; z <= radius; ++z)
            {
                Gizmos.DrawLine(new Vector3(x*space,0,z*space), new Vector3((x+1) * space, 0, z * space));
                Gizmos.DrawLine(new Vector3(x * space, 0, z * space), new Vector3((x) * space, 0, (z+1) * space));
            }
        }
        Gizmos.color = Color.white;
        for (int x = -radius; x <= radius; x+=5)
        {
            Gizmos.DrawLine(new Vector3(x * space, 0, -radius * space), new Vector3(x * space, 0, radius * space));

        }
        for (int z = -radius; z <= radius; z+=5)
        {
            Gizmos.DrawLine(new Vector3(-radius * space, 0, z * space), new Vector3(radius * space, 0, z * space));
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(0, 0, -radius * space), new Vector3(0, 0, radius * space));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-radius * space, 0, 0), new Vector3(radius * space, 0, 0));
    }
}
