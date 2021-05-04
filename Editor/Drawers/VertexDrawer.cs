using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class VertexDrawer : MeshGroupDrawer
    {
        public VertexDrawer(MeshGroup meshGroup) : base(meshGroup)
        {
        }

        public override void Draw(Camera camera)
        {
            void DrawVertex(Vector3 position)
            {
                float size = GetVertexHandleSize(position, camera);
                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.TRS(position, Quaternion.LookRotation(camera.transform.forward), new Vector3(size, size, size)));
                GL.Begin(GL.LINE_STRIP);
                GL.Color(Color.green);
                GL.Vertex(new Vector3(-0.5f, -0.5f));
                GL.Vertex(new Vector3(-0.5f, 0.5f));
                GL.Vertex(new Vector3(0.5f, 0.5f));
                GL.Vertex(new Vector3(0.5f, -0.5f));
                GL.Vertex(new Vector3(-0.5f, -0.5f));
                GL.End();
                GL.PopMatrix();
            }

            var vertices = MeshGroup.GetVertexEnumerator();
            while (vertices.MoveNext())
            {
                DrawVertex(vertices.Current);
            }
        }

        public static float GetVertexHandleSize(Vector3 position, Camera camera)
        {
            Vector3 diff = camera.transform.position - position;
            var inv = Mathf.InverseLerp(0.05f, 0.5f, diff.magnitude);
            return Mathf.Lerp(0.001f, 0.005f, inv);
        }
    }
}