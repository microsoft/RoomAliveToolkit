using UnityEngine;
using UnityEditor;
using System.Collections;

namespace RoomAliveToolkit
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(RATCalibrationData))]
    public class CalibrationDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            RATCalibrationData data = (RATCalibrationData)target;

            DrawDefaultInspector();

            if (data.calibration != null && GUILayout.Button("Reload Calibration Data"))
            {
                data.LoadAsset();
            }
            EditorGUILayout.LabelField("Loaded: " + data.IsLoaded);
        }

    }
}