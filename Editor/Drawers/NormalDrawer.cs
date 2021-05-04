using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class NormalDrawer : MeshGroupDrawer
    {
        public NormalDrawer(MeshGroup meshGroup) : base(meshGroup)
        {
        }

        public override void Draw(Camera camera)
        {
            GL.Begin(GL.LINES);
            GL.Color(Color.red);
            var vertices = MeshGroup.GetVertexEnumerator();
            var normals = MeshGroup.GetNormalsEnumerator();

            while (vertices.MoveNext())
            {
                normals.MoveNext();
                GL.Vertex(vertices.Current);
                GL.Vertex(vertices.Current + normals.Current * GizmoLineLength);
            }
            GL.End();
        }
    }
}