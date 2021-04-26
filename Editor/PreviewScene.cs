﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.Experimental.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ZeludeEditor
{
    class PreviewScene : System.IDisposable
    {
        public readonly Scene Scene;
        public readonly Camera Camera;
        public RenderTexture RenderTexture { get; private set; }

        public event System.Action OnDrawHandles;

        private readonly List<GameObject> m_GameObjects = new List<GameObject>();
        private Material _material;
        private MethodInfo _setCurrentCameraMethod;

        public PreviewScene()
        {
            Scene = EditorSceneManager.NewPreviewScene();

            GameObject camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            AddGameObject(camGO);
            Camera = camGO.GetComponent<Camera>();
            Camera.enabled = false;
            Camera.cameraType = CameraType.Preview;
            Camera.clearFlags = CameraClearFlags.Skybox;
            Camera.fieldOfView = 50f;
            Camera.farClipPlane = 100f;
            Camera.nearClipPlane = 0.01f;
            //Camera.renderingPath = RenderingPath.Forward;
            Camera.useOcclusionCulling = false;
            Camera.transform.position = new Vector3(0, 0, -10);
            Camera.scene = Scene;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            _material = new Material(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
            _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _material.SetInt("_ZWrite", 0);

            _setCurrentCameraMethod = typeof(Handles).GetMethod("Internal_SetCurrentCamera", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public void Dispose()
        {
            EditorSceneManager.ClosePreviewScene(Scene);
            foreach (GameObject gameObject in m_GameObjects)
            {
                Object.DestroyImmediate(gameObject, true);
            }
            if (RenderTexture != null) Object.DestroyImmediate(RenderTexture, true);
            if (_material != null) Object.DestroyImmediate(_material, true);
            m_GameObjects.Clear();
            OnDrawHandles = null;
        }

        public void AddGameObject(GameObject go)
        {
            if (!m_GameObjects.Contains(go))
            {
                SceneManager.MoveGameObjectToScene(go, Scene);
                m_GameObjects.Add(go);
            }
        }

        public void AddManagedGO(GameObject go)
        {
            SceneManager.MoveGameObjectToScene(go, Scene);
        }

        public void Render(Rect rect)
        {
            var materialProperty = typeof(EditorGUIUtility).GetProperty("GUITextureBlit2SRGBMaterial", BindingFlags.NonPublic | BindingFlags.Static);

            UpdateRenderTexture((int)rect.width, (int)rect.height);
            Camera.targetTexture = RenderTexture;
            Camera.pixelRect = new Rect(0f, 0f, rect.width, rect.height);
            Camera.Render();
            DrawHandles();

            Graphics.DrawTexture(rect, RenderTexture, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, GUI.color, materialProperty.GetValue(null) as Material);
        }

        private void UpdateRenderTexture(int width, int height)
        {
            int aa = Mathf.Max(1, QualitySettings.antiAliasing);
            if (RenderTexture == null || RenderTexture.width != width || RenderTexture.height != height || RenderTexture.antiAliasing != aa)
            {
                if (RenderTexture != null)
                    Object.DestroyImmediate(RenderTexture, true);
                RenderTexture = new RenderTexture(width, height, 24, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
                RenderTexture.hideFlags = HideFlags.HideAndDontSave;
                RenderTexture.antiAliasing = aa;
            }
        }

        private void DrawHandles()
        {
            if (_setCurrentCameraMethod != null) _setCurrentCameraMethod?.Invoke(null, new object[] { Camera });

            var prevTexture = RenderTexture.active;
            RenderTexture.active = RenderTexture;

            GL.PushMatrix();
            GL.modelview = Camera.worldToCameraMatrix;
            GL.LoadProjectionMatrix(Camera.projectionMatrix);
            OnDrawHandles?.Invoke();
            GL.PopMatrix();

            RenderTexture.active = prevTexture;
        }
    }
}