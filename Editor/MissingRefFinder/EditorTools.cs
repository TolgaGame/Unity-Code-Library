
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class EditorTools : Editor
{
    [MenuItem("Tools/Scenes/Menu")]
    public static void OpenSceneMENU()
    {
        // Saves first so the scene switch never discards unsaved changes
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        string scenePath = "Assets/_GameSource/_Scenes/Menu.unity";
        EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("Tools/Scenes/Loading")]
    public static void OpenSceneLoading()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        string scenePath = "Assets/_GameSource/_Scenes/Loading.unity";
        EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("Tools/Scenes/Stunt Park")]
    public static void OpenSceneStuntPark()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        string scenePath = "Assets/_GameSource/_Scenes/Publish/StuntPark.unity";
        EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("Tools/Scenes/Ramp")]
    public static void OpenSceneRamp()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        string scenePath = "Assets/_GameSource/_Scenes/Publish/Ramp.unity";
        EditorSceneManager.OpenScene(scenePath);
    }
}