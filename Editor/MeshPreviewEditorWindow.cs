using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Reflection;
using UnityEngine.Experimental.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

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
            _cameraPreviewTest = new CameraPreviewTest();
            _previewGO = Instantiate(_sourceGO);
            _cameraPreviewTest.AddGameObject(_previewGO);

            _registeredEditors.Add(_guidString, this);

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

            var rect = GUILayoutUtility.GetRect(400, 1000);

            var current = Event.current;

            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    _cameraRotation = Quaternion.AngleAxis(current.delta.y * 0.003f * 57.29578f, _cameraRotation * Vector3.right) * _cameraRotation;
                    _cameraRotation = Quaternion.AngleAxis(current.delta.x * 0.003f * 57.29578f, Vector3.up) * _cameraRotation;
                    //_cameraPivot.rotation = _cameraRotation;

                    Event.current.Use();
                }
            }

            //if (Tools.viewTool == ViewTool.Pan)
            //{
            //    if (current.type == EventType.MouseDrag && current.button == 2)
            //    {
            //        if (rect.Contains(Event.current.mousePosition))
            //        {
            //            Vector3 position = _previewRenderer.camera.transform.position;
            //            var delta = current.delta;
            //            _previewRenderer.camera.transform.position = Vector3.zero;
            //            Vector3 vector = _previewRenderer.camera.transform.rotation * new Vector3(0f, 0f, _cameraDistance);
            //            Vector3 position2 = _previewRenderer.camera.WorldToScreenPoint(vector);
            //            position2 += new Vector3(delta.x, delta.y, 0f);
            //            Vector3 result = _previewRenderer.camera.ScreenToWorldPoint(position2) - vector;
            //            result *= EditorGUIUtility.pixelsPerPoint;
            //            _previewRenderer.camera.transform.position = position;

            //            if (current.shift)
            //            {
            //                result *= 4f;
            //            }
            //            _cameraPivot.transform.position += result;

            //            Event.current.Use();
            //        }
            //    }
            //}

            //if (current.type == EventType.ScrollWheel)
            //{
            //    _cameraDistance += _cameraDistance * current.delta.y * 0.015f * 3f;
            //    //_cameraPivot.transform.position = bounds.center;
            //    _previewRenderer.camera.transform.localPosition = new Vector3(0, 0, -_cameraDistance);
            //    Event.current.Use();
            //}

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.F)
            {
                Frame();
            }



            _cameraPreviewTest.Render(rect);
        }

        private void Frame()
        {
            var bounds = CalculateBounds();
            _cameraDistance = bounds.extents.magnitude * 3f;
            _cameraPivot.transform.position = bounds.center;
            //_previewRenderer.camera.transform.localPosition = new Vector3(0, 0, -_cameraDistance);
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
    private RenderTexture _renderTexture;
    private Material _material;

    public readonly Scene Scene;
    public readonly Camera Camera;
    public RenderTexture RenderTexture => _renderTexture;

    private readonly List<GameObject> m_GameObjects = new List<GameObject>();

    public CameraPreviewTest()
    {
        Scene = EditorSceneManager.NewPreviewScene();
        Debug.Log(Scene);

        GameObject camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
        AddGameObject(camGO);
        Camera = camGO.GetComponent<Camera>();
        Camera.enabled = false;
        Camera.cameraType = CameraType.Preview;
        Camera.clearFlags = CameraClearFlags.Skybox;
        Camera.fieldOfView = 50f;
        Camera.farClipPlane = 1000f;
        Camera.nearClipPlane = 0.1f;
        Camera.renderingPath = RenderingPath.Forward;
        Camera.useOcclusionCulling = false;
        Camera.transform.position = new Vector3(0, 0, -10);
        Camera.scene = Scene;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        _material = new Material(shader);
        _material.hideFlags = HideFlags.HideAndDontSave;
        _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        _material.SetInt("_ZWrite", 0);
    }

    public void Dispose()
    {
        EditorSceneManager.ClosePreviewScene(Scene);
        foreach (GameObject gameObject in m_GameObjects)
        {
            Object.DestroyImmediate(gameObject, true);
        }
        if (RenderTexture != null) Object.DestroyImmediate(RenderTexture, true);
        if (_material != null) Object.DestroyImmediate(_material, true);
        m_GameObjects.Clear();
    }

    public void AddGameObject(GameObject go)
    {
        if (!m_GameObjects.Contains(go))
        {
            SceneManager.MoveGameObjectToScene(go, Scene);
            m_GameObjects.Add(go);
        }
    }

    public void AddManagedGO(GameObject go)
    {
        SceneManager.MoveGameObjectToScene(go, Scene);
    }

    public void Render(Rect rect)
    {
        var materialProperty = typeof(EditorGUIUtility).GetProperty("GUITextureBlit2SRGBMaterial", BindingFlags.NonPublic | BindingFlags.Static);

        UpdateRenderTexture((int)rect.width, (int)rect.height);
        Camera.targetTexture = _renderTexture;
        Camera.pixelRect = new Rect(0f, 0f, rect.width, rect.height);
        Camera.Render();
        DrawHandles();

        Graphics.DrawTexture(rect, _renderTexture, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, GUI.color, materialProperty.GetValue(null) as Material);
    }

    private void UpdateRenderTexture(int width, int height)
    {
        int aa = Mathf.Max(1, QualitySettings.antiAliasing);
        if (_renderTexture == null || _renderTexture.width != width || _renderTexture.height != height || _renderTexture.antiAliasing != aa)
        {
            if (_renderTexture != null)
                Object.DestroyImmediate(_renderTexture, true);
            _renderTexture = new RenderTexture(width, height, 24, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
            _renderTexture.hideFlags = HideFlags.HideAndDontSave;
            _renderTexture.antiAliasing = aa;
        }
    }

    private void DrawHandles()
    {
        var prevTexture = RenderTexture.active;
        RenderTexture.active = _renderTexture;

        _material.SetPass(0);

        GL.PushMatrix();
        // Set transformation matrix for drawing to
        // match our transform
        //GL.MultMatrix(transform.localToWorldMatrix);

        int lineCount = 20;
        float radius = 1f;
        // Draw lines
        GL.Begin(GL.LINES);
        for (int i = 0; i < lineCount; ++i)
        {
            float a = i / (float)lineCount;
            float angle = a * Mathf.PI * 2;
            // Vertex colors change from red to green
            GL.Color(new Color(a, 1 - a, 0, 0.8F));
            // One vertex at transform position
            GL.Vertex3(0, 0, 0);
            // Another vertex at edge of circle
            GL.Vertex3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
        GL.End();
        GL.PopMatrix();

        RenderTexture.active = prevTexture;
    }
}