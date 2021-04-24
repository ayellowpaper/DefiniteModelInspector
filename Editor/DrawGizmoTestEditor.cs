using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrawGizmoTest))]
public class DrawGizmoTestEditor : Editor
{
    DrawGizmoTest _typedTarget;

    private void OnEnable()
    {
        _typedTarget = target as DrawGizmoTest;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //Skybox component = previewCamera.GetComponent<Skybox>();
        //if ((bool)component)
        //{
        //    Skybox component2 = camera.GetComponent<Skybox>();
        //    if ((bool)component2 && component2.enabled)
        //    {
        //        component.enabled = true;
        //        component.material = component2.material;
        //    }
        //    else
        //    {
        //        component.enabled = false;
        //    }
        //}

        if (_typedTarget.PreviewCamera != null)
        {
            //var skyboxComponent = _typedTarget.GetComponent<Skybox>();
            //if (skyboxComponent == null)
            //    skyboxComponent = _typedTarget.gameObject.AddComponent<Skybox>();
            //skyboxComponent.material =
            //Handles.BeginGUI();
            //Handles.DrawCamera(new Rect(200, 200, 400, 400), _typedTarget.PreviewCamera, DrawCameraMode.Normal, true);
            //Handles.EndGUI();
        }
    }

    private void OnSceneGUI()
    {
        if (_typedTarget.PreviewCamera != null)
        {
            Handles.color = Color.red;
            Handles.DrawLine(Vector3.zero, Vector3.one, 0.2f);
            //HandleUtility.PushCamera(_typedTarget.PreviewCamera);
            Handles.SetCamera(new Rect(-50, 50, 400, 400), _typedTarget.PreviewCamera);
            Handles.DrawCamera(new Rect(-50, 50, 400, 400), _typedTarget.PreviewCamera, DrawCameraMode.Textured, true);
            //HandleUtility.PopCamera(_typedTarget.PreviewCamera);
        }
    }
}
