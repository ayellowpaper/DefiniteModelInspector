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
        [MenuItem("Window/Animation/Animation Explorer Window")]
        public static void OpenAnimationExplorerWindow()
        {
            var window = GetWindow<AnimationExplorerWindow>();
            window.Show();
        }

        private void OnEnable()
        {
            var animationExplorer = new AnimationExplorer();

            this.titleContent = new GUIContent("Animation Explorer");
            var objectField = new ObjectField("Target Asset");
            objectField.objectType = typeof(UnityEngine.Object);
            objectField.RegisterValueChangedCallback(evt => animationExplorer.Asset = evt.newValue);

            var clearButton = new Button(() => objectField.value = null);
            clearButton.text = "Clear";

            var header = new VisualElement();
            header.Add(objectField);
            header.Add(clearButton);
            header.style.flexDirection = FlexDirection.Row;
            objectField.style.flexGrow = 1;
            clearButton.style.width = 80;

            rootVisualElement.Add(header);
            rootVisualElement.Add(animationExplorer);
        }
    }
}