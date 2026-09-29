using UnityEditor;
using UnityEngine;
using YourGame.Core;

public class SetupBackgroundMusic
{
    private const string ClipPath = "Assets/Audio/Music/LittlerootTown.mp3";

    [MenuItem("Tools/2D-Gaviola/Setup Background Music")]
    public static void Setup()
    {
        // Find or create the GameManager GameObject
        GameObject gm = GameObject.Find("GameManager");
        if (gm == null)
        {
            gm = new GameObject("GameManager");
            Debug.Log("[SetupBackgroundMusic] Created GameManager GameObject.");
        }

        // Add AudioSource if missing
        AudioSource audioSource = gm.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gm.AddComponent<AudioSource>();
            Debug.Log("[SetupBackgroundMusic] Added AudioSource.");
        }

        // Add BackgroundMusic script if missing
        BackgroundMusic bgm = gm.GetComponent<BackgroundMusic>();
        if (bgm == null)
        {
            bgm = gm.AddComponent<BackgroundMusic>();
            Debug.Log("[SetupBackgroundMusic] Added BackgroundMusic component.");
        }

        // Load and assign the clip via SerializedObject
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogError(
                "[SetupBackgroundMusic] Could not find audio clip at " + ClipPath +
                ". Make sure LittlerootTown.mp3 is in Assets/Audio/Music/ and Unity has imported it.");
            return;
        }

        SerializedObject so   = new SerializedObject(bgm);
        SerializedProperty clipProp   = so.FindProperty("_musicClip");
        SerializedProperty volumeProp = so.FindProperty("_volume");

        if (clipProp != null)
        {
            clipProp.objectReferenceValue = clip;
            Debug.Log("[SetupBackgroundMusic] Assigned LittlerootTown.mp3 to BackgroundMusic.");
        }

        if (volumeProp != null)
            volumeProp.floatValue = 0.5f;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gm);

        // Save the scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupBackgroundMusic] Done. Hit Play — Littleroot Town will loop automatically.");
    }
}
