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

        private string _assetPath;
        private GameObject _sourceGO;
        private GameObject _previewGO;
        private GameObject _floor;
        private ModelImporter _modelImporter;
        private Editor _modelImporterEditor;
        private PreviewScene _previewScene;
        private Mesh[] _meshes;
        private Vector3[] _vertices;
        private Vector3[] _normals;
        private Vector3[] _binormals;
        private Vector4[] _tangents;
        private int[] _triangles;
        private Material _handleMat;

        [SerializeField]
        private PreviewSceneMotion _previewSceneMotion;
        [SerializeField]
        private MeshPreviewSettings _meshPreviewSettings;

        private static Dictionary<string, MeshPreviewEditorWindow> _registeredEditors = new Dictionary<string, MeshPreviewEditorWindow>();

        private static class Styles
        {
            public static readonly GUIStyle DropShadowLabelStyle;
            public static readonly GUIContent ToggleVerticesContent;
            public static readonly GUIContent ToggleNormalsContent;
            public static readonly GUIContent ToggleTangentsContent;
            public static readonly GUIContent ToggleBinormalsContent;
            public static readonly GUIContent ToggleGridContent;
            public static readonly GUIContent ToggleFloorContent;

            static Styles()
            {
                DropShadowLabelStyle = new GUIStyle("PreOverlayLabel");
                DropShadowLabelStyle.alignment = TextAnchor.MiddleLeft;
                DropShadowLabelStyle.fontSize = 13;

                ToggleVerticesContent = EditorGUIUtility.TrTextContent("Vertices", "");
                ToggleNormalsContent = EditorGUIUtility.TrTextContent("Normals", "");
                ToggleTangentsContent = EditorGUIUtility.TrTextContent("Tangents", "");
                ToggleBinormalsContent = EditorGUIUtility.TrTextContent("Binormals", "");
                ToggleGridContent = EditorGUIUtility.TrIconContent("GridAxisY", "");
                ToggleFloorContent = EditorGUIUtility.TrIconContent(Resources.Load<Texture>("MEshPreview/Floor"), "");
            }
        }

        [System.Serializable]
        public class MeshPreviewSettings
        {
            public bool ShowVertices = false;
            public bool ShowNormals = false;
            public bool ShowTangents = false;
            public bool ShowBinormals = false;
            public bool ShowGrid = true;
            public bool ShowFloor = true;
        }

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
            _modelImporter = AssetImporter.GetAtPath(_assetPath) as ModelImporter;
            _modelImporterEditor = Editor.CreateEditor(_modelImporter);

            titleContent = new GUIContent(_sourceGO.name, AssetPreview.GetAssetPreview(_sourceGO));
            _previewScene = new PreviewScene();
            _previewGO = Instantiate(_sourceGO);
            _previewGO.transform.position = Vector3.zero;
            _previewScene.AddGameObject(_previewGO);
            _previewScene.OnDrawHandles += DrawHandles;

            var bounds = CalculateBounds(_previewGO);

            _floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _previewScene.AddGameObject(_floor);
            _floor.transform.position = new Vector3(0, bounds.center.y - bounds.extents.y, 0);

            _meshPreviewSettings = new MeshPreviewSettings();

            _previewSceneMotion = new PreviewSceneMotion(_previewScene);
            _previewSceneMotion.TargetBounds = bounds;
            _previewSceneMotion.Frame();

                        _floor.SetActive(_meshPreviewSettings.ShowFloor);

            var filters = _sourceGO.GetComponentsInChildren<MeshFilter>();
            List<Mesh> meshes = new List<Mesh>();
            foreach (var filter in filters)
            {
                meshes.Add(filter.sharedMesh);
            }
            _meshes = meshes.ToArray();
            _normals = _meshes[0].normals;
            _vertices = _meshes[0].vertices;
            _tangents = _meshes[0].tangents;
            _triangles = _meshes[0].triangles;
            _binormals = new Vector3[_normals.Length];
            for (int i = 0; i < _binormals.Length; i++)
            {
                _binormals[i] = Vector3.Cross(_normals[i], _tangents[i]) * _tangents[i].w;
            }

            var shader = Shader.Find("Zelude/Handles Lines");
            _handleMat = new Material(shader);
            _handleMat.SetInt("_HandleZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            _handleMat.hideFlags = HideFlags.HideAndDontSave;

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
            _meshPreviewSettings.ShowVertices = GUILayout.Toggle(_meshPreviewSettings.ShowVertices, Styles.ToggleVerticesContent, EditorStyles.toolbarButton);
            _meshPreviewSettings.ShowNormals = GUILayout.Toggle(_meshPreviewSettings.ShowNormals, Styles.ToggleNormalsContent, EditorStyles.toolbarButton);
            _meshPreviewSettings.ShowTangents = GUILayout.Toggle(_meshPreviewSettings.ShowTangents, Styles.ToggleTangentsContent, EditorStyles.toolbarButton);
            _meshPreviewSettings.ShowBinormals = GUILayout.Toggle(_meshPreviewSettings.ShowBinormals, Styles.ToggleBinormalsContent, EditorStyles.toolbarButton);
            EditorGUILayout.Space();
            _meshPreviewSettings.ShowGrid = GUILayout.Toggle(_meshPreviewSettings.ShowGrid, Styles.ToggleGridContent, EditorStyles.toolbarButton);
            EditorGUI.BeginChangeCheck();
            _meshPreviewSettings.ShowFloor = GUILayout.Toggle(_meshPreviewSettings.ShowFloor, Styles.ToggleFloorContent, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                _floor.SetActive(_meshPreviewSettings.ShowFloor);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            var viewportRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true));

            _previewSceneMotion.DoOnGUI(viewportRect);

            _previewScene.OnGUI(viewportRect);

            var testRect = new Rect(10, 30, 200, 200);
            GUI.BeginGroup(viewportRect);
            BeginWindows();
            GUILayout.Window(0, new Rect(10, 10, 200, 200), MeshDetailsGUI, GUIContent.none, GUIStyle.none);
            EndWindows();
            GUI.EndGroup();
        }

        private void MeshDetailsGUI(int id)
        {
            DrawInfoLine("Vertices", string.Format("{0:n0}/{0:n0}", _vertices.Length));
            DrawInfoLine("Tris", string.Format("{0:n0}/{0:n0}", _triangles.Length / 3));
        }

        private void DrawInfoLine(string label, string text)
        {
            const int spacing = 65;
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropShadowLabelStyle);
            EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, spacing, rect.height), EditorGUIUtility.TrTextContent(label), Styles.DropShadowLabelStyle);
            EditorGUI.DropShadowLabel(new Rect(rect.x + spacing, rect.y, rect.width - spacing, rect.height), EditorGUIUtility.TrTextContent(text), Styles.DropShadowLabelStyle);
        }

        public static Bounds CalculateBounds(GameObject go)
        {
            var renderer = go.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds();
            foreach (var r in renderer)
            {
                bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        private void DrawHandles()
        {
            if (_meshPreviewSettings.ShowGrid) DrawGrid();
            if (_meshPreviewSettings.ShowNormals) DrawNormals();
            if (_meshPreviewSettings.ShowVertices) DrawVertices();
            if (_meshPreviewSettings.ShowTangents) DrawTangents();
            if (_meshPreviewSettings.ShowBinormals) DrawBinormals();
        }

        private void DrawNormals()
        {
            _handleMat.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.red);
            for (int i = 0; i < _vertices.Length; i++)
            {
                GL.Vertex(_vertices[i]);
                GL.Vertex(_vertices[i] + _normals[i] * 0.005f);
            }
            GL.End();
        }

        private void DrawVertices()
        {
            _handleMat.SetPass(0);
            void DrawVertex(Vector3 position)
            {
                float size = GetVertexHandleSize(position, _previewScene.Camera);
                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.TRS(position, Quaternion.LookRotation(_previewScene.Camera.transform.forward), new Vector3(size, size, size)));
                GL.Begin(GL.LINE_STRIP);
                GL.Color(Color.green);
                GL.Vertex(new Vector3(-0.5f,-0.5f));
                GL.Vertex(new Vector3(-0.5f, 0.5f));
                GL.Vertex(new Vector3( 0.5f, 0.5f));
                GL.Vertex(new Vector3( 0.5f,-0.5f));
                GL.Vertex(new Vector3(-0.5f, -0.5f));
                GL.End();
                GL.PopMatrix();
            }

            for (int i = 0; i < _vertices.Length; i++)
            {
                DrawVertex(_vertices[i]);
            }
        }

        public static float GetVertexHandleSize(Vector3 position, Camera camera)
        {
            Vector3 diff = camera.transform.position - position;
            var inv = Mathf.InverseLerp(0.05f, 0.5f, diff.magnitude);
            return Mathf.Lerp(0.001f, 0.005f, inv);
        }

        private void DrawTangents()
        {
            _handleMat.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.blue);
            for (int i = 0; i < _tangents.Length; i++)
            {
                GL.Vertex(_vertices[i]);
                GL.Vertex(_vertices[i] + (Vector3) _tangents[i] * 0.005f);
            }
            GL.End();
        }

        private void DrawBinormals()
        {
            _handleMat.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.yellow);
            for (int i = 0; i < _binormals.Length; i++)
            {
                GL.Vertex(_vertices[i]);
                GL.Vertex(_vertices[i] + _binormals[i] * 0.005f);
            }
            GL.End();
        }

        private void DrawGrid()
        {
            const int lineCount = 100;
            const float lineSpace = 1f;
            const float offset = (lineCount / 2f) * lineSpace;

            _handleMat.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(new Color(126/255f, 126/255f, 125/255f));
            for (int x = 0; x < lineCount; x++)
            {
                GL.Vertex(new Vector3(x * lineSpace - offset, 0, -offset));
                GL.Vertex(new Vector3(x * lineSpace - offset, 0, offset));
            }
            for (int y = 0; y < lineCount; y++)
            {
                GL.Vertex(new Vector3(-offset, 0, y * lineSpace - offset));
                GL.Vertex(new Vector3(offset, 0, y * lineSpace - offset));
            }
            GL.End();
        }
    }
}