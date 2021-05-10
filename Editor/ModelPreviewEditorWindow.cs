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
using UnityEditor.AssetImporters;
using System.Linq;

namespace ZeludeEditor
{
    public class ModelPreviewEditorWindow : EditorWindow
    {
        [SerializeField]
        private string _guidString;

        private string _assetPath;
        private GameObject _sourceGO;
        private GameObject _previewGO;
        private GameObject _ground;
        private PreviewScene _previewScene;
        private MeshGroup _meshGroup;
        private MeshGroupHierarchy _hierarchy;
        private BlendShapesList _blendShapes;
        private Material _handleMat;
        private UVTextureGenerator _uvTexture;

        private MeshGroupDrawer _vertexDrawer;
        private MeshGroupDrawer _normalDrawer;
        private MeshGroupDrawer _binormalDrawer;
        private MeshGroupDrawer _tangentDrawer;
        private IMGUIContainer _viewport;
        public UVTextureGenerator UVTexture => _uvTexture;
        public MeshGroup MeshGroup => _meshGroup;

        [SerializeField]
        private PreviewSceneMotion _previewSceneMotion;
        [SerializeField]
        private MeshPreviewSettings _meshPreviewSettings;

        public static IReadOnlyDictionary<string, ModelPreviewEditorWindow> RegisteredEditors => _registeredEditors;

