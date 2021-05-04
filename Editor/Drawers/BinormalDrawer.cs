using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class BinormalDrawer : MeshGroupDrawer
    {
        public BinormalDrawer(MeshGroup meshGroup) : base(meshGroup)
        {
        }

        public override void Draw(Camera camera)
        {
            GL.Begin(GL.LINES);
            GL.Color(Color.yellow);
            var vertices = MeshGroup.GetVertexEnumerator();
            var binormals = MeshGroup.GetBinormalsEnumerator();

            while (vertices.MoveNext())
            {
                binormals.MoveNext();
                GL.Vertex(vertices.Current);
                GL.Vertex(vertices.Current + binormals.Current * GizmoLineLength);
            }
            GL.End();
        }
    }
}