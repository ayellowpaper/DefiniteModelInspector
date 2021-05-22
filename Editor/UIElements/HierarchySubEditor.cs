using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace ZeludeEditor
{
	public class HierarchySubEditor : VisualElement
	{
		public MeshGroupHierarchy MeshGroupHierarchy { get; private set; }
		private ToolbarSearchField _searchField;

		public HierarchySubEditor()
		{
			var toolbar = new Toolbar();
			_searchField = new ToolbarSearchField();
			_searchField.AddToClassList("toolbar-filler");
			_searchField.RegisterValueChangedCallback(HandleSearchFieldChanged);
			toolbar.Add(_searchField);

			var imgui = new IMGUIContainer();
			imgui.cullingEnabled = false;
			imgui.contextType = ContextType.Editor;
			imgui.onGUIHandler = HandleOnIMGUI;
			imgui.style.flexGrow = 1;

			this.Add(toolbar);
			this.Add(imgui);
		}

		public void SetMeshGroupHierarchy(MeshGroupHierarchy hierarchy)
		{
			MeshGroupHierarchy = hierarchy;
			_searchField.SetValueWithoutNotify(MeshGroupHierarchy.searchString);
		}

		private void HandleSearchFieldChanged(ChangeEvent<string> evt)
		{
			MeshGroupHierarchy.searchString = evt.newValue;
		}

		private void HandleOnIMGUI()
		{
			MeshGroupHierarchy?.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true)));
		}
	}
}