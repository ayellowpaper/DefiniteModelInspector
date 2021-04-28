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
            var filters = objectRoot.GetComponentsInChildren<MeshFilter>();
            List<MeshInfo> meshInfos = new List<MeshInfo>();
            foreach (var filter in filters)
            {
                var meshInfo = new MeshInfo(filter);
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
            foreach (var meshInfo in MeshInfos)
            {
                foreach (var data in meshInfo.Tangents)
                {
                    yield return meshInfo.MeshFilter.transform.TransformPoint(data);
                }
            }
        }

        public IEnumerator<Vector3> GetBinormalsEnumerator()
        {
            return GetDirectionEnumerator(x => x.Binormals);
        }

        private IEnumerator<Vector3> GetPositionEnumerator(Func<MeshInfo, IList<Vector3>> func)
        {
            foreach (var meshInfo in MeshInfos)
            {
                foreach (var data in func(meshInfo))
                {
                    yield return meshInfo.MeshFilter.transform.TransformPoint(data);
                }
            }
        }

        private IEnumerator<Vector3> GetDirectionEnumerator(Func<MeshInfo, IList<Vector3>> func)
        {
            foreach (var meshInfo in MeshInfos)
            {
                foreach (var data in func(meshInfo))
                {
                    yield return meshInfo.MeshFilter.transform.TransformDirection(data);
                }
            }
        }

        private IEnumerator<T> GetMeshDataEnumerator<T>(Func<MeshInfo, IList<T>> func)
        {
            foreach (var meshInfo in MeshInfos)
            {
                foreach (var data in func(meshInfo))
                {
                    yield return data;
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
                MeshRenderer.enabled = _isVisible;
            }
        }

        public IReadOnlyList<IReadOnlyList<int>> SubmeshIndices => _submeshIndices;

        public readonly MeshFilter MeshFilter;
        public readonly MeshRenderer MeshRenderer;
        public readonly int SubMeshCount;
        public readonly int[] Triangles;
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly Vector4[] Tangents;
        public readonly Vector3[] Binormals;

        private List<List<int>> _submeshIndices;

        private bool _isVisible = true;

        public MeshInfo(MeshFilter meshFilter)
        {
            MeshFilter = meshFilter;
            var mesh = meshFilter.sharedMesh;
            SubMeshCount = mesh.subMeshCount;

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
            Tangents = mesh.tangents;
            Binormals = new Vector3[Normals.Length];

            for (int i = 0; i < Normals.Length; i++)
            {
                Binormals[i] = Vector3.Cross(Normals[i], Tangents[i]) * Tangents[i].w;
            }
        }

        public List<int> GetSubmeshVertexIndices(int index)
        {
            return _submeshIndices[index];
        }
    }
}