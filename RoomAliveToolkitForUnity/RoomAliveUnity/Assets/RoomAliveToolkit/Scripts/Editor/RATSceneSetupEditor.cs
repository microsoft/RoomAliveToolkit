using UnityEngine;
using UnityEditor;
using System.Collections;

namespace RoomAliveToolkit
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(RATSceneSetup))]
    public class RATSceneSetupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            RATSceneSetup setup = (RATSceneSetup)target;
            EditorGUILayout.LabelField("Please provide the following assets:");
            DrawDefaultInspector();

            bool ready = setup.IsCalibrationDataValid();// && setup.kinectGameObject != null && setup.projectorGameObject != null;
            if (GUILayout.Button("Use Default 3D Models"))
            {
                setup.LoadDefault3DModels();
            }
            if (GUILayout.Button("Build RoomAlive Scene"))
            {
                setup.BuildSceneComponents();
            }
            if (GUILayout.Button("Clear RoomAlive Scene"))
            {
                setup.ClearSceneComponents();
            }
            if (GUILayout.Button("Add User"))
            {
                setup.AddUser();
            }
            EditorGUILayout.TextArea((ready ? "Ready":"Not ready, please reload CalibrationData"));
        }

    }


}