        private static Dictionary<string, ModelPreviewEditorWindow> _registeredEditors = new Dictionary<string, ModelPreviewEditorWindow>();

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
                var window = CreateWindow<ModelPreviewEditorWindow>(new Type[] { typeof(ModelPreviewEditorWindow), typeof(SceneView) });
                window._guidString = guidString;
                window.Initialize();
                window.Focus();
            }
            return true;
        }

        public static bool HasWindow(string guid) => _registeredEditors.ContainsKey(guid);
        public static bool TryGetWindowByGuid(string guid, out ModelPreviewEditorWindow result) => _registeredEditors.TryGetValue(guid, out result);

        public static ModelPreviewEditorWindow GetWindowByGuid(string guid)
        {
            _registeredEditors.TryGetValue(guid, out ModelPreviewEditorWindow value);
            return value;
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
            // bind XML first
            string path = "Packages/com.zelude.meshpreview/Assets/UXML/ModelPreviewEditorWindow.uxml";
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            var uxml = template.Instantiate();
            uxml.style.flexGrow = 1;
            rootVisualElement.Add(uxml);

            _assetPath = AssetDatabase.GUIDToAssetPath(_guidString);
            _sourceGO = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
            var modelImporter = AssetImporter.GetAtPath(_assetPath) as ModelImporter;
            if (modelImporter.sourceAvatar != null)
            {
                var renderers = _sourceGO.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    _assetPath = AssetDatabase.GetAssetPath(modelImporter.sourceAvatar);
                    _sourceGO = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
                }
            }

            titleContent = new GUIContent(_sourceGO.name, AssetPreview.GetAssetPreview(_sourceGO));
            _previewScene = new PreviewScene();
            ReloadMesh();
            _previewScene.OnDoHandles += DoHandles;

            var bounds = CalculateBounds(_previewGO);

            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _previewScene.AddGameObject(_ground);
            _ground.transform.position = new Vector3(0, bounds.center.y - bounds.extents.y, 0);

            _previewSceneMotion = new PreviewSceneMotion(_previewScene);
            _previewSceneMotion.TargetBounds = bounds;
            _previewSceneMotion.Frame();

            _meshPreviewSettings = new MeshPreviewSettings();

            _ground.SetActive(_meshPreviewSettings.ShowGround);

            var shader = Shader.Find("Hidden/Internal-Colored");
            _handleMat = new Material(shader);
            //_handleMat.SetInt("_HandleZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            _handleMat.hideFlags = HideFlags.HideAndDontSave;

            _vertexDrawer = new VertexDrawer(_meshGroup);
            _tangentDrawer = new TangentDrawer(_meshGroup);
            _normalDrawer = new NormalDrawer(_meshGroup);
            _binormalDrawer = new BinormalDrawer(_meshGroup);

            _registeredEditors.Add(_guidString, this);

            // CREATE UXML HERE
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
            var groundTexture = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.zelude.meshpreview/Assets/Icons/Ground.png");
            uxml.Query<Image>(className: "ground-image").ForEach(img => UpdateToolbarImage(img, groundTexture));
            uxml.Query<Image>(className: "grid-image").ForEach(img => UpdateToolbarImage(img, gridTexture));
            _viewport = uxml.Q<IMGUIContainer>(name: "viewport");
            _viewport.cullingEnabled = false;
            _viewport.contextType = ContextType.Editor;
            _viewport.onGUIHandler = OnViewportGUI;

            var hierarchyPane = uxml.Q("hierarchy-pane__content");
            var hierarchyGui = new IMGUIContainer();
            hierarchyPane.Add(hierarchyGui);
            hierarchyGui.cullingEnabled = false;
            hierarchyGui.contextType = ContextType.Editor;
            hierarchyGui.onGUIHandler = OnHierarchyGUI;
            hierarchyGui.style.flexGrow = 1;

            var blendShapesPane = uxml.Q("blendshapes-pane__content");
            var blendShapesGui = new IMGUIContainer();
            blendShapesPane.Add(blendShapesGui);
            blendShapesGui.cullingEnabled = false;
            blendShapesGui.contextType = ContextType.Editor;
            blendShapesGui.onGUIHandler = OnBlendShapesGUI;
            blendShapesGui.style.flexGrow = 1;

            BindToggle(uxml.Q<BaseField<bool>>("toggle-vertices"), () => _meshPreviewSettings.ShowVertices, x => _meshPreviewSettings.ShowVertices = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-normals"), () => _meshPreviewSettings.ShowNormals, x => _meshPreviewSettings.ShowNormals = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-tangents"), () => _meshPreviewSettings.ShowTangents, x => _meshPreviewSettings.ShowTangents = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-binormals"), () => _meshPreviewSettings.ShowBinormals, x => _meshPreviewSettings.ShowBinormals = x);

            BindToggle(uxml.Q<BaseField<bool>>("toggle-grid"), () => _meshPreviewSettings.ShowGrid, x => _meshPreviewSettings.ShowGrid = x);
            BindToggle(uxml.Q<BaseField<bool>>("toggle-ground"), () => _meshPreviewSettings.ShowGround, x => _meshPreviewSettings.ShowGround = x);

            uxml.Q("viewport-stats").Add(CreateStatsRow("Objects", "object-count"));
            uxml.Q("viewport-stats").Add(CreateStatsRow("Vertices", "vertex-count"));
            uxml.Q("viewport-stats").Add(CreateStatsRow("Tris", "tri-count"));

            var assetButton = uxml.Q<Button>("ping-asset");
            assetButton.text = $"Model: {_sourceGO.name}";
            assetButton.clicked += () =>
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(_sourceGO);
            };

            var selectAssetButton = uxml.Q<Button>("select-asset");
            selectAssetButton.clicked += () => { Selection.activeObject = _sourceGO; };

            UpdateStatsLabel();
            ToggleUVWindow(false);
            uxml.Q<BaseField<bool>>("toggle-uv").RegisterValueChangedCallback(x => ToggleUVWindow(x.newValue));
            uxml.Q<ToolbarMenu>("toggle-uv-menu").RegisterCallback<MouseUpEvent>(x => ShowUVPopup());
        }

        private void HandleAnimationExplorerSelectionChanged(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
            }
        }

        private void ShowUVPopup()
        {
            var rect = rootVisualElement.Q("toggle-uv").worldBound;
            UnityEditor.PopupWindow.Show(rect, new UVSettingsWindow(this));
            GUIUtility.ExitGUI();
        }

        public void ChangeUVTextureIndex(int newIndex)
        {
            rootVisualElement.Q<ToolbarToggle>("toggle-uv").text = $"UV: {newIndex}";
            rootVisualElement.Q<ToolbarToggle>("toggle-uv").SetValueWithoutNotify(true);
            _uvTexture.UVChannelIndex = newIndex;
            ToggleUVWindow(true);
        }

        public void ReloadMesh()
        {
            if (_previewGO == null)
            {
                _previewGO = PrefabUtility.InstantiatePrefab(_sourceGO) as GameObject;
                _previewGO.transform.position = Vector3.zero;
                _previewScene.AddSelfManagedGO(_previewGO);
            }

            _meshGroup = new MeshGroup(_previewGO);

            _hierarchy = new MeshGroupHierarchy(_meshGroup, new TreeViewState());

            bool hasBlendShapes = BlendShapesList.CreateFromRenderers(_previewGO.GetComponentsInChildren<SkinnedMeshRenderer>(true), new TreeViewState(), out _blendShapes);
            var detailsPane = rootVisualElement.Q<TwoPaneSplitView>("details-pane");
            if (hasBlendShapes)
                detailsPane.UnCollapse();
            else
                detailsPane.CollapseChild(1);

            _uvTexture = new UVTextureGenerator();
            foreach (var meshinfo in _meshGroup.MeshInfos)
                _uvTexture.MeshInfos.Add(meshinfo);
        }

        private void UpdateStatsLabel()
        {
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
            _uvTexture.UpdateTexture(400, 400);
            var uvImage = rootVisualElement.Q<Image>("uv-image");
            uvImage.image = _uvTexture.RenderTexture;
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

        private void OnBlendShapesGUI()
        {
            _blendShapes?.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true)));
        }

        private void OnGUI()
        {
            Repaint();
        }

        private void Cleanup()
        {
            if (_previewScene != null) _previewScene.Dispose();
            _registeredEditors.Remove(_guidString);
            if (_uvTexture != null) _uvTexture.Dispose();
            if (_previewGO != null) DestroyImmediate(_previewGO, true);
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

        private void DoHandles()
        {
            /// TODO: For some reason checking handle interaction has an offset in the y axis
            Vector2 offset = _viewport.LocalToWorld(Vector2.zero);
            offset.x = 0f;
            Event.current.mousePosition += offset;

            _handleMat.SetPass(0);
            if (_meshPreviewSettings.ShowGrid) DrawGrid();
            if (_meshPreviewSettings.ShowNormals) _normalDrawer.Draw(_previewScene.Camera);
            if (_meshPreviewSettings.ShowVertices) _vertexDrawer.Draw(_previewScene.Camera);
            if (_meshPreviewSettings.ShowTangents) _tangentDrawer.Draw(_previewScene.Camera);
            if (_meshPreviewSettings.ShowBinormals) _binormalDrawer.Draw(_previewScene.Camera);

            DoToolHandles();
            HandleViewportSelection();

            Event.current.mousePosition -= offset;
        }

        private void DoToolHandles()
        {
            var selection = _hierarchy.GetSelectedObjects();
            var count = selection.Count();
            if (count <= 0) return;
            Vector3 position = Vector3.zero;
            foreach (var go in selection)
            {
                position += go.transform.position;
            }
            position /= count;

            var rotation = Quaternion.identity;

            var tool = GetActiveManipulationTool();
            tool.DoTool(position, rotation, selection);
        }

        private void HandleViewportSelection()
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.modifiers == EventModifiers.None)
            {
                var go = HandleUtility.PickGameObject(Event.current.mousePosition, false);
                if (go == null)
                    _hierarchy.SetSelection(new List<int>(), TreeViewSelectionOptions.FireSelectionChanged);
                else
                {
                    if (_hierarchy.AvailableIDs.Contains(go.GetInstanceID()))
                        _hierarchy.SetSelection(new List<int>() { go.GetInstanceID() }, TreeViewSelectionOptions.FireSelectionChanged | TreeViewSelectionOptions.RevealAndFrame);
                    else
                        _hierarchy.SetSelection(new List<int>(), TreeViewSelectionOptions.FireSelectionChanged);
                }
            }
        }

        private ManipulationTool GetActiveManipulationTool() => Tools.current switch
        {
            Tool.Rotate => ManipulationTool.Get<RotateTool>(),
            Tool.Scale => ManipulationTool.Get<ScaleTool>(),
            _ => ManipulationTool.Get<MoveTool>(),
        };

        private void DrawGrid()
        {
            const int lineCount = 20;
            const float lineSpace = 1f;
            const float offset = (lineCount / 2f) * lineSpace;

            var color = new Color(128 / 255f, 128 / 255f, 128 / 255f, 102/ 255f);
            GL.Begin(GL.LINES);
            GL.Color(color);
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