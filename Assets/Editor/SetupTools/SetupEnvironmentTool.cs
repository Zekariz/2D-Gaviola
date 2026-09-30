using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using YourGame.Gameplay.Core;
using YourGame.Gameplay.Player;

namespace YourGame.Editor
{
    public static class SetupEnvironmentTool
    {
        private const string EnvSheetPath = "Assets/Sprites/Environment/env-sheets.png";
        private const string BgPath = "Assets/Sprites/Environment/2d-background.jpg";
        private const string DirtPath = "Assets/Sprites/Environment/dirt.png";

        [MenuItem("Tools/2D-Gaviola/Player/Setup Player HitBox")]
        public static void SetupPlayerHitBox()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[SetupTool] No PlayerController found in the scene! Make sure the Player is in the scene.");
                return;
            }

            // Remove existing HitBox if present
            var existing = player.transform.Find("HitBox");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            // The player's physics BoxCollider2D is: size (0.8, 1.5), offset (0, 0.75)
            // That means the body bottom is at local y=0 and top is at local y=1.5
            // We want the HitBox to cover from the mid-torso up (feet excluded).
            // Height = 1.0, offset starts at y=0.30 above feet -> center at y=0.80
            GameObject hitBox = new GameObject("HitBox");
            hitBox.transform.SetParent(player.transform);
            hitBox.transform.localPosition = Vector3.zero;
            hitBox.transform.localScale    = Vector3.one;

            var col = hitBox.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            
            // Player body is 1.5 tall (bottom at 0, top at 1.5).
            // "Ankle to forehead": start at y=0.2, end at y=1.3 -> Height 1.1, center at 0.75
            col.size      = new Vector2(0.6f, 1.1f);   
            col.offset    = new Vector2(0f, 0.75f);    
            
            // Set to "Ignore Raycast" layer (2) so it doesn't block ground checks
            hitBox.layer = 2;

