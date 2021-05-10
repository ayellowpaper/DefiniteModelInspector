using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;

namespace ZeludeEditor
{
    public class MeshGroupHierarchy : TreeView
    {
        public readonly MeshGroup MeshGroup;

        public event System.EventHandler OnSelectionChanged;

        public HashSet<int> AvailableIDs = new HashSet<int>();

        public MeshGroupHierarchy(MeshGroup meshGroup, TreeViewState state) : base(state)
        {
            MeshGroup = meshGroup;
            Reload();
            ExpandAll();
        }

        protected override TreeViewItem BuildRoot()
        {
            AvailableIDs.Clear();
            var root = new TreeViewItem(0, -1, "Root");

            AddChild(MeshGroup.Root, root);
            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (GUI.Button(rect, "", GUIStyle.none))
            {
                this.SetSelection(new List<int>(), TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        private void AddChild(MeshGroupNode node, TreeViewItem parent)
        {
            var id = node.GameObject.GetInstanceID();
            AvailableIDs.Add(id);
            var item = new TreeViewItem(id, -1, node.GameObject.name);
            if (node.MeshInfo != null)
                item.icon = EditorGUIUtility.ObjectContent(node.MeshInfo.Renderer, typeof(Renderer)).image as Texture2D;
            else
                item.icon = EditorGUIUtility.LoadRequired("d_Transform Icon") as Texture2D;
            parent.AddChild(item);
            foreach (var child in node.Children)
            {
                AddChild(child, item);
            }
        }

        public GameObject InstanceIDToObject(int id) => EditorUtility.InstanceIDToObject(id) as GameObject;

        public IEnumerable<GameObject> GetSelectedObjects()
        {
            foreach (var id in state.selectedIDs)
                yield return InstanceIDToObject(id);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            Selection.instanceIDs = selectedIds.ToArray();
            OnSelectionChanged?.Invoke(this, System.EventArgs.Empty);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            //PR PrefabLabel

            //EditorGUI.LabelField(args.rowRect, args.label);
            base.RowGUI(args);
        }
    }
}