using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class RotateTool : ManipulationTool
    {
        

        public override void DoTool(Vector3 position, Quaternion rotation, IEnumerable<GameObject> targets)
        {
            EditorGUI.BeginChangeCheck();
            var newrotation = Handles.RotationHandle(rotation, position);
            if (EditorGUI.EndChangeCheck())
            {
                var diff = rotation * Quaternion.Inverse(newrotation);
                foreach (var go in targets)
                {
                    Undo.RecordObject(go.transform, "Rotate");
                    go.transform.rotation *= diff;
                }
            }
        }
    }
}