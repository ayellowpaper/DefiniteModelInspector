using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using UnityEditor.IMGUI.Controls;
using System;

namespace ZeludeEditor
{
    public class MeshPreviewEditorWindow : EditorWindow
    {
        [SerializeField]
        private string _guidString;

        const float GizmoLineLength = 0.02f;

        private string _assetPath;
        private GameObject _sourceGO;
        private GameObject _previewGO;
        private GameObject _ground;
        private ModelImporter _modelImporter;
        private Editor _modelImporterEditor;
        private PreviewScene _previewScene;
        private MeshGroup _meshGroup;
        private MeshGroupHierarchy _hierarchy;
        private Material _handleMat;
        private RenderTexture _uvTexture;

        private Image _uvImage;

        [SerializeField]
        private TreeViewState _treeViewState = new TreeViewState();
        [SerializeField]
        private PreviewSceneMotion _previewSceneMotion;
        [SerializeField]
        private MeshPreviewSettings _meshPreviewSettings;

        private static Dictionary<string, MeshPreviewEditorWindow> _registeredEditors = new Dictionary<string, MeshPreviewEditorWindow>();

        [System.Serializable]
        public class MeshPreviewSettings
        {
            public bool ShowVertices = false;
            public bool ShowNormals = false;
            public bool ShowTangents = false;
            public bool ShowBinormals = false;
            public bool ShowGrid = true;
            public bool ShowGround = true;
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
                var window = CreateWindow<MeshPreviewEditorWindow>(new Type[] { typeof(MeshPreviewEditorWindow), typeof(SceneView) });
                window._guidString = guidString;
                window.Initialize();
                window.Focus();
            }
            return true;
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

