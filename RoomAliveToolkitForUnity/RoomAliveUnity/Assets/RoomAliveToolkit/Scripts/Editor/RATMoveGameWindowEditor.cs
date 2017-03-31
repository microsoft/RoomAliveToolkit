#define UNITY_5_1

using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple editor utility to move the game window to a desired location and account for the chrome (border) width and height
/// </summary>
public class RATMoveGameWindowEditor: EditorWindow
{
    private static string settingsDir; //needs to be set from OnEnable

    private MoveWindowDataContainer data;

    private EditorWindow gameWindow;

    private Rect targetRect = new Rect(0,0,1280,720);

    private int topBorder = 22;
    private int borderWidth = 5;


    [MenuItem("Window/Move Game Window")]
    static void Init()
    {
        var myWindow = EditorWindow.GetWindow<RATMoveGameWindowEditor>();
        Directory.CreateDirectory(settingsDir);
        myWindow.Show();
    }

    public void OnEnable()
    {
        settingsDir = Application.dataPath + "/";
    }

    public void OnDisable()
    {

    }

    public void OnDestroy()
    {
    }

    void LoadSettings()
    {
        if (data == null)
        {
            try
            {
                data = MoveWindowDataContainer.FromFile(settingsDir + "MoveGameWindow.xml");
            }
            catch (Exception)
            {
                data = new MoveWindowDataContainer();
            }
        }
    }


    private void MoveGameWindow()
    {
        if (gameWindow != null)
        {
            gameWindow.minSize = new Vector2(targetRect.width, targetRect.height + topBorder - borderWidth);
            gameWindow.maxSize = gameWindow.minSize; //if the min and max size is the same Unity does not show the border from OS

            Rect newPos = new Rect(targetRect.x, targetRect.y - topBorder, targetRect.width, targetRect.height + topBorder - borderWidth);
            gameWindow.position = newPos;
            gameWindow.ShowPopup();
        }
    }

    private EditorWindow FindGameWindow()
    {
        try
        {
#if UNITY_5_1
            return (from window in Resources.FindObjectsOfTypeAll<EditorWindow>()
                    where window.titleContent.text == "Game"
                    select window).Single();
#else
            return (from window in Resources.FindObjectsOfTypeAll<EditorWindow>()
                    where window.title == "UnityEditor.GameView"
                    select window).Single();
#endif
        }
        catch (Exception)
        {
            return null;
        }
    }

    void OnGUI()
    {
        LoadSettings();

        EditorGUILayout.Space();

        // Window coordinates

        targetRect = EditorGUILayout.RectField("Game Window Target Coordinates:", targetRect);

        //so that the data can be saved to file if desired
        data.xLoc = (int)targetRect.x; 
        data.yLoc = (int)targetRect.y; 
        data.width = (int)targetRect.width;
        data.height = (int)targetRect.height;

        if (GUILayout.Button("Move Game Window"))
            MoveGameWindow();

        // Setup instance variables
        if (!gameWindow)
        {
            gameWindow = FindGameWindow();
            if (!gameWindow)
            {
                GUILayout.Label("Please re-open the game window.");
                return;
            }
        }

        EditorGUILayout.Space();

        // File save/load
        if (GUILayout.Button("Load Coordinates"))
        {
            try
            {
                data = MoveWindowDataContainer.FromFile(settingsDir + "MoveGameWindow.xml");
                Repaint();
                targetRect = new Rect(data.xLoc, data.yLoc, data.width, data.height);
                Repaint();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error loading settings file MoveGameWindow.xml: " + e.Message);
            }
        }
        if (GUILayout.Button("Save Coordinates"))
        {
            data.Save(settingsDir + "MoveGameWindow.xml");

        }
    }
}

/// <summary>
/// Simple helper class to provide easy XML serialization capabilities.
/// To use, subclass this generic class with T as your subclass type.
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
public class XmlSerializable<T>
{
    public void Save(string filePath)
    {
        var serializer = new XmlSerializer(typeof(T));
        using (var stream = new StreamWriter(filePath))
        {
            serializer.Serialize(stream, this);
        }
    }

    public static T FromFile(string filePath)
    {
        var serializer = new XmlSerializer(typeof(T));
        using (var stream = new StreamReader(filePath))
        {
            return (T)serializer.Deserialize(stream);
        }
    }
}

/// <summary>
/// Class to hold savable settings
/// </summary>
[Serializable]
public class MoveWindowDataContainer : XmlSerializable<MoveWindowDataContainer>
{
    public int xLoc, yLoc, width, height;
}