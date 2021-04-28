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
        public readonly MeshInfo[] MeshInfos;

        public MeshGroup(GameObject objectRoot)
        {
            var renderers = objectRoot.GetComponentsInChildren<Renderer>();
            List<MeshInfo> meshInfos = new List<MeshInfo>();
            foreach (var renderer in renderers)
            {
                if (MeshInfo.CreateFromRenderer(renderer, out MeshInfo meshInfo))
                    meshInfos.Add(meshInfo);
            }
            MeshInfos = meshInfos.ToArray();
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

    //public class Node<T>
    //{
    //    public readonly T Target;
    //    public IReadOnlyList<Node<T>> Children => _children;

    //    private List<Node<T>> _children = new List<Node<T>>();

    //    public void AddChild(Node<T> child)
    //    {
    //        _children.Add(child);
    //    }
    //}

    public class MeshInfo
    {
        public bool IsVisible {
            get => _isVisible;
            set {
                if (_isVisible == value) return;
                _isVisible = value;
                Renderer.enabled = _isVisible;
            }
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

        private bool _isVisible = true;
        private bool[] _visibleSubmeshes;

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

        public void SetSubmeshVisible(int submeshIndex, bool flag)
        {
            _visibleSubmeshes[submeshIndex] = flag;
        }

        public bool IsSubmeshVisible(int submeshIndex)
        {
            return _visibleSubmeshes[submeshIndex];
        }

        public List<int> GetSubmeshVertexIndices(int index)
        {
            return _submeshIndices[index];
        }
    }
}