        private void Initialize()
        {
            _assetPath = AssetDatabase.GUIDToAssetPath(_guidString);
            _sourceGO = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
            _modelImporter = AssetImporter.GetAtPath(_assetPath) as ModelImporter;
            _modelImporterEditor = Editor.CreateEditor(_modelImporter);

            titleContent = new GUIContent(_sourceGO.name, AssetPreview.GetAssetPreview(_sourceGO));
            _previewScene = new PreviewScene();
            _previewGO = Instantiate(_sourceGO);
            _previewGO.name = _sourceGO.name;
            _previewGO.transform.position = Vector3.zero;
            _previewScene.AddGameObject(_previewGO);
            _previewScene.OnDrawHandles += DrawHandles;

            var bounds = CalculateBounds(_previewGO);

            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _previewScene.AddGameObject(_ground);
            _ground.transform.position = new Vector3(0, bounds.center.y - bounds.extents.y, 0);

            _previewSceneMotion = new PreviewSceneMotion(_previewScene);
            _previewSceneMotion.TargetBounds = bounds;
            _previewSceneMotion.Frame();

            _meshPreviewSettings = new MeshPreviewSettings();

            _meshGroup = new MeshGroup(_previewGO);

            _ground.SetActive(_meshPreviewSettings.ShowGround);

            var shader = Shader.Find("Zelude/Handles Lines");
            _handleMat = new Material(shader);
            _handleMat.SetInt("_HandleZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            _handleMat.hideFlags = HideFlags.HideAndDontSave;

            _registeredEditors.Add(_guidString, this);

            _hierarchy = new MeshGroupHierarchy(_meshGroup, _treeViewState);

            // CREATE UXML HERE
            string path = "Packages/com.zelude.meshpreview/Assets/UXML/ModelPreviewEditorWindow.uxml";
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            var uxml = template.Instantiate();
            uxml.style.flexGrow = 1;
            rootVisualElement.Add(uxml);

            void BindToggle(BaseField<bool> toggle, Func<bool> getter, Action<bool> setter)
            {
                toggle.value = getter();
                toggle.RegisterValueChangedCallback(x => setter(x.newValue));
            }

            void UpdateToolbarImage(Image image, Texture texture)
            {
                image.image = texture;
                image.scaleMode = ScaleMode.ScaleToFit;
            }

            var gridTexture = EditorGUIUtility.LoadRequired("d_GridAxisY") as Texture;
            var groundTexture = Resources.Load<Texture>("MeshPreview/Ground");
            uxml.Query<Image>(className: "ground-image").ForEach(img => UpdateToolbarImage(img, groundTexture));
            uxml.Query<Image>(className: "grid-image").ForEach(img => UpdateToolbarImage(img, gridTexture));
            var imguiViewport = uxml.Q<IMGUIContainer>(name: "viewport");
            imguiViewport.cullingEnabled = false;
            imguiViewport.contextType = ContextType.Editor;
            imguiViewport.onGUIHandler = OnViewportGUI;

            var imguiHierarchy = uxml.Q<IMGUIContainer>(name: "hierarchy");
            imguiHierarchy.cullingEnabled = false;
            imguiHierarchy.contextType = ContextType.Editor;
            imguiHierarchy.onGUIHandler = OnHierarchyGUI;

            BindToggle(uxml.Q<BaseField<bool>>("toggle-vertices"), () => _meshPreviewSettings.ShowVertices, x => _meshPreviewSettings.ShowVertices = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-normals"), () => _meshPreviewSettings.ShowNormals, x => _meshPreviewSettings.ShowNormals = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-tangents"), () => _meshPreviewSettings.ShowTangents, x => _meshPreviewSettings.ShowTangents = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-binormals"), () => _meshPreviewSettings.ShowBinormals, x => _meshPreviewSettings.ShowBinormals = x);

            BindToggle(uxml.Q<BaseField<bool>>("toggle-grid"), () => _meshPreviewSettings.ShowGrid, x => _meshPreviewSettings.ShowGrid = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-ground"), () => _meshPreviewSettings.ShowGround, x => _meshPreviewSettings.ShowGround = x);

            uxml.Q("viewport-stats").Add(CreateStatsRow("Objects", "object-count"));
            uxml.Q("viewport-stats").Add(CreateStatsRow("Vertices", "vertex-count"));
            uxml.Q("viewport-stats").Add(CreateStatsRow("Tris", "tri-count"));

            UpdateStatsLabel();
            ToggleUVWindow(false);
            uxml.Q<BaseField<bool>>("toggle-uv").RegisterValueChangedCallback(x => ToggleUVWindow(x.newValue));
        }

        private void UpdateStatsLabel()
        {
            DrawInfoLine("Objects", string.Format("{0:n0}/{0:n0}", _meshGroup.MeshInfos.Length));
            DrawInfoLine("Vertices", string.Format("{0:n0}/{0:n0}", _meshGroup.GetVertexCount()));
            DrawInfoLine("Tris", string.Format("{0:n0}/{0:n0}", _meshGroup.GetTriCount()));

            rootVisualElement.Q<Label>("object-count").text = string.Format("{0:n0}/{0:n0}", _meshGroup.MeshInfos.Length);
            rootVisualElement.Q<Label>("vertex-count").text = string.Format("{0:n0}/{0:n0}", _meshGroup.GetVertexCount());
            rootVisualElement.Q<Label>("tri-count").text = string.Format("{0:n0}/{0:n0}", _meshGroup.GetTriCount());
        }

        private VisualElement CreateStatsRow(string labelText, string valueXmlName)
        {
            var statsRow = new VisualElement();
            statsRow.AddToClassList("row");
            var label = new Label(labelText);
            label.AddToClassList("row__label");
            var value = new Label();
            value.name = valueXmlName;
            value.AddToClassList("row__value");
            statsRow.Add(label);
            statsRow.Add(value);

            statsRow.pickingMode = PickingMode.Ignore;
            label.pickingMode = PickingMode.Ignore;
            value.pickingMode = PickingMode.Ignore;

            return statsRow;
        }

        private void ToggleUVWindow(bool flag)
        {
            rootVisualElement.Q("uv-window").visible = flag;
            if (flag == false) return;

            int width = 400;
            int height = 400;
            if (_uvTexture == null) _uvTexture = CreateRenderTexture(width, height);
            var uvImage = rootVisualElement.Q<Image>("uv-image");
            uvImage.image = _uvTexture;
            uvImage.style.width = width;
            uvImage.style.height = height;
        }

        private void OnViewportGUI()
        {
            _ground.SetActive(_meshPreviewSettings.ShowGround);

            var viewportRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true));

            _previewSceneMotion.DoOnGUI(viewportRect);

            _previewScene.OnGUI(viewportRect);
        }

