using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public static class NeonMazeBuilder
{
    static NeonMazeBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (GameObject.Find("COLOURFUL MAZE WORLD") == null && !SessionState.GetBool("ColorfulMaze_AutoBuilt_v5", false))
            {
                SessionState.SetBool("ColorfulMaze_AutoBuilt_v5", true);
                GenerateSceneInternal(false);
            }
        };
    }

    [MenuItem("Neon Maze Runner/Generate Playable Maze Scene")]
    public static void Generate()
    {
        GenerateSceneInternal(true);
    }

    [MenuItem("Tools/Generate Playable Maze Scene")]
    public static void GenerateFromToolsMenu()
    {
        GenerateSceneInternal(true);
    }

    static void GenerateSceneInternal(bool showDialog)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        NeonMazeGame.currentLevel = 1;
        NeonMazeGame.totalScoreAcrossLevels = 0;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NeonMazeGame.BuildCurrentLevel();

        string scenePath = "Assets/Scenes/NeonMaze.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        RegisterSceneInBuildSettings(scenePath);
        AssetDatabase.SaveAssets();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Colourful Key Maze (5 Levels) Ready!",
                "Scene generated and saved to Assets/Scenes/NeonMaze.unity! Press PLAY to start Level 1.",
                "OK"
            );
        }
        else
        {
            Debug.Log("Colourful Key Maze (5 Levels) ready at Assets/Scenes/NeonMaze.unity! Press Play to start.");
        }
    }

    static void RegisterSceneInBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool exists = false;
        foreach (EditorBuildSettingsScene s in scenes)
        {
            if (s.path == scenePath) { exists = true; break; }
        }
        if (!exists)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
