using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.IMGUI.Controls;
using System;

namespace ZeludeEditor
{
	public class BlendShapesSubEditor : VisualElement
	{
		public BlendShapesList List { get; private set; }
		private Toggle _combinedToggle;
		private Toggle _sortingToggle;
		private ToolbarSearchField _searchField;

		public BlendShapesSubEditor()
		{
			var toolbar = new Toolbar();
			_combinedToggle = new ToolbarToggle();
			_combinedToggle.text = "C";
			_combinedToggle.RegisterValueChangedCallback(HandleCombinedToggleValueChanged);
			_sortingToggle = new ToolbarToggle();
			var image = new Image();
			image.image = EditorGUIUtility.Load("d_AlphabeticalSorting") as Texture;
			_sortingToggle.Add(image);
			_searchField = new ToolbarSearchField();
			_searchField.AddToClassList("toolbar-filler");
			_searchField.RegisterValueChangedCallback(HandleSearchFieldChanged);
			toolbar.Add(_combinedToggle);
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

		private void HandleSearchFieldChanged(ChangeEvent<string> evt)
		{
			if (List != null)
				List.searchString = evt.newValue;
		}

		private void HandleCombinedToggleValueChanged(ChangeEvent<bool> evt)
		{
			if (List != null)
				List.ShowCombined = evt.newValue;
		}

		public void SetList(BlendShapesList list)
		{
			List = list;
			_combinedToggle.SetValueWithoutNotify(list.ShowCombined);
			_sortingToggle.SetValueWithoutNotify(list.SortAlphabetically);
			_searchField.SetValueWithoutNotify(list.searchString);
		}

		private void OnBlendShapesGUI()
		{
			List?.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true)));
		}
	}
}