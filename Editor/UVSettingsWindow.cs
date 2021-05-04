using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace ZeludeEditor
{
    public class UVSettingsWindow : PopupWindowContent
    {
        private MeshPreviewEditorWindow _window;
        private bool[] _availableUVs;

        public UVSettingsWindow(MeshPreviewEditorWindow window)
        {
            _window = window;
            var meshinfos = _window.UVTexture.MeshInfos;
            _availableUVs = new bool[8];

            foreach (var meshinfo in meshinfos)
            {
                for (int i = 0; i < 8; i++)
                {
                    _availableUVs[i] |= meshinfo.HasUVChannel(i);
                }
            }
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginVertical();
            for (int i = 0; i < _availableUVs.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                GUI.enabled = _availableUVs[i];
                GUILayout.Toggle(_window.UVTexture.UVChannelIndex == i, new GUIContent($"Channel {i}"), new GUIStyle("MenuItem"));
                if (EditorGUI.EndChangeCheck())
                    _window.ChangeUVTextureIndex(i);
            }
            GUILayout.EndVertical();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(120, EditorGUIUtility.singleLineHeight * 8);
        }
    }
}