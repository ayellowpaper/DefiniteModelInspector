using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace ZeludeEditor
{
    public class MeshGroupHierarchy : TreeView
    {
        public readonly MeshGroup MeshGroup;

        public MeshGroupHierarchy(MeshGroup meshGroup, TreeViewState state) : base(state)
        {
            MeshGroup = meshGroup;
            Reload();
            ExpandAll();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1, "Root");

            AddChild(MeshGroup.Root, root);
            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        private void AddChild(MeshGroupNode node, TreeViewItem parent)
        {
            var item = new TreeViewItem(node.GameObject.GetInstanceID(), -1, node.GameObject.name);
            //item.icon = EditorGUIUtility.LoadRequired("d_MeshRenderer Icon") as Texture2D;
            if (node.MeshInfo != null)
            {
                item.icon = EditorGUIUtility.ObjectContent(node.MeshInfo.Renderer, typeof(Renderer)).image as Texture2D;
            }
            parent.AddChild(item);
            foreach (var child in node.Children)
            {
                AddChild(child, item);
            }
        }


        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            //PR PrefabLabel

            //EditorGUI.LabelField(args.rowRect, args.label);
            base.RowGUI(args);
        }
    }
}