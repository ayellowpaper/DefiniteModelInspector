using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace ZeludeEditor
{
	public class BlendShapesSubEditor : VisualElement
	{
		public BlendShapesList List { get; private set; }
		private Toggle _collapsedToggle;
		private Toggle _showIndexToggle;
		private Toggle _sortingToggle;
		private ToolbarSearchField _searchField;

		public BlendShapesSubEditor()
		{
			var toolbar = new Toolbar();
			_collapsedToggle = new ToolbarToggle();
			_collapsedToggle.text = "C";
			_collapsedToggle.tooltip = "Collapse BlendShapes with the same name into one.";
			_collapsedToggle.RegisterValueChangedCallback(HandleCollapsedToggleValueChanged);

			_showIndexToggle = new ToolbarToggle();
			_showIndexToggle.text = "[i]";
			_showIndexToggle.tooltip = "Show the index of the BlendShape.";
			_showIndexToggle.RegisterValueChangedCallback(HandleShowIndexToggleValueChanged);

			_sortingToggle = new ToolbarToggle();
			var image = new Image();
			image.image = EditorGUIUtility.Load("d_AlphabeticalSorting") as Texture;
			_sortingToggle.Add(image);
			_sortingToggle.tooltip = "Sort alphabetically.";
			_sortingToggle.RegisterValueChangedCallback(HandleSortingToggleValueChanged);

			_searchField = new ToolbarSearchField();
			_searchField.AddToClassList("toolbar-filler");
			_searchField.RegisterValueChangedCallback(HandleSearchFieldValueChanged);

			toolbar.Add(_collapsedToggle);
			toolbar.Add(_showIndexToggle);
			toolbar.Add(_sortingToggle);
			toolbar.Add(_searchField);

			var blendShapesGui = new IMGUIContainer();
			blendShapesGui.cullingEnabled = false;
			blendShapesGui.contextType = ContextType.Editor;
			blendShapesGui.onGUIHandler = OnBlendShapesGUI;
			blendShapesGui.style.flexGrow = 1;

			this.Add(toolbar);
			this.Add(blendShapesGui);
		}

		private void HandleSortingToggleValueChanged(ChangeEvent<bool> evt)
		{
			if (List != null)
				List.SortAlphabetically = evt.newValue;
		}

		private void HandleShowIndexToggleValueChanged(ChangeEvent<bool> evt)
		{
			if (List != null)
				List.ShowIndex = evt.newValue;
		}

		private void HandleSearchFieldValueChanged(ChangeEvent<string> evt)
		{
			if (List != null)
				List.searchString = evt.newValue;
		}

		private void HandleCollapsedToggleValueChanged(ChangeEvent<bool> evt)
		{
			if (List != null)
				List.ShowCollapsed = evt.newValue;
		}

		public void SetList(BlendShapesList list)
		{
			List = list;
			_collapsedToggle.SetValueWithoutNotify(list.ShowCollapsed);
			_showIndexToggle.SetValueWithoutNotify(list.ShowIndex);
			_sortingToggle.SetValueWithoutNotify(list.SortAlphabetically);
			_searchField.SetValueWithoutNotify(list.searchString);
		}

		private void OnBlendShapesGUI()
		{
			List?.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true)));
		}
	}
}