            hitBox.AddComponent<PlayerHitBox>();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SetupTool] Player HitBox child created. Feet are now excluded from hazard detection!");
        }

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
        [MenuItem("Tools/2D-Gaviola/Environment/Create Movable Crates")]
        public static void CreateMovableCrates()
        {
            string outFolder = "Assets/Prefabs/Environment/Movables";
            EnsureFolderExists(outFolder);

            string cratePath = "Assets/Sprites/Environment/crate.png";
            var sprites = LoadSprites(cratePath);
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogError($"[EnvironmentSetup] Could not find crates at {cratePath}");
                return;
            }

            var targetIndices = new HashSet<int>();
            for (int i = 0; i <= 6; i++) targetIndices.Add(i);
            for (int i = 8; i <= 14; i++) targetIndices.Add(i);
            for (int i = 27; i <= 33; i++) targetIndices.Add(i);
            targetIndices.Add(51);
            targetIndices.Add(52);
            targetIndices.Add(74);
            for (int i = 85; i <= 88; i++) targetIndices.Add(i);

            int created = 0;
            foreach (var sprite in sprites)
            {
                // The name is likely "crate_X"
                string numStr = sprite.name.Replace("crate_", "");
                if (int.TryParse(numStr, out int index) && targetIndices.Contains(index))
                {
                    string path = $"{outFolder}/{sprite.name}.prefab";
                    GameObject root = new GameObject(sprite.name);
                    
                    var sr = root.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    
                    // The collider should perfectly trace the sprite
                    var col = root.AddComponent<BoxCollider2D>();
                    
                    // Add physics for pushing
                    var rb = root.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.mass = 1f; // easy enough to push
                    // Add friction/drag so it doesn't slide infinitely like on ice
                    rb.linearDamping = 5f; 
                    rb.freezeRotation = true; // prevent the crate from tumbling over
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                    created++;
                }
            }
            
            Debug.Log($"[EnvironmentSetup] Created {created} Movable Crate prefabs in {outFolder}");
        }

        [MenuItem("Tools/2D-Gaviola/Environment/Create Finish Flag & UI")]
        public static void CreateFinishFlagAndUI()
        {
            string outFolder = "Assets/Prefabs/Environment";
            EnsureFolderExists(outFolder);

            // 1. Create Finish Flag Prefab
            string flagPath = "Assets/Sprites/Environment/finish-flag.png";
            var flagSprites = LoadSprites(flagPath);
            if (flagSprites != null && flagSprites.Length > 0)
            {
                GameObject flagRoot = new GameObject("Finish_Flag");
                var sr = flagRoot.AddComponent<SpriteRenderer>();
                sr.sprite = flagSprites[0];
                var col = flagRoot.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                flagRoot.AddComponent<YourGame.Gameplay.Environment.FinishFlag>();
                PrefabUtility.SaveAsPrefabAsset(flagRoot, $"{outFolder}/Finish_Flag.prefab");
                Object.DestroyImmediate(flagRoot);
                Debug.Log("[EnvironmentSetup] Created Finish_Flag prefab.");
            }

            // 2. Create Level Complete UI Canvas Prefab
            string accompPath = "Assets/Sprites/Environment/accomplished.png";
            var accompSprites = LoadSprites(accompPath);
            Sprite accompSprite = null;
            if (accompSprites != null)
            {
                foreach (var s in accompSprites)
                {
                    if (s.name == "accomplished_58")
                    {
                        accompSprite = s;
                        break;
                    }
                }
            }

            if (accompSprite == null)
            {
                Debug.LogError("[EnvironmentSetup] Could not find 'accomplished_58' inside accomplished.png!");
                return;
            }

            GameObject canvasObj = new GameObject("LevelCompleteCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject bgObj = new GameObject("DarkBackground");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0); // Black, 0 alpha
            var bgRect = bgImg.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            GameObject accompObj = new GameObject("AccomplishedImage");
            accompObj.transform.SetParent(canvasObj.transform, false);
            var accompImg = accompObj.AddComponent<UnityEngine.UI.Image>();
            accompImg.sprite = accompSprite;
            accompImg.SetNativeSize(); // keep original proportions
            var accompRect = accompImg.rectTransform;
            accompRect.anchorMin = new Vector2(0.5f, 0.5f); // Center
            accompRect.anchorMax = new Vector2(0.5f, 0.5f);
            accompRect.pivot = new Vector2(0.5f, 0.5f);
            accompRect.anchoredPosition = new Vector2(0, -1000); // Start offscreen

            var manager = canvasObj.AddComponent<YourGame.Gameplay.Environment.LevelCompleteManager>();
            
            // Link references via SerializedObject to bypass private fields easily
            var so = new SerializedObject(manager);
            so.FindProperty("_darkBackground").objectReferenceValue = bgImg;
            so.FindProperty("_accomplishedImage").objectReferenceValue = accompRect;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(canvasObj, $"{outFolder}/LevelCompleteCanvas.prefab");
            Object.DestroyImmediate(canvasObj);
            
            Debug.Log("[EnvironmentSetup] Created LevelCompleteCanvas prefab. Drag BOTH into your scene!");
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

        [MenuItem("Tools/2D-Gaviola/Environment/Create Checkpoint Prefab")]
        public static void CreateCheckpointPrefab()
        {
            string outFolder = "Assets/Prefabs/Environment/Interactables";
            EnsureFolderExists(outFolder);

            string flagPath = "Assets/Sprites/Environment/checkpoint-flag.png";
            var sprites = LoadSprites(flagPath);
            
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogError($"[EnvironmentSetup] Could not find checkpoint sprite at {flagPath}");
                return;
            }

            string path = $"{outFolder}/CheckpointFlag.prefab";
            GameObject root = new GameObject("CheckpointFlag");
            
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            
            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            
            root.AddComponent<YourGame.Gameplay.Environment.Checkpoint>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            
            Debug.Log($"[EnvironmentSetup] Created Checkpoint prefab at {path}");
        }

        [MenuItem("Tools/2D-Gaviola/Environment/Create Invisible Wall")]
        public static void CreateInvisibleWall()
        {
            string outFolder = "Assets/Prefabs/Environment/Solid";
            EnsureFolderExists(outFolder);

            string path = $"{outFolder}/InvisibleWall.prefab";
            GameObject root = new GameObject("InvisibleWall");
            
            var col = root.AddComponent<BoxCollider2D>();
            // Default to a tall wall shape
            col.size = new Vector2(1f, 20f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Debug.Log($"[EnvironmentSetup] Created Invisible Wall prefab at {path}");
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
            // Unity automatically sets col.size and col.offset to match the Sprite exactly.
            Vector2 origSize = col.size;
            Vector2 origOffset = col.offset;

            if (isHazard)
            {
                // 1. SOLID COLLIDER (Outer layer - Blocks Crates & Player)
                // We reuse the 'col' created above.
                // Shrunk to 70% so it's deeply inside the sprite, acting as a physical wall.
                col.size = new Vector2(origSize.x * 0.70f, origSize.y * 0.70f);
                col.offset = origOffset;
                col.isTrigger = false;

                // 2. TRIGGER COLLIDER (Inner layer - Kills Player)
                // Sized to 85% so it extends slightly OUTSIDE the solid block.
                // This ensures the player's HitBox touches the death trigger BEFORE their physical body hits the solid block.
                var triggerCol = root.AddComponent<BoxCollider2D>();
                triggerCol.size = new Vector2(origSize.x * 0.85f, origSize.y * 0.85f);
                triggerCol.offset = origOffset;
                triggerCol.isTrigger = true;
                
                root.AddComponent<KillZone>();
            }
            else
            {
                // Solid block: just slightly shave the top to prevent floating on transparent edges.
                // We'll reduce the height by a tiny fraction (e.g. 5%) and shift it down by half that fraction.
                col.size   = new Vector2(origSize.x, origSize.y * 0.95f);
                col.offset = new Vector2(origOffset.x, origOffset.y - (origSize.y * 0.025f));
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
