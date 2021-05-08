using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class MoveTool : ManipulationTool
    {
        public override void DoTool(Vector3 position, Quaternion rotation, IEnumerable<GameObject> targets)
        {
            EditorGUI.BeginChangeCheck();
            var newPos = Handles.PositionHandle(position, rotation);
            if (EditorGUI.EndChangeCheck())
            {
                var diff = newPos - position;
                foreach (var go in targets)
                {
                    Undo.RecordObject(go.transform, "Move");
                    go.transform.position += diff;
                }
            }
        }
    }
}