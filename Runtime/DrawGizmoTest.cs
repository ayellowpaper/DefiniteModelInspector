using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DrawGizmoTest : MonoBehaviour
{
    [SerializeField]
    public Camera PreviewCamera;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(Vector3.zero, 0.2f);
    }
}
