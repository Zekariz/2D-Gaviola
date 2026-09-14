using UnityEngine;
using UnityEditor;
using YourGame.Gameplay.Player;

public class FixPlayerSetup
{
    [MenuItem("Tools/Fix Player Setup")]
    public static void Fix()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        // 1. Add Animator Driver if missing
        if (player.GetComponent<PlayerAnimatorDriver>() == null)
        {
            player.AddComponent<PlayerAnimatorDriver>();
            Debug.Log("Added missing PlayerAnimatorDriver!");
        }

        // 2. Set Ground Layer in PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            SerializedObject so = new SerializedObject(pc);
            SerializedProperty groundLayerProp = so.FindProperty("_groundLayer");
            if (groundLayerProp != null)
            {
                // Set to 'Everything' (-1) or 'Default' (1) if they haven't created a specific ground layer
                groundLayerProp.intValue = -1; // -1 means Everything
                so.ApplyModifiedProperties();
                Debug.Log("Fixed Ground Layer mask so IsGrounded works!");
            }
        }

        // 3. Make sure the collider fits the sprite better
        BoxCollider2D col = player.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            // The sprites are about 1.6 units tall, so let's make the collider 1.5 tall
            col.size = new Vector2(0.8f, 1.5f);
        }

        EditorUtility.SetDirty(player);
        Debug.Log("Player fixed!");
    }
}
