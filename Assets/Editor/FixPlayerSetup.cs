using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using YourGame.Gameplay.Player;

public class FixPlayerSetup
{
    private const string ControllerPath = "Assets/Animations/Player/Player.controller";

    [MenuItem("Tools/Fix Player Setup")]
    public static void Fix()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[FixPlayerSetup] Player not found in the scene!");
            return;
        }

        // 1. Add PlayerAnimatorDriver if missing
        if (player.GetComponent<PlayerAnimatorDriver>() == null)
        {
            player.AddComponent<PlayerAnimatorDriver>();
            Debug.Log("[FixPlayerSetup] Added missing PlayerAnimatorDriver.");
        }

        // 2. Assign Player.controller to the Animator if it isn't already set
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
            {
                Debug.LogWarning(
                    "[FixPlayerSetup] Player.controller not found at " + ControllerPath +
                    ". Run Tools > 2D-Gaviola > Animations > Build All Player Animations and Controller first.");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = ctrl;
                Debug.Log("[FixPlayerSetup] Assigned Player.controller to Animator.");
            }
            else
            {
                Debug.Log("[FixPlayerSetup] Animator already has a controller assigned.");
            }
        }

        // 3. Fix Ground Layer mask (set to Everything so IsGrounded works without a dedicated layer)
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            SerializedObject so = new SerializedObject(pc);
            SerializedProperty groundLayerProp = so.FindProperty("_groundLayer");
            if (groundLayerProp != null)
            {
                groundLayerProp.intValue = -1; // -1 = Everything
                so.ApplyModifiedProperties();
                Debug.Log("[FixPlayerSetup] Set Ground Layer to Everything.");
            }
            else
            {
                Debug.LogWarning("[FixPlayerSetup] Could not find '_groundLayer' property. The field may have been renamed.");
            }
        }

        // 4. Resize BoxCollider2D to better fit the character sprite (~1.6 units tall)
        BoxCollider2D col = player.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(0.8f, 1.5f);
            Debug.Log("[FixPlayerSetup] Resized BoxCollider2D to (0.8, 1.5).");
        }

        EditorUtility.SetDirty(player);
        Debug.Log("[FixPlayerSetup] Done! Re-enter Play mode to test.");
    }
}
