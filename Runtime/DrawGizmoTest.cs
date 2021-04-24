using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGizmoTest : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(Vector3.zero, 0.2f);
    }
}
