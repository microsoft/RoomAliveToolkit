using UnityEditor;
using System.Collections;
using UnityEngine;

namespace RoomAliveToolkit
{
    /// <summary>
    /// Editor script that adds buttons to RATProjectionPass behaviors. 
    /// </summary>
    [CustomEditor(typeof(RATProjectionPass))]
    public class RATProjectionPassEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            RATProjectionPass control = (RATProjectionPass)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Set Static Defaults"))
            {
                control.SetDefaultStaticShaders();
            }
            if (GUILayout.Button("Set Dynamic Defaults"))
            {
                control.SetDefaultDynamicShaders();
            }

            EditorUtility.SetDirty(target);
        }
    }
}

