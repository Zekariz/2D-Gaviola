using UnityEditor;
using UnityEngine;
using YourGame.Gameplay.Player;

public class SetupDeathAnimation : Editor
{
    [InitializeOnLoadMethod]
    private static void Setup()
    {
        // Find the player in the scene
        var playerController = Object.FindAnyObjectByType<PlayerController>();
        if (playerController == null) return;

        var playerObj = playerController.gameObject;

        // Check if component is already added
        var deathAnim = playerObj.GetComponent<DeathAnimationController>();
        if (deathAnim == null)
        {
            deathAnim = playerObj.AddComponent<DeathAnimationController>();
            Debug.Log("Successfully added DeathAnimationController to the Player!");
        }
        
        // Use SerializedObject to check if sprites are assigned
        var so = new SerializedObject(deathAnim);
        var prop = so.FindProperty("_deathSprites");
        
        // If array is empty or size 0, assign them
        if (prop.arraySize != 6)
        {
            // Try to load the sprites
            Sprite[] sprites = new Sprite[6];
            bool allFound = true;
            for (int i = 0; i < 6; i++)
            {
                string path = $"Assets/Sprites/Characters/death{i}.png";
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[i] == null)
                {
                    allFound = false;
                    Debug.LogWarning($"Could not find death sprite at {path}");
                }
            }

            if (allFound)
            {
                prop.arraySize = 6;
                for (int i = 0; i < 6; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                }
                so.ApplyModifiedProperties();
                Debug.Log("Successfully assigned death0 to death5 sprites to the Player!");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerObj.scene);
            }
        }
    }
}
