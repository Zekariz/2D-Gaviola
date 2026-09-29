using UnityEditor;
using UnityEngine;
using YourGame.Gameplay.Obstacles;
using YourGame.Gameplay.Core;

namespace YourGame.Editor
{
    public class SetupCourseTool
    {
        [MenuItem("Tools/2D-Gaviola/Setup/4. Generate Obstacle Prefabs")]
        public static void GeneratePrefabs()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Obstacles"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Obstacles");
            }

            Sprite blockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/surface-block.png");

            CreateLooseCrate(blockSprite);
            CreateFallingPlatform(blockSprite);
            CreateSwingingHazard(blockSprite);
            CreateMovingPlatform(blockSprite);
            CreateFinishLine(blockSprite);

            AssetDatabase.SaveAssets();
            Debug.Log("[SetupCourseTool] Generated 4 obstacle prefabs and FinishLine.");
        }

        private static void CreateLooseCrate(Sprite sprite)
        {
            GameObject go = new GameObject("Prop_LooseCrate");
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.8f, 0.5f, 0.2f); // Wood color
            
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 1f;
            rb.gravityScale = 1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.05f;
            rb.freezeRotation = true;

            PhysicsMaterial2D mat = new PhysicsMaterial2D("CrateMat");
            mat.friction = 0.4f;
            mat.bounciness = 0f;
            AssetDatabase.CreateAsset(mat, "Assets/Prefabs/Obstacles/CrateMat.physicsMaterial2D");
            bc.sharedMaterial = mat;

            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Obstacles/Prop_LooseCrate.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreateFallingPlatform(Sprite sprite)
        {
            GameObject go = new GameObject("Obstacle_FallingPlatform");
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.red;
            
            go.AddComponent<BoxCollider2D>();
            
            // Script adds Rigidbody2D automatically
            go.AddComponent<FallingPlatform>();

            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Obstacles/Obstacle_FallingPlatform.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreateSwingingHazard(Sprite sprite)
        {
            GameObject pivot = new GameObject("Obstacle_SwingingHazard");
            
            GameObject arm = new GameObject("Arm");
            arm.transform.SetParent(pivot.transform);
            arm.transform.localPosition = new Vector3(0, -3f, 0); // Hang down
            
            SpriteRenderer sr = arm.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.grey;
            // Stretch it to look like an arm
            arm.transform.localScale = new Vector3(0.2f, 6f, 1f);
            
            Rigidbody2D armRb = arm.AddComponent<Rigidbody2D>();
            armRb.bodyType = RigidbodyType2D.Dynamic;
            armRb.mass = 1f;
            armRb.angularDamping = 0.05f;

            HingeJoint2D hinge = arm.AddComponent<HingeJoint2D>();
            hinge.connectedBody = null;
            hinge.anchor = new Vector2(0, 0.5f); // Top of the arm

            // Add the hazard part
            GameObject hazard = new GameObject("Hazard");
            hazard.transform.SetParent(arm.transform);
            hazard.transform.localPosition = new Vector3(0, -0.6f, 0);
            hazard.transform.localScale = new Vector3(8f, 0.25f, 1f); // Counter-scale
            
            SpriteRenderer hSr = hazard.AddComponent<SpriteRenderer>();
            hSr.sprite = sprite;
            hSr.color = Color.black;
            
            CircleCollider2D cc = hazard.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            hazard.tag = "Hazard";
            hazard.layer = 7; // Hazard layer
            hazard.AddComponent<KillZone>(); // Instantly kills player on touch

            pivot.AddComponent<SwingingHazard>();

            PrefabUtility.SaveAsPrefabAsset(pivot, "Assets/Prefabs/Obstacles/Obstacle_SwingingHazard.prefab");
            Object.DestroyImmediate(pivot);
        }

        private static void CreateMovingPlatform(Sprite sprite)
        {
            GameObject go = new GameObject("Obstacle_MovingPlatform");
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.blue;
            go.transform.localScale = new Vector3(3f, 0.5f, 1f); // Wide platform
            
            go.AddComponent<BoxCollider2D>();
            
            MovingPlatform mp = go.AddComponent<MovingPlatform>();
            SerializedObject so = new SerializedObject(mp);
            so.FindProperty("_pointA").vector3Value = new Vector3(0, 0, 0);
            so.FindProperty("_pointB").vector3Value = new Vector3(5, 0, 0);
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Obstacles/Obstacle_MovingPlatform.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreateFinishLine(Sprite sprite)
        {
            GameObject go = new GameObject("Course_FinishLine");
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.yellow;
            go.transform.localScale = new Vector3(0.5f, 5f, 1f); // Tall finish line pole
            
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;

            go.AddComponent<FinishLine>();

            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Obstacles/Course_FinishLine.prefab");
            Object.DestroyImmediate(go);
        }
    }
}
