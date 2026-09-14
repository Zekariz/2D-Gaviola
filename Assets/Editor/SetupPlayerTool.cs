using UnityEngine;
using UnityEditor;
using Gaviola.Player;

public class SetupPlayerTool
{
    [MenuItem("Tools/Auto-Setup Player")]
    public static void CreatePlayer()
    {
        if (GameObject.Find("Player") != null)
        {
            Debug.LogWarning("Player already exists in the scene!");
            return;
        }

        // Create main Player object
        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;

        // Add components
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Freeze Z
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f); // Default size

        // Add Animator
        player.AddComponent<Animator>();
        
        // Add PlayerController
        PlayerController pc = player.AddComponent<PlayerController>();

        // Create Sprite child
        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(player.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5; // Put it in front

        // Create GroundCheck child
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);

        // Assign GroundCheck to PlayerController using SerializedObject (since the field is private)
        SerializedObject so = new SerializedObject(pc);
        SerializedProperty groundCheckProp = so.FindProperty("groundCheck");
        if (groundCheckProp != null)
        {
            groundCheckProp.objectReferenceValue = groundCheck.transform;
            so.ApplyModifiedProperties();
        }

        // Select the player in the editor so the user sees it immediately
        Selection.activeGameObject = player;
        Undo.RegisterCreatedObjectUndo(player, "Auto-Setup Player");
        
        Debug.Log("Player successfully auto-generated!");
    }
}
