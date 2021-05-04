using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System.Linq;

namespace ZeludeEditor
{
    public class MeshGroup
    {
        public readonly MeshGroupNode Root;
        public readonly MeshInfo[] MeshInfos;

        public MeshGroup(GameObject objectRoot)
        {
            Root = ConstructTree(objectRoot);
            List<MeshInfo> meshInfos = new List<MeshInfo>();
            foreach (var node in Root)
                if (node.MeshInfo != null)
                    meshInfos.Add(node.MeshInfo);
            MeshInfos = meshInfos.ToArray();
        }

        private MeshGroupNode ConstructTree(GameObject gameObject)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            MeshInfo.CreateFromRenderer(renderer, out MeshInfo meshInfo);
            var node = new MeshGroupNode(gameObject, meshInfo);
            foreach (Transform child in gameObject.transform)
            {
                node.AddChild(ConstructTree(child.gameObject));
            }
            return node;
        }

        public int GetVertexCount()
        {
            return MeshInfos.Sum(meshInfo => meshInfo.Vertices.Length);
        }

        public int GetTriCount()
        {
            return MeshInfos.Sum(meshInfo => meshInfo.Triangles.Length) / 3;
        }

        public int GetSubMeshCount()
        {
            return MeshInfos.Sum(meshInfo => meshInfo.SubMeshCount);
        }

        public IEnumerator<Vector3> GetVertexEnumerator()
        {
            return GetPositionEnumerator(x => x.Vertices);
        }

        public IEnumerator<Vector3> GetNormalsEnumerator()
        {
            return GetDirectionEnumerator(x => x.Normals);
        }

        public IEnumerator<Vector3> GetTangentsEnumerator()
        {
            return GetDirectionEnumerator(x => x.Tangents);
        }

        public IEnumerator<Vector3> GetBinormalsEnumerator()
        {
            return GetDirectionEnumerator(x => x.Binormals);
        }

        private IEnumerator<Vector3> GetPositionEnumerator(Func<MeshInfo, IList<Vector3>> func, bool onlyVisible = true)
        {
            foreach (var meshInfo in MeshInfos)
            {
                if (onlyVisible && !meshInfo.IsVisible) continue;

                foreach (var data in func(meshInfo))
                {
                    yield return meshInfo.Transform.TransformPoint(data);
                }
            }
        }

        private IEnumerator<Vector3> GetDirectionEnumerator(Func<MeshInfo, IList<Vector3>> func, bool onlyVisible = true)
        {
            foreach (var meshInfo in MeshInfos)
            {
                if (onlyVisible && !meshInfo.IsVisible) continue;

                foreach (var data in func(meshInfo))
                {
                    yield return meshInfo.Transform.TransformDirection(data);
                }
            }
        }
    }

    public class MeshGroupNode : IEnumerable<MeshGroupNode>
    {
        public readonly GameObject GameObject;
        public readonly MeshInfo MeshInfo;
        public IReadOnlyList<MeshGroupNode> Children => _children;

        private List<MeshGroupNode> _children = new List<MeshGroupNode>();

        public void AddChild(MeshGroupNode child)
        {
            _children.Add(child);
        }

        public IEnumerator<MeshGroupNode> GetEnumerator()
        {
            return Recursive(this);
        }

        private IEnumerator<MeshGroupNode> Recursive(MeshGroupNode node)
        {
            yield return node;
            foreach (var child in node.Children)
            {
                var ite = Recursive(child);
                while (ite.MoveNext())
                    yield return ite.Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public MeshGroupNode(GameObject gameObject, MeshInfo meshInfo = null)
        {
            GameObject = gameObject;
            MeshInfo = meshInfo;
        }
    }

    public class MeshInfo
    {
        public bool IsVisible {
            get => Renderer.enabled;
            set => Renderer.enabled = value;
        }

        public IReadOnlyList<IReadOnlyList<int>> SubmeshIndices => _submeshIndices;
        public IReadOnlyList<bool> VisibleSubmeshes => _visibleSubmeshes;

        public Transform Transform => Renderer.transform;
        public readonly Renderer Renderer;
        public readonly int SubMeshCount;
        public readonly int[] Triangles;
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly Vector3[] Tangents;
        public readonly Vector3[] Binormals;

        private List<List<int>> _submeshIndices;
        private List<List<Vector2>> _uvs;
        private List<int> _availableUVs;

        private bool[] _visibleSubmeshes;

        public IReadOnlyList<int> AvailableUVs => _availableUVs;

        public MeshInfo(Renderer renderer, Mesh mesh)
        {
            Renderer = renderer;
            SubMeshCount = mesh.subMeshCount;
            _visibleSubmeshes = new bool[SubMeshCount];

            List<int> tris = new List<int>();
            _submeshIndices = new List<List<int>>(SubMeshCount);
            for (int submeshIndex = 0; submeshIndex < _submeshIndices.Count; submeshIndex++)
            {
                mesh.GetTriangles(tris, submeshIndex);
                HashSet<int> indices = new HashSet<int>(tris);
                _submeshIndices.Add(indices.ToList());
            }
            Triangles = mesh.triangles;
            Vertices = mesh.vertices;
            Normals = mesh.normals;
            var tangents = mesh.tangents;
            Tangents = new Vector3[tangents.Length];
            Binormals = new Vector3[Normals.Length];

            for (int i = 0; i < Normals.Length; i++)
            {
                Tangents[i] = tangents[i];
                Binormals[i] = Vector3.Cross(Normals[i], tangents[i]) * tangents[i].w;
            }

            _uvs = new List<List<Vector2>>(8);
            _availableUVs = new List<int>();
            List<Vector2> _temporaryList = new List<Vector2>(Vertices.Length);
            for (int i = 0; i < 8; i++)
            {
                mesh.GetUVs(i, _temporaryList);
                _uvs.Add(new List<Vector2>(_temporaryList));
                if (_temporaryList.Count > 0)
                    _availableUVs.Add(i);
            }
        }

        public static bool CreateFromRenderer(Renderer renderer, out MeshInfo meshInfo)
        {
            if (renderer is MeshRenderer)
            {
                meshInfo = new MeshInfo(renderer, renderer.GetComponent<MeshFilter>().sharedMesh);
                return true;
            }
            else if (renderer is SkinnedMeshRenderer smr)
            {
                meshInfo = new MeshInfo(renderer, smr.sharedMesh);
                return true;
            }
            meshInfo = null;
            return false;
        }

        public List<int> GetVisibleSubmeshIndices()
        {
            List<int> visibleIndices = new List<int>(_visibleSubmeshes.Length);
            for (int i = 0; i < _visibleSubmeshes.Length; i++)
                if (_visibleSubmeshes[i])
                    visibleIndices.Add(i);
            return visibleIndices;
        }
        public void SetSubmeshVisible(int submeshIndex, bool flag) => _visibleSubmeshes[submeshIndex] = flag;
        public bool IsSubmeshVisible(int submeshIndex) =>_visibleSubmeshes[submeshIndex];
        public IReadOnlyList<int> GetSubmeshVertexIndices(int index) => _submeshIndices[index];
        public IReadOnlyList<Vector2> GetUVs(int channel) => _uvs[channel];
        public bool HasUVChannel(int channel) => _uvs[channel].Count > 0;
    }
}