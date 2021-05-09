using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace ZeludeEditor
{
    public class AssetDragAndDropManipulator : PointerManipulator
    {
        public int InstanceID { get; set; }

        private static bool _isWatingForDrag = false;
        private static Vector2 _dragStartPosition;

        public AssetDragAndDropManipulator(int instanceID)
        {
            InstanceID = instanceID;
        }

        private void DragAndDropMouseDown(MouseDownEvent evt)
        {
            _isWatingForDrag = true;
            _dragStartPosition = evt.mousePosition;
        }

        private void DragAndDropMouseMove(MouseMoveEvent evt)
        {
            if (_isWatingForDrag && Vector2.Distance(_dragStartPosition, evt.mousePosition) >= 6f)
            {
                _isWatingForDrag = false;
                var asset = EditorUtility.InstanceIDToObject(InstanceID);
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { asset };
                DragAndDrop.StartDrag($"Drag {asset.name}");
            }
        }

        private void DragAndDropMouseUp(MouseUpEvent evt)
        {
            _isWatingForDrag = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(DragAndDropMouseDown);
            target.RegisterCallback<MouseMoveEvent>(DragAndDropMouseMove);
            target.RegisterCallback<MouseUpEvent>(DragAndDropMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(DragAndDropMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(DragAndDropMouseMove);
            target.UnregisterCallback<MouseUpEvent>(DragAndDropMouseUp);
        }
    }
}