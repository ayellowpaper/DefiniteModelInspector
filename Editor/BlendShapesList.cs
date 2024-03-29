using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using UnityEditor.UIElements;

namespace ZeludeEditor
{
	public class BlendShapesList : TreeView
	{
		public bool ShowCollapsed
		{
			get => _showCollapsed;
			set
			{
				if (_showCollapsed == value) return;
				_showCollapsed = value;
				UpdateSerializedState();
				Reload();
				ExpandAll();
			}
		}

		public bool SortAlphabetically
		{
			get => _sortAlphabetically;
			set
			{
				if (_sortAlphabetically == value) return;
				_sortAlphabetically = value;
				UpdateSerializedState();
				Reload();
			}
		}

		public bool ShowIndex
		{
			get => _showIndex;
			set
			{
				if (_showIndex == value) return;
				_showIndex = value;
				UpdateSerializedState();
				Reload();
			}
		}

		private List<SkinnedMeshRenderer> _skinnedMeshRenderers;
		private bool _showCollapsed;
		private bool _sortAlphabetically;
		private bool _showIndex;

		public BlendShapesList(IEnumerable<SkinnedMeshRenderer> renderers, BlendShapesListState state) : base(state)
		{
			UpdateFromSerializedState();
			_skinnedMeshRenderers = renderers.ToList();
			this.getNewSelectionOverride = SelectionOverride;
			Reload();
			ExpandAll();
		}

		public void ResetBlendShapes()
		{
			foreach (var item in GetRows())
			{
				var blendShapeItem = (item as BlendShapeItem);
				blendShapeItem?.Reset();
			}
		}

		private void UpdateSerializedState()
		{
			if (state is BlendShapesListState listState)
			{
				listState.ShowCollapsed = _showCollapsed;
				listState.SortAlphabetically = _sortAlphabetically;
				listState.ShowIndex = _showIndex;
			}
		}

		private void UpdateFromSerializedState()
		{
			if (state is BlendShapesListState listState)
			{
				_showCollapsed = listState.ShowCollapsed;
				_sortAlphabetically = listState.SortAlphabetically;
				_showIndex = listState.ShowIndex;
			}
		}

		public void SetRenderers(IEnumerable<SkinnedMeshRenderer> renderers)
		{
			_skinnedMeshRenderers = renderers.ToList();
		}

		protected override TreeViewItem BuildRoot()
		{
			var item = _showCollapsed ? GetCombinedTree() : GetFlatTree();
			if (_sortAlphabetically)
				SortByDisplayNameRecursive(item);
			return item;
		}

		private TreeViewItem GetFlatTree()
		{
			int id = -1;
			var root = new TreeViewItem(++id, -1, "Root");

			foreach (var renderer in _skinnedMeshRenderers)
			{
				var mesh = renderer.sharedMesh;
				int blendShapeCount = mesh.blendShapeCount;
				if (blendShapeCount == 0) continue;
				var skinnedMeshItem = new TreeViewItem(++id, 0, renderer.name);
				root.AddChild(skinnedMeshItem);
				for (int i = 0; i < blendShapeCount; i++)
				{
					var blendShapeItem = new BlendShapeItem(++id, 1, new BlendShape(renderer, i));
					skinnedMeshItem.AddChild(blendShapeItem);
				}
			}

			return root;
		}

		private void OrderByDisplayName(TreeViewItem item)
		{
			item.children = item.children.OrderBy(x => x.displayName).ToList();
		}

		private void SortByDisplayNameRecursive(TreeViewItem item)
		{
			OrderByDisplayName(item);
			foreach (var child in item.children)
			{
				if (child.hasChildren && child.children.Count > 1)
					SortByDisplayNameRecursive(child);
			}
		}