        private void OnHierarchyGUI()
        {
            _hierarchy.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true)));
        }

        private void Cleanup()
        {
            if (_previewScene != null) _previewScene.Dispose();
            _registeredEditors.Remove(_guidString);
            if (_uvTexture != null) DestroyImmediate(_uvTexture);
            //if (_modelImporterEditor != null) DestroyImmediate(_modelImporterEditor);
        }

        private void MeshDetailsGUI(int id)
        {
            DrawInfoLine("Objects", string.Format("{0:n0}/{0:n0}", _meshGroup.MeshInfos.Length));
            DrawInfoLine("Vertices", string.Format("{0:n0}/{0:n0}", _meshGroup.GetVertexCount()));
            DrawInfoLine("Tris", string.Format("{0:n0}/{0:n0}", _meshGroup.GetTriCount()));
        }

        private void DrawUVsGUI(int id)
        {
            if (_uvTexture == null) _uvTexture = CreateRenderTexture(400, 400);
            var rect = GUILayoutUtility.GetRect(400, 400);
            //EditorGUI.DrawPreviewTexture(rect, _renderTexture);
            Graphics.DrawTexture(rect, _uvTexture, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, GUI.color);
        }

        private RenderTexture CreateRenderTexture(int width, int height)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            renderTexture.hideFlags = HideFlags.HideAndDontSave;
            renderTexture.antiAliasing = 1;
            renderTexture.autoGenerateMips = false;
            var prevTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Vector2 multiplier = new Vector2(width, height);

            GL.Clear(true, true, new Color(1, 1, 1, 0));

            GL.PushMatrix();
            GL.modelview = Matrix4x4.TRS(new Vector3(0, 0, -1), Quaternion.Euler(0, 0, 0), Vector3.one);
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, 1, 0, 1, 0.01f, 10f));

            _handleMat.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.white);

            foreach (var meshInfo in _meshGroup.MeshInfos)
            {
                if (!meshInfo.IsVisible) continue;

                var tris = meshInfo.Triangles;
                var availableChannels = meshInfo.GetAvailableUVChannels();
                foreach (var channel in availableChannels)
                {
                    var uvs = meshInfo.GetUVs(channel);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        int i0 = i;
                        int i1 = i + 1;
                        int i2 = i + 2;
                        GL.Vertex(uvs[tris[i0]]);
                        GL.Vertex(uvs[tris[i1]]);

                        GL.Vertex(uvs[tris[i1]]);
                        GL.Vertex(uvs[tris[i2]]);

                        GL.Vertex(uvs[tris[i2]]);
                        GL.Vertex(uvs[tris[i0]]);
                    }
                }
            }

            GL.End();

            GL.PopMatrix();

            RenderTexture.active = prevTexture;
            return renderTexture;
        }

        private void DrawInfoLine(string label, string text)
        {
            //const int spacing = 65;
            //var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropShadowLabelStyle);
            //EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, spacing, rect.height), EditorGUIUtility.TrTextContent(label), Styles.DropShadowLabelStyle);
            //EditorGUI.DropShadowLabel(new Rect(rect.x + spacing, rect.y, rect.width - spacing, rect.height), EditorGUIUtility.TrTextContent(text), Styles.DropShadowLabelStyle);
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
            var vertices = _meshGroup.GetVertexEnumerator();
            var normals = _meshGroup.GetNormalsEnumerator();

            while (vertices.MoveNext())
            {
                normals.MoveNext();
                GL.Vertex(vertices.Current);
                GL.Vertex(vertices.Current + normals.Current * GizmoLineLength);
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

            var vertices = _meshGroup.GetVertexEnumerator();
            while (vertices.MoveNext())
            {
                DrawVertex(vertices.Current);
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
            var vertices = _meshGroup.GetVertexEnumerator();
            var tangents = _meshGroup.GetTangentsEnumerator();

            while (vertices.MoveNext())
            {
                tangents.MoveNext();
                GL.Vertex(vertices.Current);
                GL.Vertex(vertices.Current + tangents.Current * GizmoLineLength);
            }
            GL.End();
        }

        private void DrawBinormals()
        {
            _handleMat.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.yellow);
            var vertices = _meshGroup.GetVertexEnumerator();
            var binormals = _meshGroup.GetBinormalsEnumerator();

            while (vertices.MoveNext())
            {
                binormals.MoveNext();
                GL.Vertex(vertices.Current);
                GL.Vertex(vertices.Current + binormals.Current * GizmoLineLength);
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