using UnityEditor;
using UnityEngine;
using YourGame.Gameplay.Coins;

public static class SetupFixer
{
    [MenuItem("Tools/Fix Coins")]
    public static void Fix()
    {
        CoinManager manager = Object.FindAnyObjectByType<CoinManager>();
        if (manager == null) {
            Debug.Log("CoinManager not found!");
            return;
        }

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/coin-collection-sound.mp3");
        if (clip != null) {
            manager.coinSound = clip;
            Debug.Log("Sound fixed!");
        } else {
            Debug.Log("Clip not found!");
        }

        Sprite sprite = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/accomplished.png"))
        {
            if (asset is Sprite s && s.name == "accomplished_83") sprite = s;
        }
        if (sprite != null) manager.coinSprite = sprite;

        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
