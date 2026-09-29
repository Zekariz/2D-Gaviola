using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YourGame.Gameplay.Victory;

namespace YourGame.Editor
{
    /// <summary>
    /// Tools → 2D-Gaviola → Setup → Create Victory Screen
    ///
    /// Idempotent — re-running updates existing objects rather than duplicating them.
    /// After running, the scene is saved automatically.
    /// </summary>
    public static class SetupVictoryScreenTool
    {
        private const string AccomplishedSpritePath = "Assets/Sprites/Environment/accomplished.png";
        private const string FinishFlagSpritePath   = "Assets/Sprites/Environment/finish-flag.png";
        private const string FinishFlagPrefabPath   = "Assets/Prefabs/Environment/Finish_Flag.prefab";

        private const string TargetSliceName        = "accomplished_58";

        // [MenuItem("Tools/2D-Gaviola/Setup/Create Victory Screen")]
        public static void CreateVictoryScreen()
        {
            // ── 1. Find accomplished_58 sprite ───────────────────────────────
            Sprite accomplishedSprite = FindSprite(AccomplishedSpritePath, TargetSliceName);
            if (accomplishedSprite == null)
            {
                Debug.LogError(
                    $"[SetupVictoryScreen] Could not find sprite '{TargetSliceName}' in {AccomplishedSpritePath}. " +
                    "Ensure the texture is sliced in the Sprite Editor.");
                return;
            }

            // ── 2. Find or create the Canvas ─────────────────────────────────
            Canvas canvas = FindOrCreateCanvas("VictoryCanvas");

            // ── 3. Create dark overlay Image ──────────────────────────────────
            Image overlay = FindOrCreateImageChild(canvas.transform, "DarkOverlay");
            overlay.color = new Color(0f, 0f, 0f, 0f); // start fully transparent
            // Stretch to fill the canvas
            RectTransform overlayRect = overlay.rectTransform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.gameObject.SetActive(false);

            // ── 4. Create victory sprite Image ────────────────────────────────
            Image victoryImg = FindOrCreateImageChild(canvas.transform, "VictoryImage");
            victoryImg.sprite    = accomplishedSprite;
            victoryImg.type      = Image.Type.Simple;
            victoryImg.preserveAspect = true;
            victoryImg.SetNativeSize();
            victoryImg.gameObject.SetActive(false);

            // Center anchor / pivot, start below the screen
            RectTransform victoryRect = victoryImg.rectTransform;
            victoryRect.anchorMin        = new Vector2(0.5f, 0.5f);
            victoryRect.anchorMax        = new Vector2(0.5f, 0.5f);
            victoryRect.pivot            = new Vector2(0.5f, 0.5f);
            victoryRect.anchoredPosition = new Vector2(0f, -1200f);
            victoryRect.localScale       = new Vector3(3f, 3f, 1f); // Make it 3x bigger

            // ── 5. Create or find VictoryScreenManager ────────────────────────
            VictoryScreenManager manager = FindOrCreateManager(canvas.gameObject);

            // Wire serialized references via SerializedObject so private fields are set
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("_darkOverlay").objectReferenceValue      = overlay;
            so.FindProperty("_victoryImageRect").objectReferenceValue = victoryRect;
            so.ApplyModifiedProperties();

            // ── 6. Create Finish Flag prefab ──────────────────────────────────
            CreateFinishFlagPrefab();

            // ── 7. Save the scene ─────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log(
                "[SetupVictoryScreen] Done! " +
                "Drag 'Finish_Flag' from Assets/Prefabs/Environment into the scene and place it at the end of the level.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Sprite FindSprite(string assetPath, string sliceName)
        {
            // LoadAllAssetsAtPath returns the texture and all its sub-sprites
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite s && s.name == sliceName)
                    return s;
            }
            return null;
        }

        private static Canvas FindOrCreateCanvas(string goName)
        {
            // Look for an existing canvas with this name
            var existing = GameObject.Find(goName);
            if (existing != null)
            {
                var c = existing.GetComponent<Canvas>();
                if (c != null) return c;
            }

            // Create fresh
            var canvasGO = new GameObject(goName);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // renders above everything

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create VictoryCanvas");
            return canvas;
        }

        private static Image FindOrCreateImageChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                var img = existing.GetComponent<Image>();
                if (img != null) return img;
            }

            var go  = new GameObject(childName);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");
            return go.AddComponent<Image>();
        }

        private static VictoryScreenManager FindOrCreateManager(GameObject canvasGO)
        {
            // Manager lives on the Canvas GameObject itself
            var manager = canvasGO.GetComponent<VictoryScreenManager>();
            if (manager != null) return manager;

            return Undo.AddComponent<VictoryScreenManager>(canvasGO);
        }

        private static void CreateFinishFlagPrefab()
        {
            // Ensure prefab output folder exists
            string folder = "Assets/Prefabs/Environment";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Environment");

            // Load the finish-flag sprite
            Sprite[] sprites = GetAllSpritesAt(FinishFlagSpritePath);
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"[SetupVictoryScreen] Could not find finish-flag sprite at {FinishFlagSpritePath}. " +
                                 "Prefab was NOT created.");
                return;
            }

            // If prefab already exists, update its collider
            if (System.IO.File.Exists(FinishFlagPrefabPath))
            {
                GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FinishFlagPrefabPath);
                if (existingPrefab != null)
                {
                    var existingCol = existingPrefab.GetComponent<BoxCollider2D>();
                    if (existingCol != null)
                    {
                        // Shrink trigger to the center of the flag to avoid early triggers
                        existingCol.size = new Vector2(0.5f, 3.0f);
                        existingCol.offset = Vector2.zero;
                    }
                    if (existingPrefab.GetComponent<FinishLineTrigger>() == null)
                        existingPrefab.AddComponent<FinishLineTrigger>();
                    
                    PrefabUtility.SavePrefabAsset(existingPrefab);
                    Debug.Log($"[SetupVictoryScreen] Updated existing Finish_Flag prefab at {FinishFlagPrefabPath} with tighter collider.");
                }
                return;
            }

            var root = new GameObject("Finish_Flag");

            var sr    = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0]; // finish-flag_0

            var col       = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            // Shrink trigger to the center of the flag to avoid early triggers
            col.size = new Vector2(0.5f, 3.0f);
            col.offset = Vector2.zero;

            root.AddComponent<FinishLineTrigger>();

            PrefabUtility.SaveAsPrefabAsset(root, FinishFlagPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[SetupVictoryScreen] Created Finish_Flag prefab at {FinishFlagPrefabPath}.");
        }

        private static Sprite[] GetAllSpritesAt(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            System.Collections.Generic.List<Sprite> result = new();
            foreach (Object a in assets)
            {
                if (a is Sprite s) result.Add(s);
            }
            return result.ToArray();
        }
    }
}
