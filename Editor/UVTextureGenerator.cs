using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public class UVTextureGenerator : IDisposable
    {
        public List<MeshInfo> MeshInfos = new List<MeshInfo>();
        public int UVChannelIndex = 0;

        private RenderTexture _renderTexture;
        private Material _renderMaterial;

        public RenderTexture RenderTexture => _renderTexture;

        public UVTextureGenerator()
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            _renderMaterial = new Material(shader);
            _renderMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        public bool UpdateTexture(int width, int height)
        {
            if (_renderTexture != null) UnityEngine.Object.DestroyImmediate(_renderTexture);
            _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _renderTexture.hideFlags = HideFlags.HideAndDontSave;
            _renderTexture.antiAliasing = 1;
            _renderTexture.autoGenerateMips = false;
            var prevTexture = RenderTexture.active;
            RenderTexture.active = _renderTexture;

            GL.Clear(true, true, new Color(1, 1, 1, 0));

            const float margin = 0.001f;

            GL.PushMatrix();
            GL.modelview = Matrix4x4.TRS(new Vector3(0, 0, -1), Quaternion.Euler(0, 0, 0), Vector3.one);
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(-margin, 1 + margin, -margin, 1 + margin, 0.01f, 10f));

            _renderMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.white);

            foreach (var meshInfo in MeshInfos)
            {
                if (!meshInfo.IsVisible) continue;

                var tris = meshInfo.Triangles;
                var availableChannels = meshInfo.AvailableUVs;
                if (!availableChannels.Contains(UVChannelIndex)) continue;

                var uvs = meshInfo.GetUVs(UVChannelIndex);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int i0 = i;
                    int i1 = i + 1;
                    int i2 = i + 2;
                    GL.Vertex(uvs[tris[i0]]);
                    GL.Vertex(uvs[tris[i1]]);

                    GL.Vertex(uvs[tris[i1]]);
                    GL.Vertex(uvs[tris[i2]]);

                    GL.Vertex(uvs[tris[i2]]);
                    GL.Vertex(uvs[tris[i0]]);
                }
            }

            GL.End();

            GL.PopMatrix();

            RenderTexture.active = prevTexture;
            return true;
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(_renderTexture, true);
            UnityEngine.Object.DestroyImmediate(_renderMaterial, true);
        }
    }
}