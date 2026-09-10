
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class EditorTools : Editor {

    ///////////////////////////// SCENE SHORTCUTS

    [MenuItem("Tools/Scenes/MENU")]
    public static void OpenSceneMENU()
    {
        // Kaydedilmemiş değişiklikler olup olmadığını kontrol et
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            // Değişiklikleri kaydet
            EditorSceneManager.SaveOpenScenes();
        }

        // Sahne dosya yolunu burada belirtin
        string scenePath = "Assets/_GameSource/_Scenes/Menu.unity";
        EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("Tools/Scenes/LOADING")]
    public static void OpenSceneLoading()
    {
        // Kaydedilmemiş değişiklikler olup olmadığını kontrol et
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            // Değişiklikleri kaydet
            EditorSceneManager.SaveOpenScenes();
        }

        string scenePath = "Assets/_GameSource/_Scenes/Loading.unity";
        EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("Tools/Scenes/STUNT PARK")]
    public static void OpenSceneStuntPark()
    {
        // Kaydedilmemiş değişiklikler olup olmadığını kontrol et
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            // Değişiklikleri kaydet
            EditorSceneManager.SaveOpenScenes();
        }

        string scenePath = "Assets/_GameSource/_Scenes/Publish/StuntPark.unity";
        EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("Tools/Scenes/RAMP")]
    public static void OpenSceneRamp()
    {
        // Kaydedilmemiş değişiklikler olup olmadığını kontrol et
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            // Değişiklikleri kaydet
            EditorSceneManager.SaveOpenScenes();
        }

        string scenePath = "Assets/_GameSource/_Scenes/Publish/Ramp.unity";
        EditorSceneManager.OpenScene(scenePath);
    }


}