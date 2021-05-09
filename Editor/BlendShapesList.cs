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
        private List<SkinnedMeshRenderer> _skinnedMeshRenderers;

        public static bool CreateFromRenderers(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers, TreeViewState state, out BlendShapesList list)
        {
            bool hasBlendShapes = skinnedMeshRenderers.Any(renderer => renderer.sharedMesh.blendShapeCount > 0);

            if (hasBlendShapes)
            {
                list = new BlendShapesList(skinnedMeshRenderers, state);
                return true;
            }

            list = null;
            return false;
        }

        public BlendShapesList(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers, TreeViewState state) : base(state)
        {
            _skinnedMeshRenderers = skinnedMeshRenderers.ToList();
            this.getNewSelectionOverride = SelectionOverride;
            Reload();
            ExpandAll();
        }

        private List<int> SelectionOverride(TreeViewItem clickedItem, bool keepMultiSelection, bool useActionKeyAsShift)
        {
            return new List<int>();
        }

        protected override TreeViewItem BuildRoot()
        {
            int id = -1;
            var root = new TreeViewItem(++id, -1, "Root");

            foreach (var renderer in _skinnedMeshRenderers)
            {
                int blendShapeCount = renderer.sharedMesh.blendShapeCount;
                if (blendShapeCount == 0) continue;
                var skinnedMeshItem = new TreeViewItem(++id, 0, renderer.name);
                root.AddChild(skinnedMeshItem);
                for (int i = 0; i < blendShapeCount; i++)
                {
                    var blendShapeItem = new BlendShapeItem(++id, 1, renderer.sharedMesh.GetBlendShapeName(i), i, renderer);
                    skinnedMeshItem.AddChild(blendShapeItem);
                }
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
                var newValue = EditorGUI.Slider(rect, blendShapeItem.displayName, blendShapeItem.Renderer.GetBlendShapeWeight(blendShapeItem.BlendShapeIndex), blendShapeItem.MinValue, blendShapeItem.MaxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(blendShapeItem.Renderer, "Blendshape");
                    blendShapeItem.Renderer.SetBlendShapeWeight(blendShapeItem.BlendShapeIndex, newValue);
                }
            }
            else
                base.RowGUI(args);
        }

        private class BlendShapeItem : TreeViewItem
        {
            public SkinnedMeshRenderer Renderer;
            public int BlendShapeIndex;
            public float MinValue = 0;
            public float MaxValue = 100;

            public BlendShapeItem(int id, int depth, string displayName, int blendShapeIndex, SkinnedMeshRenderer renderer) : base(id, depth, displayName)
            {
                Renderer = renderer;
                BlendShapeIndex = blendShapeIndex;
            }
        }
    }
}