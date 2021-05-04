using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class TangentDrawer : MeshGroupDrawer
    {
        public TangentDrawer(MeshGroup meshGroup) : base(meshGroup)
        {
        }

        public override void Draw(Camera camera)
        {
            GL.Begin(GL.LINES);
            GL.Color(Color.blue);
            var vertices = MeshGroup.GetVertexEnumerator();
            var tangents = MeshGroup.GetTangentsEnumerator();

            while (vertices.MoveNext())
            {
                tangents.MoveNext();
                GL.Vertex(vertices.Current);
                GL.Vertex(vertices.Current + tangents.Current * GizmoLineLength);
            }
            GL.End();
        }
    }
}