using UnityEditor;
using UnityEngine;

namespace YourGame.Editor
{
    /// <summary>
    /// Creates the zero-friction PhysicsMaterial2D used by the Player BoxCollider2D.
    /// Without this, the collider snags on CompositeCollider2D tilemap seams and
    /// abruptly kills horizontal velocity mid-walk.
    ///
    /// Menu: Tools -> 2D-Gaviola -> Setup -> Create Player Physics Material
    ///
    /// Safe to re-run: skips creation if the asset already exists.
    /// After running, manually assign the generated asset to:
    ///   Player -> BoxCollider2D -> Material
    /// </summary>
    public static class CreatePlayerPhysicsMaterial
    {
        private const string OutputPath = "Assets/Physics/PlayerNoFriction.physicsMaterial2D";

        [MenuItem("Tools/2D-Gaviola/Setup/Create Player Physics Material")]
        public static void Create()
        {
            // Skip if asset already exists.
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(OutputPath);
            if (existing != null)
            {
                Debug.Log("[CreatePlayerPhysicsMaterial] Asset already exists at: " + OutputPath);
                EditorGUIUtility.PingObject(existing);
                return;
            }

            // Ensure the folder exists.
            if (!AssetDatabase.IsValidFolder("Assets/Physics"))
                AssetDatabase.CreateFolder("Assets", "Physics");

            var mat = new PhysicsMaterial2D("PlayerNoFriction")
            {
                friction   = 0f,
                bounciness = 0f,
                // frictionCombine / bounceCombine default to Minimum in Unity 6,
                // which picks the lower value when two materials interact,
                // so the player stays snag-free even on surfaces that have friction.
            };

            AssetDatabase.CreateAsset(mat, OutputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CreatePlayerPhysicsMaterial] Created: " + OutputPath +
                      "\nNow assign it to Player -> BoxCollider2D -> Material in the Inspector.");
            EditorGUIUtility.PingObject(mat);
        }
    }
}
