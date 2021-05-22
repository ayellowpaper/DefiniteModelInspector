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
		public bool ShowCombined
		{
			get => _showCombined;
			set
			{
				if (_showCombined == value) return;
				_showCombined = value;
				UpdateSerializedState();
				Reload();
			}
		}

		public bool SortAlphabetically
		{
			get => _sortAlphabetically;
			set
			{
				if (_sortAlphabetically != value) return;
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
		private bool _showCombined;
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

		private void UpdateSerializedState()
		{
			if (state is BlendShapesListState listState)
			{
				listState.ShowCombined = _showCombined;
				listState.SortAlphabetically = _sortAlphabetically;
				listState.ShowIndex = _showIndex;
			}
		}

		private void UpdateFromSerializedState()
		{
			if (state is BlendShapesListState listState)
			{
				_showCombined = listState.ShowCombined;
				_sortAlphabetically = listState.SortAlphabetically;
				_showIndex = listState.ShowIndex;
			}
		}

		public void SetRenderers(IEnumerable<SkinnedMeshRenderer> renderers)
		{
			_skinnedMeshRenderers = renderers.ToList();
		}

		protected override TreeViewItem BuildRoot() => _showCombined ? GetCombinedTree() : GetFlatTree();

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
				var content = EditorGUIUtility.TrTextContent(blendShapeItem.GetName(_showIndex), blendShapeItem.GetTooltip(_showIndex));
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
				get => BlendShapes.Sum(x => x.NormaizedWeight) / BlendShapes.Count;
				set => BlendShapes.ForEach(x => x.NormaizedWeight = value);
			}

			public BlendShapeItem(int id, int depth, BlendShape blendShape) : base(id, depth, blendShape.Name)
			{
				BlendShapes = new List<BlendShape>() { blendShape };
			}

			public string GetTooltip(bool showIndex) => String.Join(Environment.NewLine, BlendShapes.Select(x => x.GetPath(showIndex)));
			public string GetName(bool showIndex)
			{
				if (BlendShapes.Count == 1)
					return BlendShapes[0].GetName(showIndex);
				if (showIndex)
					return BlendShape.FormatName(BlendShapes[0].Name, string.Join(",", BlendShapes.Select(x => x.BlendShapeIndex.ToString())));
				return BlendShapes[0].Name;
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

			public float NormaizedWeight
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
		}
	}

	[System.Serializable]
	public class BlendShapesListState : TreeViewState
	{
		public bool ShowCombined;
		public bool SortAlphabetically;
		public bool ShowIndex;
	}
}