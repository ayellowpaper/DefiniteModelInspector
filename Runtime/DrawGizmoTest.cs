using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class DrawGizmoTest : MonoBehaviour
{
    public Camera PreviewCamera;
    public Material Material;

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawSphere(Vector3.zero, 0.2f);
    }

    private void Awake()
    {
        if (Material == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            Material = new Material(shader);
            Material.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            Material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            Material.SetInt("_ZWrite", 0);
        }
    }

    private void OnDestroy()
    {
        if (Material != null)
            UnityEngine.Object.DestroyImmediate(Material, true);
    }

    void OnRenderObject()
    {
        if (Material == null)
            return;

        Material.SetPass(0);

        GL.PushMatrix();
        // Set transformation matrix for drawing to
        // match our transform
        GL.MultMatrix(transform.localToWorldMatrix);

        int lineCount = 20;
        float radius = 1f;
        // Draw lines
        GL.Begin(GL.LINES);
        for (int i = 0; i < lineCount; ++i)
        {
            float a = i / (float)lineCount;
            float angle = a * Mathf.PI * 2;
            // Vertex colors change from red to green
            GL.Color(new Color(a, 1 - a, 0, 0.8F));
            // One vertex at transform position
            GL.Vertex3(0, 0, 0);
            // Another vertex at edge of circle
            GL.Vertex3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
        GL.End();
        GL.PopMatrix();

        //GL.PushMatrix();
        //Material.SetPass(0);
        //Handles.color = Color.red;
        //Handles.DrawWireCube(Vector3.zero, Vector3.one);
        //GL.PopMatrix();
    }
}
