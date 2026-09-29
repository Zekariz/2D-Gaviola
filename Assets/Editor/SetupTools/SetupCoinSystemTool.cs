using UnityEditor;
using UnityEngine;
using YourGame.Gameplay.Coins;
using System.IO;
using UnityEditor.SceneManagement;

namespace YourGame.Editor
{
    /// <summary>
    /// One-click setup tool for the Coin System.
    /// Run via: Tools → 2D-Gaviola → Setup → 5. Setup Coin System
    /// </summary>
    public static class SetupCoinSystemTool
    {
        private const string SpriteSheetPath = "Assets/Sprites/Environment/accomplished.png";
        private const string CoinSoundPath   = "Assets/Audio/SFX/coin-collection-sound.mp3";
        private const string CoinPrefabDir   = "Assets/Prefabs/Coins";
        private const string CoinPrefabPath  = "Assets/Prefabs/Coins/Coin.prefab";
        private const string CoinSpriteName  = "accomplished_83";

        // NOTE: [MenuItem] left commented out per project convention.
        // Uncomment to expose in Unity menu if needed.
        // [MenuItem("Tools/2D-Gaviola/Setup/5. Setup Coin System")]
        public static void RunSetup()
        {
            // ── 1. Load assets ──────────────────────────────────────────────────
            Sprite coinSprite = GetSpriteFromSheet(SpriteSheetPath, CoinSpriteName);
            if (coinSprite == null)
            {
                Debug.LogError($"[SetupCoinSystem] Sprite '{CoinSpriteName}' not found in {SpriteSheetPath}");
                return;
            }

            AudioClip coinSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CoinSoundPath);
            if (coinSound == null)
            {
                Debug.LogError($"[SetupCoinSystem] Audio clip not found at {CoinSoundPath}");
                return;
            }

            // ── 2. Create / update CoinManager in scene ─────────────────────────
            CoinManager manager = Object.FindAnyObjectByType<CoinManager>();
            if (manager == null)
            {
                GameObject go = new GameObject("CoinManager");
                manager = go.AddComponent<CoinManager>();
                Debug.Log("[SetupCoinSystem] Created CoinManager GameObject in scene.");
            }

            manager.coinSprite = coinSprite;
            manager.coinSound  = coinSound;
            EditorUtility.SetDirty(manager);

            // ── 3. Create Coin Prefab ────────────────────────────────────────────
            if (!Directory.Exists(CoinPrefabDir))
            {
                Directory.CreateDirectory(CoinPrefabDir);
                AssetDatabase.Refresh();
            }

            GameObject tempCoin = new GameObject("Coin");

            SpriteRenderer sr = tempCoin.AddComponent<SpriteRenderer>();
            sr.sprite           = coinSprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 5;

            BoxCollider2D bc = tempCoin.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;

            tempCoin.AddComponent<Coin>();

            PrefabUtility.SaveAsPrefabAsset(tempCoin, CoinPrefabPath);
            Object.DestroyImmediate(tempCoin);
            Debug.Log($"[SetupCoinSystem] Coin prefab saved to {CoinPrefabPath}");

            // ── 4. Spawn 3 test coins near the player ────────────────────────────
            SpawnTestCoins();

            // ── 5. Mark scene dirty so Unity asks to save ────────────────────────
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[SetupCoinSystem] ✅ Setup Complete! Press Play to test.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static void SpawnTestCoins()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
            if (prefab == null) return;

            GameObject player    = GameObject.Find("Player");
            Vector3    spawnBase = player != null
                ? player.transform.position + Vector3.right * 3f + Vector3.up
                : new Vector3(0, 2, 0);

            for (int i = 0; i < 3; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = spawnBase + new Vector3(i * 1.5f, 0, 0);
            }

            Debug.Log("[SetupCoinSystem] Spawned 3 test coins near the Player.");
        }

        private static Sprite GetSpriteFromSheet(string sheetPath, string spriteName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                if (asset is Sprite s && s.name == spriteName)
                    return s;
            }
            return null;
        }
    }
}
