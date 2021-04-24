using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Reflection;
using UnityEngine.Experimental.Rendering;

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
        private PreviewRenderUtility _previewRenderer;
        private Transform _cameraPivot;

        private static Dictionary<string, MeshPreviewEditorWindow> _registeredEditors = new Dictionary<string, MeshPreviewEditorWindow>();

        private CameraPreviewTest _cameraPreviewTest;

        [OnOpenAsset(1)]
        public static bool step1(int instanceID, int line, int column)
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
            _modelImporter = AssetImporter.GetAtPath(_assetPath) as ModelImporter;
            _modelImporterEditor = Editor.CreateEditor(_modelImporter);

            titleContent = new GUIContent(_sourceGO.name, AssetPreview.GetAssetPreview(_sourceGO));
            _previewRenderer = new PreviewRenderUtility();
            _previewGO = Instantiate(_sourceGO);
            _previewRenderer.AddSingleGO(_previewGO);

            _cameraPivot = new GameObject("CameraRoot").transform;
            _cameraPivot.gameObject.AddComponent<DrawGizmoTest>();
            _cameraPivot.position = Vector3.zero;
            _previewRenderer.AddSingleGO(_cameraPivot.gameObject);
            _previewRenderer.camera.transform.SetParent(_cameraPivot.transform);

            var sceneView = SceneView.lastActiveSceneView;
            var camera = sceneView.camera;

            _previewRenderer.cameraFieldOfView = 80f;
            _previewRenderer.camera.nearClipPlane = 0.01f;
            _previewRenderer.camera.clearFlags = CameraClearFlags.Skybox;
            _previewRenderer.camera.transform.rotation = _cameraRotation;
            EditorUtility.SetCameraAnimateMaterials(_previewRenderer.camera, true);
            Frame();

            _registeredEditors.Add(_guidString, this);

            _cameraPreviewTest = new CameraPreviewTest();

            //var wireframeMat = CreateWireframeMaterial();
            //foreach (var renderer in _previewGO.GetComponentsInChildren<Renderer>())
            //{
            //    Material[] mats = renderer.sharedMaterials;
            //    for (int i = 0; i < mats.Length; i++)
            //        mats[i] = wireframeMat;
            //    renderer.materials = mats;
            //}
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
            if (_cameraPreviewTest != null) _cameraPreviewTest.Dispose();
            _registeredEditors.Remove(_guidString);
            if (_modelImporterEditor != null) DestroyImmediate(_modelImporterEditor);
            if (_previewRenderer != null) _previewRenderer.Cleanup();
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            var rect = GUILayoutUtility.GetRect(400, 1000);

            var current = Event.current;

            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    _cameraRotation = Quaternion.AngleAxis(current.delta.y * 0.003f * 57.29578f, _cameraRotation * Vector3.right) * _cameraRotation;
                    _cameraRotation = Quaternion.AngleAxis(current.delta.x * 0.003f * 57.29578f, Vector3.up) * _cameraRotation;
                    _cameraPivot.rotation = _cameraRotation;

                    Event.current.Use();
                }
            }

            if (Tools.viewTool == ViewTool.Pan)
            {
                if (current.type == EventType.MouseDrag && current.button == 2)
                {
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        Vector3 position = _previewRenderer.camera.transform.position;
                        var delta = current.delta;
                        _previewRenderer.camera.transform.position = Vector3.zero;
                        Vector3 vector = _previewRenderer.camera.transform.rotation * new Vector3(0f, 0f, _cameraDistance);
                        Vector3 position2 = _previewRenderer.camera.WorldToScreenPoint(vector);
                        position2 += new Vector3(delta.x, delta.y, 0f);
                        Vector3 result = _previewRenderer.camera.ScreenToWorldPoint(position2) - vector;
                        result *= EditorGUIUtility.pixelsPerPoint;
                        _previewRenderer.camera.transform.position = position;

                        if (current.shift)
                        {
                            result *= 4f;
                        }
                        _cameraPivot.transform.position += result;

                        Event.current.Use();
                    }
                }
            }

            if (current.type == EventType.ScrollWheel)
            {
                _cameraDistance += _cameraDistance * current.delta.y * 0.015f * 3f;
                //_cameraPivot.transform.position = bounds.center;
                _previewRenderer.camera.transform.localPosition = new Vector3(0, 0, -_cameraDistance);
                Event.current.Use();
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.F)
            {
                Frame();
            }

            //if (current.type != EventType.Repaint)
            //{
            //    Handles.SetCamera(_previewRenderer.camera);
            //}
            //else
            {
                _previewRenderer.BeginPreview(rect, GUIStyle.none);
                HandleUtility.PushCamera(_previewRenderer.camera);
                _previewRenderer.Render(true, true);
                HandleUtility.PopCamera(_previewRenderer.camera);


                //MethodInfo drawGizmosMethod = typeof(Handles).GetMethod("Internal_DoDrawGizmos", BindingFlags.NonPublic | BindingFlags.Static);
                //drawGizmosMethod?.Invoke(null, new object[] { _previewRenderer.camera });

                //var lastSceneView = SceneView.lastActiveSceneView;
                //var gridsProp = typeof(SceneView).GetProperty("sceneViewGrids", BindingFlags.NonPublic | BindingFlags.Instance);
                //var grids = gridsProp.GetValue(lastSceneView);
                //var prepareGridRenderMethod = grids.GetType().GetMethod("PrepareGridRender", BindingFlags.NonPublic | BindingFlags.Instance);
                //var drawGridParameters = prepareGridRenderMethod.Invoke(grids, new object[] { _previewRenderer.camera, _previewRenderer.camera.transform.position, _previewRenderer.camera.transform.rotation, _previewRenderer.camera.orthographicSize, _previewRenderer.camera.orthographic });

                //MethodInfo drawCameraStep1 = typeof(Handles).GetMethod("DrawCameraStep1", BindingFlags.NonPublic | BindingFlags.Static);
                //MethodInfo drawCameraStep2 = typeof(Handles).GetMethod("DrawCameraStep2", BindingFlags.NonPublic | BindingFlags.Static);
                //drawCameraStep1?.Invoke(null, new object[] { rect, _previewRenderer.camera, DrawCameraMode.TexturedWire, drawGridParameters, true });
                //drawCameraStep2?.Invoke(null, new object[] { _previewRenderer.camera, DrawCameraMode.TexturedWire, true });



                _previewRenderer.EndAndDrawPreview(rect);
            }

            if (current.type == EventType.Repaint)
            {
                //_cameraPreviewTest.Render(rect);
            }

            //_modelImporterEditor.OnInspectorGUI();
        }

        private void Frame()
        {
            var bounds = CalculateBounds();
            _cameraDistance = bounds.extents.magnitude * 3f;
            _cameraPivot.transform.position = bounds.center;
            _previewRenderer.camera.transform.localPosition = new Vector3(0, 0, -_cameraDistance);
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

        private static Material CreateWireframeMaterial()
        {
            Shader shader = Shader.Find("Custom/Wireframe");
            if (!shader)
            {
                Debug.LogWarning("Could not find the built-in Colored shader");
                return null;
            }
            Material material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
            //material.SetColor("_Color", new Color(0f, 0f, 0f, 0.3f));
            //material.SetInt("_ZWrite", 0);
            //material.SetFloat("_ZBias", -1f);
            return material;
        }
    }
}








