using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZeludeEditor
{
    [System.Serializable]
    public class PreviewSceneMotion
    {
        [SerializeField]
        public float CameraDistance = 5f;
        [SerializeField]
        public Quaternion PivotRotation = Quaternion.Euler(25f, 290f, 0);
        [SerializeField]
        public Vector3 PivotPosition = Vector3.zero;
        public readonly PreviewScene PreviewScene;
        public readonly Transform Pivot;

        public Bounds TargetBounds { get; set; }

        public PreviewSceneMotion(PreviewScene scene)
        {
            PreviewScene = scene;

            Pivot = new GameObject("Camera Pivot").transform;
            Pivot.transform.position = Vector3.zero;
            PreviewScene.AddGameObject(Pivot.gameObject);
            PreviewScene.Camera.transform.SetParent(Pivot);
            UpdateCameraDistance();
            Pivot.transform.SetPositionAndRotation(PivotPosition, PivotRotation);
        }

        public void DoOnGUI(Rect rect)
        {
            var current = Event.current;

            if (!rect.Contains(current.mousePosition)) return;

            if (HandleUtility.nearestControl != 0 || GUIUtility.hotControl != 0) return;

            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                PivotRotation = Quaternion.AngleAxis(current.delta.y * 0.003f * 57.29578f, PivotRotation * Vector3.right) * PivotRotation;
                PivotRotation = Quaternion.AngleAxis(current.delta.x * 0.003f * 57.29578f, Vector3.up) * PivotRotation;
                Pivot.rotation = PivotRotation;
                Event.current.Use();
            }

            if (Tools.viewTool == ViewTool.Pan)
            {
                if (current.type == EventType.MouseDrag && current.button == 2)
                {
                    Vector3 GetWorldPoint(Vector2 guiPosition)
                    {
                        var plane = new Plane(-PreviewScene.Camera.transform.forward, CameraDistance);
                        var screenPoint = Rect.PointToNormalized(rect, guiPosition);
                        screenPoint.y = 1 - screenPoint.y;
                        var ray = PreviewScene.Camera.ViewportPointToRay(screenPoint);
                        plane.Raycast(ray, out float enter);
                        return ray.GetPoint(enter);
                    }

                    var prevPoint = GetWorldPoint(current.mousePosition - current.delta);
                    var newPoint = GetWorldPoint(current.mousePosition);

                    // Use this if something gets sketchy on bigger displays?
                    //EditorGUIUtility.pixelsPerPoint;

                    Pivot.position += (prevPoint - newPoint);
                    Event.current.Use();
                }
            }

            if (current.type == EventType.ScrollWheel)
            {
                CameraDistance += CameraDistance * current.delta.y * 0.015f * 3f;
                UpdateCameraDistance();
                Event.current.Use();
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.F)
            {
                Frame();
            }

            PivotPosition = Pivot.position;
        }

        private void UpdateCameraDistance()
        {
            PreviewScene.Camera.transform.localPosition = new Vector3(0, 0, -CameraDistance);
        }

        public void Frame()
        {
            CameraDistance = TargetBounds.extents.magnitude * 2f;
            Pivot.position = TargetBounds.center;
            UpdateCameraDistance();
        }
    }
}