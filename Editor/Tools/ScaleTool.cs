using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class ScaleTool : ManipulationTool
    {
        public override void DoTool(Vector3 position, Quaternion rotation, IEnumerable<GameObject> targets)
        {
            EditorGUI.BeginChangeCheck();
            var newScale = Handles.ScaleHandle(Vector3.one, position, rotation, HandleUtility.GetHandleSize(position));
            if (EditorGUI.EndChangeCheck())
            {
                //var diff = newScale - position;
                //foreach (var go in targets)
                //{
                //    Undo.RecordObject(go.transform, "Scale");
                //    go.transform.position += diff;
                //}
            }
        }
    }
}