		private TreeViewItem GetCombinedTree()
		{
			int id = -1;
			var root = new TreeViewItem(++id, -1, "Root");

			Dictionary<string, BlendShapeItem> _blendShapeItemsByName = new Dictionary<string, BlendShapeItem>();

			foreach (var renderer in _skinnedMeshRenderers)
			{
				var mesh = renderer.sharedMesh;
				int blendShapeCount = mesh.blendShapeCount;
				if (blendShapeCount == 0) continue;
				for (int i = 0; i < blendShapeCount; i++)
				{
					string name = mesh.GetBlendShapeName(i);
					var blendShape = new BlendShape(renderer, i);
					if (_blendShapeItemsByName.TryGetValue(name, out var item))
						item.BlendShapes.Add(blendShape);
					else
						_blendShapeItemsByName.Add(name, new BlendShapeItem(++id, 1, blendShape));
				}
			}

			Dictionary<int, TreeViewItem> _rendererItems = new Dictionary<int, TreeViewItem>();

			foreach (var kvp in _blendShapeItemsByName)
			{
				int compoundID = 0;
				foreach (var blendShape in kvp.Value.BlendShapes)
					compoundID = compoundID ^ blendShape.Renderer.GetInstanceID();

				if (_rendererItems.TryGetValue(compoundID, out var treeViewItem))
				{
					treeViewItem.AddChild(kvp.Value);
					continue;
				}

				string name = string.Join(" | ", kvp.Value.BlendShapes.Select(x => x.Renderer.name));

				var skinnedMeshRendererItem = new TreeViewItem(++id, 0, name);
				skinnedMeshRendererItem.AddChild(kvp.Value);
				root.AddChild(skinnedMeshRendererItem);
				_rendererItems.Add(compoundID, skinnedMeshRendererItem);
			}

			return root;
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			if (args.item is BlendShapeItem blendShapeItem)
			{
				var rect = args.rowRect;
				rect.xMin += GetContentIndent(blendShapeItem);
				EditorGUIUtility.labelWidth = 100;
				var content = EditorGUIUtility.TrTextContent(blendShapeItem.GetName(_showIndex), blendShapeItem.GetTooltip(true));
				EditorGUI.BeginChangeCheck();
				var newValue = EditorGUI.Slider(rect, content, blendShapeItem.NormalizedWeight, 0f, 1f);
				if (EditorGUI.EndChangeCheck())
				{
					foreach (var blendShape in blendShapeItem.BlendShapes)
						Undo.RecordObject(blendShape.Renderer, "Blendshape");
					blendShapeItem.NormalizedWeight = newValue;
				}
			}
			else
				base.RowGUI(args);
		}

		private List<int> SelectionOverride(TreeViewItem clickedItem, bool keepMultiSelection, bool useActionKeyAsShift) => new List<int>();

		private class BlendShapeItem : TreeViewItem
		{
			public List<BlendShape> BlendShapes { get; private set; }
			public float NormalizedWeight
			{
				get => BlendShapes.Sum(x => x.NormalizedWeight) / BlendShapes.Count;
				set => BlendShapes.ForEach(x => x.NormalizedWeight = value);
			}

			public BlendShapeItem(int id, int depth, BlendShape blendShape) : base(id, depth, blendShape.Name)
			{
				BlendShapes = new List<BlendShape>() { blendShape };
			}

			public string GetTooltip(bool showIndex) => String.Join(Environment.NewLine, BlendShapes.Select(x => x.GetPath(showIndex)));

			public string GetName(bool showIndex)
			{
				int targetIndex = BlendShapes[0].BlendShapeIndex;
				bool allIndicesEqual = !BlendShapes.Any(x => x.BlendShapeIndex != targetIndex);
				if (allIndicesEqual)
					return BlendShapes[0].GetName(showIndex);

				return showIndex ? BlendShape.FormatName(BlendShapes[0].Name, "?") : BlendShapes[0].Name;
			}

			public void Reset()
			{
				foreach (var blendshape in BlendShapes)
					blendshape.Reset();
			}
		}

		private class BlendShape
		{
			public static string Format = "[{1}] {0}";
			public readonly SkinnedMeshRenderer Renderer;
			public readonly int BlendShapeIndex;
			public readonly float MinValue;
			public readonly float MaxValue;
			public string Name => Renderer.sharedMesh.GetBlendShapeName(BlendShapeIndex);
			public string Path => Renderer.name + "/" + Name;

			public float NormalizedWeight
			{
				get => Mathf.InverseLerp(MinValue, MaxValue, Renderer.GetBlendShapeWeight(BlendShapeIndex));
				set => Renderer.SetBlendShapeWeight(BlendShapeIndex, Mathf.Lerp(MinValue, MaxValue, Mathf.Clamp01(value)));
			}

			public BlendShape(SkinnedMeshRenderer renderer, int blendShapeIndex)
			{
				Renderer = renderer;
				BlendShapeIndex = blendShapeIndex;

				var mesh = renderer.sharedMesh;
				MinValue = 0;
				MaxValue = 0;
				int frameCount = mesh.GetBlendShapeFrameCount(blendShapeIndex);
				for (int i = 0; i < frameCount; i++)
				{
					float weight = mesh.GetBlendShapeFrameWeight(blendShapeIndex, i);
					MinValue = Mathf.Min(MinValue, weight);
					MaxValue = Mathf.Max(MaxValue, weight);
				}
			}

			public static string FormatName(string name, string index)
			{
				return string.Format(Format, name, index);
			}

			public string GetName(bool withIndex)
			{
				if (!withIndex) return Name;
				return FormatName(Name, BlendShapeIndex.ToString());
			}

			public string GetPath(bool withIndex)
			{
				if (!withIndex) return Path;
				return FormatName(Path, BlendShapeIndex.ToString());
			}

			public void Reset()
			{
				NormalizedWeight = 0f;
			}
		}
	}

	[System.Serializable]
	public class BlendShapesListState : TreeViewState
	{
		public bool ShowCollapsed;
		public bool SortAlphabetically;
		public bool ShowIndex;
	}
}