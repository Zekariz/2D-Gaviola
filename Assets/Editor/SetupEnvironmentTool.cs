using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using YourGame.Gameplay.Core;

namespace YourGame.Editor
{
    public static class SetupEnvironmentTool
    {
        private const string EnvSheetPath = "Assets/Sprites/Environment/env-sheets.png";
        private const string BgPath = "Assets/Sprites/Environment/2d-background.jpg";
        private const string DirtPath = "Assets/Sprites/Environment/dirt.png";

        [MenuItem("Tools/2D-Gaviola/Environment/Create Solid Tiles")]
        public static void CreateSolidTiles()
        {
            string outFolder = "Assets/Prefabs/Environment/Solid";
            EnsureFolderExists(outFolder);

            int created = 0;

            // Process env-sheets
            var sprites = LoadSprites(EnvSheetPath);
            if (sprites != null && sprites.Length > 0)
            {
                var solidNames = new HashSet<string>();
                for (int i = 0; i <= 12; i++) solidNames.Add($"env-sheets_{i}");
                solidNames.Add("env-sheets_19");
                solidNames.Add("env-sheets_21");
                solidNames.Add("env-sheets_22");

                foreach (var sprite in sprites)
                {
                    if (solidNames.Contains(sprite.name))
                    {
                        CreatePrefab(outFolder, sprite, false);
                        created++;
                    }
                }
            }

            // Process dirt
            var dirtSprites = LoadSprites(DirtPath);
            if (dirtSprites != null && dirtSprites.Length > 0)
            {
                // Create a solid prefab for the dirt sprite (assuming it's a Single sprite or we just use the first slice)
                CreatePrefab(outFolder, dirtSprites[0], false);
                created++;
            }

            Debug.Log($"[EnvironmentSetup] Created/Updated {created} Solid Tile prefabs in {outFolder}");
        }

        [MenuItem("Tools/2D-Gaviola/Environment/Create Hazard Tiles")]
        public static void CreateHazardTiles()
        {
            string outFolder = "Assets/Prefabs/Environment/Hazards";
            EnsureFolderExists(outFolder);

            var sprites = LoadSprites(EnvSheetPath);
            if (sprites == null || sprites.Length == 0) return;

            var hazardNames = new HashSet<string>();
            for (int i = 34; i <= 45; i++) hazardNames.Add($"env-sheets_{i}");

            int created = 0;
            foreach (var sprite in sprites)
            {
                if (hazardNames.Contains(sprite.name))
                {
                    CreatePrefab(outFolder, sprite, true);
                    created++;
                }
            }
            Debug.Log($"[EnvironmentSetup] Created/Updated {created} Hazard Tile prefabs in {outFolder}");
        }

        [MenuItem("Tools/2D-Gaviola/Environment/Setup Fixed Background")]
        public static void SetupFixedBackground()
        {
            var sprites = LoadSprites(BgPath);
            if (sprites == null || sprites.Length == 0) return;

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[EnvironmentSetup] No Main Camera found in the scene! Please tag your camera as MainCamera.");
                return;
            }

            Sprite bgSprite = sprites[0];
            string objName = "Background_Fixed";
            
            GameObject bgObj = GameObject.Find(objName);
            if (bgObj == null) bgObj = new GameObject(objName);

            // Parent to the camera
            bgObj.transform.SetParent(cam.transform);

            var sr = bgObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = bgObj.AddComponent<SpriteRenderer>();
            
            sr.sprite = bgSprite;
            sr.sortingLayerName = GetLowestSortingLayer();
            
            // Set local position relative to camera
            bgObj.transform.localPosition = new Vector3(0, 0, 10); 

            // Scale to fit camera
            float camHeight = 2f * cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;
            Vector2 spriteSize = bgSprite.bounds.size;
            
            // Avoid division by zero
            if (spriteSize.x > 0 && spriteSize.y > 0)
            {
                float scaleX = camWidth / spriteSize.x;
                float scaleY = camHeight / spriteSize.y;
                // Stretch to fit the camera exactly so the full image is visible
                bgObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[EnvironmentSetup] Setup {objName} attached to Main Camera using sorting layer '{sr.sortingLayerName}'.");
        }

        [MenuItem("Tools/2D-Gaviola/Environment/Create Moving Platforms")]
        public static void CreateMovingPlatforms()
        {
            string outFolder = "Assets/Prefabs/Environment/Moving";
            EnsureFolderExists(outFolder);

            var sprites = LoadSprites(EnvSheetPath);
            if (sprites == null || sprites.Length == 0) return;

            var baseSprite = sprites.FirstOrDefault(s => s.name == "env-sheets_10");
            if (baseSprite == null)
            {
                Debug.LogError("[EnvironmentSetup] Could not find env-sheets_10 to use as a base for moving platforms.");
                return;
            }

            // Create Vertical Platform
            CreateMovingPrefab(outFolder, "platform_v", baseSprite, new Vector3(0, 3f, 0));
            
            // Create Horizontal Platform (Right-moving)
            CreateMovingPrefab(outFolder, "platform_h", baseSprite, new Vector3(3f, 0, 0));

            // Create Horizontal Platform (Left-moving)
            CreateMovingPrefab(outFolder, "platform_h_left", baseSprite, new Vector3(-3f, 0, 0));

            Debug.Log($"[EnvironmentSetup] Created Moving Platforms in {outFolder}");
        }

        private static void CreateMovingPrefab(string folder, string name, Sprite sprite, Vector3 offset)
        {
            string path = $"{folder}/{name}.prefab";
            GameObject root = new GameObject(name);
            
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            
            var col = root.AddComponent<BoxCollider2D>();
            col.usedByEffector = true;
            
            var effector = root.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;

            var mover = root.AddComponent<YourGame.Gameplay.Environment.MovingPlatform>();
            mover.moveOffset = offset;
            
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreatePrefab(string folder, Sprite sprite, bool isHazard)
        {
            string path = $"{folder}/{sprite.name}.prefab";
            GameObject root = new GameObject(sprite.name);
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            var col = root.AddComponent<BoxCollider2D>();

            // Make env-sheets_10 a one-way platform
            if (sprite.name == "env-sheets_10")
            {
                col.usedByEffector = true;
                var effector = root.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
            }

            if (isHazard)
            {
                col.isTrigger = true;
                root.AddComponent<KillZone>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static Sprite[] LoadSprites(string path)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return sprites;
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string newFolder = Path.GetFileName(path);
                EnsureFolderExists(parent);
                AssetDatabase.CreateFolder(parent, newFolder);
            }
        }

        private static string GetLowestSortingLayer()
        {
            var layers = SortingLayer.layers;
            if (layers.Length > 0)
            {
                var bgLayer = layers.FirstOrDefault(l => l.name == "Background");
                if (bgLayer.name == "Background") return "Background";
                return layers[0].name;
            }
            return "Default";
        }
    }
}
