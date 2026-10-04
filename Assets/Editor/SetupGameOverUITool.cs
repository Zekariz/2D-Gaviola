#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YourGame.Gameplay.UI;

public static class SetupGameOverUITool
{
    [MenuItem("Tools/Setup Game Over UI")]
    public static void GenerateUI()
    {
        // 1. Find or create PlayerUICanvas
        GameObject canvasObj = GameObject.Find("PlayerUICanvas");
        Canvas canvas;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("PlayerUICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Delete existing GameOverUI if it exists
        Transform existing = canvasObj.transform.Find("GameOverUI");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Load Sprites correctly from slices
        Sprite bannerSprite = null;
        Sprite retrySprite = null;
        Sprite exitSprite = null;

        Object[] gameOverAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/game-over.png");
        if (gameOverAssets != null)
        {
            foreach (var obj in gameOverAssets)
            {
                if (obj is Sprite s)
                {
                    if (s.name == "game-over_0") bannerSprite = s;
                    if (s.name == "game-over_2") retrySprite = s;
                }
            }
        }

        Object[] mainMenuAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Menu/main-menu.png");
        if (mainMenuAssets != null)
        {
            foreach (var obj in mainMenuAssets)
            {
                if (obj is Sprite s && s.name == "main-menu_5")
                {
                    exitSprite = s;
                    break;
                }
            }
        }

        // 2. Create Main Panel
        GameObject panelObj = new GameObject("GameOverUI");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.6f); // Dark tint

        // 3. Create Banner
        GameObject bannerObj = new GameObject("Banner");
        bannerObj.transform.SetParent(panelRect, false);
        Image bannerImg = bannerObj.AddComponent<Image>();
        bannerImg.sprite = bannerSprite;
        bannerImg.preserveAspect = true;
        
        RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.anchoredPosition = new Vector2(0, 150); // Shifted up
        bannerRect.sizeDelta = new Vector2(800, 300); // Approximate banner size

        // 4. Create Buttons Container
        GameObject btnContainer = new GameObject("ButtonsContainer");
        btnContainer.transform.SetParent(panelRect, false);
        RectTransform btnContainerRect = btnContainer.AddComponent<RectTransform>();
        btnContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnContainerRect.pivot = new Vector2(0.5f, 0.5f);
        btnContainerRect.anchoredPosition = new Vector2(0, -100); // Shifted down
        btnContainerRect.sizeDelta = new Vector2(600, 100);

        HorizontalLayoutGroup layout = btnContainer.AddComponent<HorizontalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
        layout.spacing = 50f;
        layout.childAlignment = TextAnchor.MiddleCenter;

        // Create Exit Button
        Button exitBtn = CreateButton("ExitButton", btnContainerRect, exitSprite);
        // Create Retry Button
        Button retryBtn = CreateButton("RetryButton", btnContainerRect, retrySprite);

        // 5. Attach & Setup Manager
        GameOverUIManager manager = panelObj.AddComponent<GameOverUIManager>();
        
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("gameOverPanel").objectReferenceValue = panelObj;
        so.FindProperty("exitButton").objectReferenceValue = exitBtn;
        so.FindProperty("retryButton").objectReferenceValue = retryBtn;
        so.ApplyModifiedProperties();

        // 6. Delete old GameOverUIManager if it was scattered elsewhere
        GameOverUIManager[] oldManagers = Object.FindObjectsByType<GameOverUIManager>(FindObjectsSortMode.None);
        foreach (var oldMgr in oldManagers)
        {
            if (oldMgr != manager)
            {
                Object.DestroyImmediate(oldMgr.gameObject);
            }
        }

        Selection.activeGameObject = panelObj;
        Undo.RegisterCreatedObjectUndo(panelObj, "Setup Game Over UI");
        Debug.Log("[SetupGameOverUITool] Game Over UI generated successfully.");
    }

    private static Button CreateButton(string name, RectTransform parent, Sprite buttonSprite)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.sprite = buttonSprite;
        bgImg.preserveAspect = true;
        Button btn = btnObj.AddComponent<Button>();
        return btn;
    }
}
#endif
