using UnityEngine;
using System.Collections;

using RoomAliveToolkit;
using UnityEditor;

[CustomEditor(typeof(RATKinectPlaybackController))]
public class RATKinectPlaybackControllerEditor : Editor {

    private static string[] fsOptions = new string[]{ FileStreamingMode.None.ToString(), FileStreamingMode.Read.ToString(), FileStreamingMode.ReadPreloaded.ToString(), FileStreamingMode.Write.ToString() };

    public override void OnInspectorGUI()
    {
        RATKinectPlaybackController controller = (RATKinectPlaybackController)target;
        if (controller.clients.Count == 0) controller.ConnectToKinectClients();

        bool playMode = controller.streamingMode == FileStreamingMode.Read || controller.streamingMode == FileStreamingMode.ReadPreloaded;
        bool editorMode = !Application.isPlaying;
        bool streamActive = controller.streamingMode != FileStreamingMode.None;
        bool streamWrite = controller.streamingMode == FileStreamingMode.Write;
        bool streamRead = controller.streamingMode == FileStreamingMode.Read || controller.streamingMode == FileStreamingMode.ReadPreloaded;

        EditorGUILayout.LabelField("Connected Clients",""+controller.clients.Count);
        GUI.enabled = editorMode;
        controller.streamingMode = (FileStreamingMode)EditorGUILayout.Popup("Streaming Mode", (int)controller.streamingMode, fsOptions);
        if (streamActive)
        {
            controller.streamFolder = EditorGUILayout.TextField("Stream Folder", controller.streamFolder);
            //controller.alternateAudio = (AudioClip)EditorGUILayout.ObjectField("Alternate Audio", controller.alternateAudio, typeof(AudioClip), false);

        }
        if (streamActive)
        {
            GUI.enabled = editorMode;
            controller.startOnAwake = EditorGUILayout.ToggleLeft("Start On Awake", controller.startOnAwake);
        }
        if (playMode)
        {
            controller.loopPlayback = EditorGUILayout.ToggleLeft("Loop Playback", controller.loopPlayback);
        }
        if(streamWrite)
        {
            GUI.enabled = editorMode;
            controller.writeAudioToWav = EditorGUILayout.ToggleLeft("Write Audio to WAV", controller.writeAudioToWav);
            if (editorMode)  controller.UpdateWritingToWAV();
        }
        
        GUI.enabled = true;

        EditorGUILayout.Space();

        if (streamRead)
        {
            controller.AudioDelay = EditorGUILayout.FloatField("Audio Delay", controller.AudioDelay);
            controller.RGBDelay = EditorGUILayout.FloatField("RGB Delay", controller.RGBDelay);
            controller.trimProCamPlayback = EditorGUILayout.ToggleLeft("Trim Depth Playback", controller.trimProCamPlayback);
            GUI.enabled = streamRead && controller.trimProCamPlayback;
            EditorGUI.indentLevel++;

            controller.depthFramesTrimFromStart = EditorGUILayout.IntField("Starting Frames", controller.depthFramesTrimFromStart);
            controller.depthFramesTrimFromEnd = EditorGUILayout.IntField("Ending Frames", controller.depthFramesTrimFromEnd);

            EditorGUI.indentLevel--;
        }
        GUI.enabled = !editorMode && !controller.endOfStream;
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if(streamWrite)
        {
            if(!controller.running)
            {
                if (GUILayout.Button("Record"))
                {
                    controller.StartStream();
                }
            }
            else
            {
                if (GUILayout.Button("Pause Recording"))
                {
                    controller.Pause();
                }
            }
        }
        else if(streamRead)
        {
            if (!controller.running)
            {
                if (GUILayout.Button("Play"))
                {
                    controller.StartStream();
                }
            }
            else
            {
                if (GUILayout.Button("Pause"))
                {
                    controller.Pause();
                }
            }
            GUI.enabled = !editorMode;
            if (GUILayout.Button("Rewind"))
                controller.Restart();
        }
        
        EditorGUILayout.EndHorizontal();

        EditorUtility.SetDirty(target);
    }
}
