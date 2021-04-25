using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace ZeludeEditor
{
    public class MeshPreviewEditorWindow : EditorWindow
    {
        [SerializeField]
        private string _guidString;
        [SerializeField]
        private Quaternion _cameraRotation = Quaternion.identity;
        [SerializeField]
        private float _cameraDistance = 5f;

        private string _assetPath;
        private GameObject _sourceGO;
        private GameObject _previewGO;
        private ModelImporter _modelImporter;
        private Editor _modelImporterEditor;
        private Transform _cameraPivot;
        private PreviewScene _previewScene;

        private static Dictionary<string, MeshPreviewEditorWindow> _registeredEditors = new Dictionary<string, MeshPreviewEditorWindow>();

        public Camera Camera => _previewScene.Camera;

        [OnOpenAsset(1)]
        public static bool OpenAssetHook(int instanceID, int line, int column)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            var assetPath = AssetDatabase.GetAssetPath(obj);
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null || importer.GetType() != typeof(ModelImporter)) return false;

            var guidString = AssetDatabase.AssetPathToGUID(assetPath);

            if (_registeredEditors.ContainsKey(guidString))
            {
                _registeredEditors[guidString].Focus();
            }
            else
            {
                var window = CreateWindow<MeshPreviewEditorWindow>(new System.Type[] { typeof(SceneView), typeof(MeshPreviewEditorWindow) });
                window._guidString = guidString;
                window.Initialize();
                window.Focus();
            }
            return true;
        }

        private void Initialize()
        {
            _assetPath = AssetDatabase.GUIDToAssetPath(_guidString);
            _sourceGO = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
            //_modelImporter = AssetImporter.GetAtPath(_assetPath) as ModelImporter;
            //_modelImporterEditor = Editor.CreateEditor(_modelImporter);

            titleContent = new GUIContent(_sourceGO.name, AssetPreview.GetAssetPreview(_sourceGO));
            _previewScene = new PreviewScene();
            _previewGO = Instantiate(_sourceGO);
            _previewGO.transform.position = Vector3.zero;
            _previewScene.AddGameObject(_previewGO);
            _cameraPivot = new GameObject("Camera Pivot").transform;
            _cameraPivot.transform.position = Vector3.zero;
            _previewScene.AddGameObject(_cameraPivot.gameObject);
            _previewScene.Camera.transform.SetParent(_cameraPivot);

            //var gizmoTest = new GameObject("GizmoTest");
            //gizmoTest.AddComponent<DrawGizmoTest>();
            //_previewScene.AddGameObject(gizmoTest);

            Frame();

            _registeredEditors.Add(_guidString, this);
        }

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(_guidString)) return;
            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (_previewScene != null) _previewScene.Dispose();
            _registeredEditors.Remove(_guidString);
            //if (_modelImporterEditor != null) DestroyImmediate(_modelImporterEditor);
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Toggle(false, EditorGUIUtility.TrIconContent("SceneViewTools", "Hide or show the Component Editor Tools panel in the Scene view."), EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            var viewportRect = GUILayoutUtility.GetRect(400, 1000);

            var current = Event.current;

            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                _cameraRotation = Quaternion.AngleAxis(current.delta.y * 0.003f * 57.29578f, _cameraRotation * Vector3.right) * _cameraRotation;
                _cameraRotation = Quaternion.AngleAxis(current.delta.x * 0.003f * 57.29578f, Vector3.up) * _cameraRotation;
                _cameraPivot.rotation = _cameraRotation;

                Event.current.Use();
            }

            if (Tools.viewTool == ViewTool.Pan)
            {
                if (current.type == EventType.MouseDrag && current.button == 2)
                {
                    Vector3 GetWorldPoint(Vector2 guiPosition)
                    {
                        var plane = new Plane(-Camera.transform.forward, _cameraDistance);
                        var screenPoint = Rect.PointToNormalized(viewportRect, guiPosition);
                        screenPoint.y = 1 - screenPoint.y;
                        var ray = Camera.ViewportPointToRay(screenPoint);
                        plane.Raycast(ray, out float enter);
                        return ray.GetPoint(enter);
                    }

                    var prevPoint = GetWorldPoint(current.mousePosition - current.delta);
                    var newPoint = GetWorldPoint(current.mousePosition);

                    // Use this if something gets sketchy on bigger displays?
                    //EditorGUIUtility.pixelsPerPoint;

                    _cameraPivot.transform.position += (prevPoint - newPoint);

                    Event.current.Use();
                }
            }

            if (current.type == EventType.ScrollWheel)
            {
                _cameraDistance += _cameraDistance * current.delta.y * 0.015f * 3f;
                UpdateCameraDistance();
                Event.current.Use();
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.F)
            {
                Frame();
            }



            _previewScene.Render(viewportRect);
        }

        private void UpdateCameraDistance()
        {
            _previewScene.Camera.transform.localPosition = new Vector3(0, 0, -_cameraDistance);
        }

        private void Frame()
        {
            var bounds = CalculateBounds();
            _cameraDistance = bounds.extents.magnitude * 3f;
            _cameraPivot.transform.position = bounds.center;
            UpdateCameraDistance();
        }

        private Bounds CalculateBounds()
        {
            var renderer = _previewGO.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds();
            foreach (var r in renderer)
            {
                bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }
    }
}