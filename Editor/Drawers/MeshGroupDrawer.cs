using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public abstract class MeshGroupDrawer
    {
        public MeshGroup MeshGroup;

        public const float GizmoLineLength = 0.02f;

        public MeshGroupDrawer(MeshGroup meshGroup)
        {
            MeshGroup = meshGroup;
        }

        public abstract void Draw(Camera camera);
    }
}