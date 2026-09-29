using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using YourGame.Gameplay.UI;

/// <summary>
/// Cleans up any old Main Menu garbage in the scene, then places
/// a single "MainMenuManager" GameObject with all 6 sprites auto-assigned.
/// Run via: Tools > Setup Main Menu (Clean)
/// </summary>
public class SetupMainMenuTool : Editor
{
    [MenuItem("Tools/Setup Main Menu (Clean)")]
    public static void SetupMainMenu()
    {
        // ── 1. Nuke every old element from previous setups ────────────────────
        string[] staleNames =
        {
            "_MainMenuCanvas", "MainMenuCanvas",
            "HamburgerButton", "DarkOverlay", "MenuContainer",
            "MenuPanel", "ButtonsLayout", "ButtonLayout",
            "ResumeButton", "RestartButton", "SettingsButton", "ExitButton",
        };

        // Remove stale GameObjects
        var allGOs = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGOs)
        {
            foreach (var staleName in staleNames)
            {
                if (go != null && go.name == staleName)
                {
                    Debug.Log($"[SetupMainMenu] Removing stale object: {go.name}");
                    Object.DestroyImmediate(go);
                    break;
                }
            }
        }

        // Remove stale MainMenuManager components (could be orphaned on other objects)
        var staleManagers = Object.FindObjectsByType<MainMenuManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in staleManagers)
        {
            Debug.Log($"[SetupMainMenu] Removing stale MainMenuManager from {m.gameObject.name}");
            Object.DestroyImmediate(m);
        }

        // ── 2. Ensure an EventSystem exists ───────────────────────────────────
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Debug.Log("[SetupMainMenu] Created EventSystem.");
        }

        // ── 3. Create the single MainMenuManager GameObject ───────────────────
        var managerGO = new GameObject("MainMenuManager");
        var manager   = managerGO.AddComponent<MainMenuManager>();

        // ── 4. Auto-assign sprites from main-menu.png ─────────────────────────
        const string spritePath = "Assets/Sprites/Menu/main-menu.png";
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);

        if (allAssets.Length == 0)
        {
            Debug.LogError($"[SetupMainMenu] Could not find sprite sheet at {spritePath}. " +
                           "Attach sprites manually in the Inspector.");
        }
        else
        {
            var so = new SerializedObject(manager);
            AssignSprite(so, "_bgSprite",        allAssets, "main-menu_0");
            AssignSprite(so, "_hamburgerSprite",  allAssets, "main-menu_1");
            AssignSprite(so, "_resumeSprite",     allAssets, "main-menu_2");
            AssignSprite(so, "_restartSprite",    allAssets, "main-menu_3");
            AssignSprite(so, "_settingsSprite",   allAssets, "main-menu_4");
            AssignSprite(so, "_exitSprite",       allAssets, "main-menu_5");
            so.ApplyModifiedProperties();
            Debug.Log("[SetupMainMenu] All 6 sprites assigned.");
        }

        // ── 5. Save scene ─────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            managerGO.scene);

        Debug.Log("[SetupMainMenu] Done! Press Play — the UI builds itself at runtime.");
    }

    private static void AssignSprite(SerializedObject so,
                                     string fieldName,
                                     Object[] assets,
                                     string spriteName)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[SetupMainMenu] Could not find field '{fieldName}'.");
            return;
        }

        foreach (var asset in assets)
        {
            if (asset is Sprite s && s.name == spriteName)
            {
                prop.objectReferenceValue = s;
                return;
            }
        }

        Debug.LogWarning($"[SetupMainMenu] Sprite '{spriteName}' not found in sheet.");
    }
}
