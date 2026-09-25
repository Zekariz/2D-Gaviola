using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace YourGame.Editor
{
    public class SetupTilemapTool
    {
        [MenuItem("Tools/2D-Gaviola/Setup/2. Create Ground Tilemap")]
        public static void CreateTilemap()
        {
            // Create Grid
            GameObject gridGo = GameObject.Find("Grid");
            if (gridGo == null)
            {
                gridGo = new GameObject("Grid");
                gridGo.AddComponent<Grid>();
            }

            // Create Tilemap layers
            CreateTilemapLayer(gridGo, "Ground", 6, true);      // Layer 6 is Ground
            CreateTilemapLayer(gridGo, "Walls", 6, true);       // Layer 6 is Ground
            CreateTilemapLayer(gridGo, "Decoration", 0, false); // Default Layer, no collision

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SetupTilemapTool] Tilemap foundation created. You can now paint your level.");
        }

        private static void CreateTilemapLayer(GameObject parent, string name, int layerIndex, bool hasCollision)
        {
            Transform existing = parent.transform.Find(name);
            GameObject layerGo;
            
            if (existing != null)
            {
                layerGo = existing.gameObject;
            }
            else
            {
                layerGo = new GameObject(name);
                layerGo.transform.SetParent(parent.transform);
                layerGo.AddComponent<Tilemap>();
                layerGo.AddComponent<TilemapRenderer>();
            }

            layerGo.layer = layerIndex;

            if (hasCollision)
            {
                TilemapCollider2D tc = layerGo.GetComponent<TilemapCollider2D>();
                if (tc == null) tc = layerGo.AddComponent<TilemapCollider2D>();
                tc.usedByComposite = true;

                CompositeCollider2D cc = layerGo.GetComponent<CompositeCollider2D>();
                if (cc == null) cc = layerGo.AddComponent<CompositeCollider2D>();
                cc.geometryType = CompositeCollider2D.GeometryType.Polygons;

                Rigidbody2D rb = layerGo.GetComponent<Rigidbody2D>();
                // CompositeCollider2D automatically adds Rigidbody2D
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Static;
                }
            }
        }
    }
}
