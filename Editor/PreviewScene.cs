using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.Experimental.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ZeludeEditor
{
    public class PreviewScene : System.IDisposable
    {
        public readonly Scene Scene;
        public readonly Camera Camera;
        public RenderTexture RenderTexture { get; private set; }

        public event System.Action OnDrawHandles;

        private readonly List<GameObject> m_GameObjects = new List<GameObject>();

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

            var light = new GameObject().AddComponent<Light>();
            AddGameObject(light.gameObject);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, 130, 90);
            light.shadows = LightShadows.Soft;
            light.color = new Color(1, 244/255f, 214/255f);
            light.intensity = 2f;
        }

        public void Dispose()
        {
            EditorSceneManager.ClosePreviewScene(Scene);
            foreach (GameObject gameObject in m_GameObjects)
            {
                Object.DestroyImmediate(gameObject, true);
            }
            if (RenderTexture != null) Object.DestroyImmediate(RenderTexture, true);
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

        public void AddSelfManagedGO(GameObject go)
        {
            SceneManager.MoveGameObjectToScene(go, Scene);
        }

        public void OnGUI(Rect rect)
        {
            var materialProperty = typeof(EditorGUIUtility).GetProperty("GUITextureBlit2SRGBMaterial", BindingFlags.NonPublic | BindingFlags.Static);

            if (Event.current.type == EventType.Repaint)
            {
                UpdateRenderTexture((int)rect.width, (int)rect.height);
                Camera.targetTexture = RenderTexture;
                Camera.pixelRect = new Rect(0f, 0f, rect.width, rect.height);
                Camera.Render();
                DrawHandles();

                Graphics.DrawTexture(rect, RenderTexture, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, GUI.color, materialProperty.GetValue(null) as Material);
            }
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