using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class AssetPostprocessorHook : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject gameObject)
        {
            if (MeshPreviewEditorWindow.RegisteredEditors.Count == 0) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (MeshPreviewEditorWindow.TryGetWindowByGuid(guid, out var window))
            {
                EditorApplication.delayCall += () => window.ReloadMesh();
            }
        }
    }
}