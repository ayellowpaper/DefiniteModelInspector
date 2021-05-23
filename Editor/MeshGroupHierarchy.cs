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

		public HashSet<int> AvailableGameObjectIDs = new HashSet<int>();

		public MeshGroupHierarchy(MeshGroup meshGroup, TreeViewState state) : base(state)
		{
			MeshGroup = meshGroup;
			Reload();
			ExpandAll();
		}

		public override void OnGUI(Rect rect)
		{
			base.OnGUI(rect);
			if (GUI.Button(rect, "", GUIStyle.none))
			{
				this.SetSelection(new List<int>(), TreeViewSelectionOptions.FireSelectionChanged);
			}
		}

		public GameObject InstanceIDToGameObject(int id) => EditorUtility.InstanceIDToObject(id) as GameObject;

		public IEnumerable<GameObject> GetSelectedObjects()
		{
			foreach (var id in state.selectedIDs)
			{
				var unityObject = InstanceIDToGameObject(id);
				if (unityObject != null) yield return unityObject;
			}
		}

		protected override TreeViewItem BuildRoot()
		{
			AvailableGameObjectIDs.Clear();
			var root = new TreeViewItem(0, -1, "Root");

			AddChild(MeshGroup.Root, root);
			SetupDepthsFromParentsAndChildren(root);

			return root;
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			base.SelectionChanged(selectedIds);
			OnSelectionChanged?.Invoke(this, System.EventArgs.Empty);
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			bool enabled = !(args.item is SubmeshTreeViewItem);
			GUI.enabled = enabled;
			base.RowGUI(args);
		}

		private void AddChild(MeshGroupNode node, TreeViewItem parent)
		{
			var id = node.GameObject.GetInstanceID();
			AvailableGameObjectIDs.Add(id);
			var item = new TreeViewItem(id, -1, node.GameObject.name);
			if (node.MeshInfo != null)
			{
				item.icon = EditorGUIUtility.ObjectContent(node.MeshInfo.Renderer, typeof(Renderer)).image as Texture2D;
				AddSubmeshes(node, item);
			}
			else
				item.icon = EditorGUIUtility.LoadRequired("d_Transform Icon") as Texture2D;
			parent.AddChild(item);
			foreach (var child in node.Children)
			{
				AddChild(child, item);
			}
		}

		private void AddSubmeshes(MeshGroupNode node, TreeViewItem parent)
		{
			if (node.MeshInfo == null) return;

			var materials = node.MeshInfo.Renderer.sharedMaterials;

			for (int i = 0; i < node.MeshInfo.SubMeshCount; i++)
			{
				string materialName = (i < materials.Length && materials[i] != null) ? materials[i].name : "[NONE]";
				var item = new SubmeshTreeViewItem(node.GameObject.GetInstanceID() ^ (i + 1), -1, $"[{i}] {materialName}");
				item.icon = EditorGUIUtility.LoadRequired("d_Material Icon") as Texture2D;
				parent.AddChild(item);
			}
		}

		private class SubmeshTreeViewItem : TreeViewItem
		{
			public SubmeshTreeViewItem(int id, int depth, string displayName) : base(id, depth, displayName)
			{
			}
		}
	}
}