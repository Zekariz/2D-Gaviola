using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using YourGame.Gameplay.Player;

/// <summary>
/// Fixes the Player hierarchy so all physics and logic components live on the
/// Player root, and only the SpriteRenderer lives on the Sprite child.
///
/// Run this once after the scene has drifted: Tools > Fix Player Setup
/// </summary>
public class FixPlayerSetup
{
    private const string ControllerPath = "Assets/Animations/Player/Player.controller";

    [MenuItem("Tools/Fix Player Setup")]
    public static void Fix()
    {
        // ── 1. Find Player root ───────────────────────────────────────────────
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[FixPlayerSetup] No GameObject named 'Player' in the scene.");
            return;
        }

        // ── 2. Find the Sprite child ──────────────────────────────────────────
        Transform spriteTransform = player.transform.Find("Sprite");
        if (spriteTransform == null)
        {
            Debug.LogWarning("[FixPlayerSetup] No child named 'Sprite' found under Player. Skipping Sprite-child cleanup.");
        }
        else
        {
            GameObject spriteChild = spriteTransform.gameObject;

            // Remove any stray physics/logic components from the Sprite child.
            // The Sprite child should only have Transform + SpriteRenderer.
            RemoveIfPresent<Rigidbody2D>(spriteChild,         "Rigidbody2D");
            RemoveIfPresent<BoxCollider2D>(spriteChild,        "BoxCollider2D");
            RemoveIfPresent<Animator>(spriteChild,             "Animator");
            RemoveIfPresent<PlayerController>(spriteChild,     "PlayerController");
            RemoveIfPresent<PlayerAnimatorDriver>(spriteChild, "PlayerAnimatorDriver");
        }

        // ── 3. Ensure required components exist on Player root ────────────────
        EnsureComponent<Rigidbody2D>(player);
        EnsureComponent<BoxCollider2D>(player);
        EnsureComponent<Animator>(player);
        EnsureComponent<PlayerController>(player);
        EnsureComponent<PlayerAnimatorDriver>(player);

        // ── 4. Configure Rigidbody2D ──────────────────────────────────────────
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.gravityScale         = 3f;
        rb.freezeRotation       = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Debug.Log("[FixPlayerSetup] Rigidbody2D configured (gravityScale=3, freezeRotation=true, Continuous detection).");

        // ── 5. Configure BoxCollider2D ────────────────────────────────────────
        BoxCollider2D col = player.GetComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.5f);
        Debug.Log("[FixPlayerSetup] BoxCollider2D resized to (0.8, 1.5).");

        // ── 6. Assign Player.controller to Animator ───────────────────────────
        Animator animator = player.GetComponent<Animator>();
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (ctrl == null)
        {
            Debug.LogWarning(
                "[FixPlayerSetup] Player.controller not found at " + ControllerPath +
                ". Run Tools > 2D-Gaviola > Animations > Build All Player Animations and Controller first.");
        }
        else
        {
            animator.runtimeAnimatorController = ctrl;
            Debug.Log("[FixPlayerSetup] Player.controller assigned to Animator.");
        }

        // ── 7. Wire GroundCheck to PlayerController ───────────────────────────
        PlayerController pc = player.GetComponent<PlayerController>();

        Transform groundCheckTransform = player.transform.Find("GroundCheck");
        if (groundCheckTransform == null)
        {
            // Create it if missing
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(player.transform);
            gc.transform.localPosition = new Vector3(0f, -0.75f, 0f); // half of collider height
            groundCheckTransform = gc.transform;
            Debug.Log("[FixPlayerSetup] Created missing GroundCheck child at y=-0.75.");
        }

        SerializedObject so = new SerializedObject(pc);

        SerializedProperty groundCheckProp = so.FindProperty("_groundCheck");
        if (groundCheckProp != null)
        {
            groundCheckProp.objectReferenceValue = groundCheckTransform;
            Debug.Log("[FixPlayerSetup] Assigned GroundCheck transform to PlayerController._groundCheck.");
        }
        else
        {
            Debug.LogWarning("[FixPlayerSetup] Could not find '_groundCheck' property. Script may not have compiled yet.");
        }

        // ── 8. Set Ground Layer to Everything ────────────────────────────────
        SerializedProperty groundLayerProp = so.FindProperty("_groundLayer");
        if (groundLayerProp != null)
        {
            groundLayerProp.intValue = -1; // -1 = Everything
            Debug.Log("[FixPlayerSetup] Set _groundLayer to Everything (-1).");
        }
        else
        {
            Debug.LogWarning("[FixPlayerSetup] Could not find '_groundLayer' property.");
        }

        so.ApplyModifiedProperties();

        // ── 9. Mark dirty and save the scene so changes survive reloads ───────
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveCurrentModifiedScenesWithoutAsking();

        Debug.Log("[FixPlayerSetup] Scene saved. All done — enter Play mode to test.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void RemoveIfPresent<T>(GameObject go, string label) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp != null)
        {
            Object.DestroyImmediate(comp);
            Debug.Log($"[FixPlayerSetup] Removed stray {label} from '{go.name}' child.");
        }
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
        {
            go.AddComponent<T>();
            Debug.Log($"[FixPlayerSetup] Added missing {typeof(T).Name} to '{go.name}'.");
        }
    }
}