class CameraPreviewTest : System.IDisposable
{
    private Camera m_PreviewCamera;
    private RenderTexture m_PreviewTexture;
    private int m_QualitySettingsAntiAliasing = -1;

    public Camera Camera => m_PreviewCamera;

    public CameraPreviewTest()
    {
        m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
        m_PreviewCamera.enabled = false;
        m_PreviewCamera.useOcclusionCulling = false;
        m_PreviewCamera.renderingPath = RenderingPath.Forward;
    }

    public void Dispose()
    {
        if (m_PreviewCamera != null)
        {
            Object.DestroyImmediate(m_PreviewCamera.gameObject, allowDestroyingAssets: true);
        }
    }

    public void Render(Rect rect)
    {
        MethodInfo emitMethod = typeof(Handles).GetMethod("EmitGUIGeometryForCamera", BindingFlags.NonPublic | BindingFlags.Static);
        var material = typeof(EditorGUIUtility).GetProperty("GUITextureBlit2SRGBMaterial", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo drawGizmosMethod = typeof(Handles).GetMethod("Internal_DoDrawGizmos", BindingFlags.NonPublic | BindingFlags.Static);

        var sceneView = SceneView.lastActiveSceneView;
        var camera = sceneView.camera;
        m_PreviewCamera.CopyFrom(sceneView.camera);
        if (sceneView.camera.overrideSceneCullingMask != 0)
        {
            m_PreviewCamera.overrideSceneCullingMask = sceneView.camera.overrideSceneCullingMask;
        }

        RenderTexture previewTextureWithSizeAndAA = GetPreviewTextureWithSizeAndAA((int)rect.width, (int)rect.height);
        m_PreviewCamera.targetTexture = previewTextureWithSizeAndAA;
        m_PreviewCamera.pixelRect = new Rect(0f, 0f, rect.width, rect.height);
        //Handles.EmitGUIGeometryForCamera(camera, previewCamera);
        emitMethod.Invoke(null, new object[] { camera, m_PreviewCamera });
        if (camera.usePhysicalProperties)
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = previewTextureWithSizeAndAA;
            GL.Clear(clearDepth: false, clearColor: true, Color.clear);
            RenderTexture.active = active;
        }
        //Handles.DrawGizmos(m_PreviewCamera);
        m_PreviewCamera.Render();
        Graphics.DrawTexture(rect, previewTextureWithSizeAndAA, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, GUI.color, material.GetValue(null) as Material);
        drawGizmosMethod?.Invoke(null, new object[] { m_PreviewCamera });
    }

    private RenderTexture GetPreviewTextureWithSizeAndAA(int width, int height)
    {
        int num = Mathf.Max(1, QualitySettings.antiAliasing);
        if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height || m_PreviewTexture.antiAliasing != num)
        {
            m_PreviewTexture = new RenderTexture(width, height, 24, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
            m_QualitySettingsAntiAliasing = QualitySettings.antiAliasing;
            m_PreviewTexture.antiAliasing = num;
        }
        return m_PreviewTexture;
    }
}