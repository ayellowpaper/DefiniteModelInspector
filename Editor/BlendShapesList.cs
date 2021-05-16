using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;

namespace ZeludeEditor
{
    public class BlendShapesList : TreeView
    {
        public bool ShowCombined {
            get => _showCombined;
            set {
                if (_showCombined == value) return;
                _showCombined = value;
                Reload();
            }
        }

        private List<SkinnedMeshRenderer> _skinnedMeshRenderers;
        private bool _showCombined;

        public BlendShapesList(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers, TreeViewState state, bool showCombined = false) : base(state)
        {
            _showCombined = showCombined;
            _skinnedMeshRenderers = skinnedMeshRenderers.ToList();
            this.getNewSelectionOverride = SelectionOverride;
            Reload();
            ExpandAll();
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
                    var blendShapeItem = new BlendShapeItem(++id, 1, mesh.GetBlendShapeName(i), new BlendShape(renderer, i));
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
                        _blendShapeItemsByName.Add(name, new BlendShapeItem(++id, 1, mesh.GetBlendShapeName(i), blendShape));
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
                EditorGUIUtility.labelWidth = 80;
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.Slider(rect, blendShapeItem.displayName, blendShapeItem.LerpValue, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var blendShape in blendShapeItem.BlendShapes)
                        Undo.RecordObject(blendShape.Renderer, "Blendshape");
                    blendShapeItem.Lerp(newValue);
                }
            }
            else
                base.RowGUI(args);
        }

        private List<int> SelectionOverride(TreeViewItem clickedItem, bool keepMultiSelection, bool useActionKeyAsShift) => new List<int>();

        private class BlendShapeItem : TreeViewItem
        {
            public List<BlendShape> BlendShapes { get; private set; }
            public float LerpValue { get; private set; }

            public BlendShapeItem(int id, int depth, string displayName, BlendShape blendShape) : base(id, depth, displayName) => BlendShapes = new List<BlendShape>() { blendShape };
            public BlendShapeItem(int id, int depth, string displayName, IEnumerable<BlendShape> blendShapes) : base(id, depth, displayName) => BlendShapes = new List<BlendShape>(blendShapes);

            public void Lerp(float t)
            {
                LerpValue = Mathf.Clamp01(t);
                foreach (var info in BlendShapes)
                    info.Lerp(LerpValue);
            }
        }

        private class BlendShape
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly int BlendShapeIndex;
            public readonly float MinValue;
            public readonly float MaxValue;

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

            public void Lerp(float t) => Renderer.SetBlendShapeWeight(BlendShapeIndex, Mathf.Lerp(MinValue, MaxValue, t));
        }
    }
}