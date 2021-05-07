using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Linq;

using UnityEditor.UIElements;
namespace ZeludeEditor
{
    public class AnimationExplorerWindow : EditorWindow
    {
        [MenuItem("Window/Animation Explorer Window")]
        public static void OpenAnimationExplorerWindow()
        {
            var window = GetWindow<AnimationExplorerWindow>();
            window.Show();
        }

        private void OnEnable()
        {
            this.titleContent = new GUIContent("Animation Explorer");
            var objectField = new ObjectField("Target Asset");
            objectField.objectType = typeof(UnityEngine.Object);
            var animationExplorer = new AnimationExplorer();
            objectField.RegisterValueChangedCallback(x => animationExplorer.Asset = x.newValue);
            rootVisualElement.Add(objectField);
            rootVisualElement.Add(animationExplorer);

            animationExplorer.ListView.onSelectionChange += ListView_onSelectionChange;
        }

        private void ListView_onSelectionChange(IEnumerable<object> items)
        {
            foreach (var item in items)
                if (item is UnityEngine.Object asset)
                    EditorGUIUtility.PingObject(asset);
        }
    }
}