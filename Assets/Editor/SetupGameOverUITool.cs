using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YourGame.Gameplay.UI;

public class SetupGameOverUITool
{
    [MenuItem("Tools/Setup Game Over UI")]
    public static void RunSetup()
    {
        // 1. Find or create the Canvas
        GameObject canvasGO = GameObject.Find("GameOverCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("GameOverCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // Above HUD and other elements
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // 2. Setup Manager
        GameOverUIManager manager = canvasGO.GetComponent<GameOverUIManager>();
        if (manager == null) manager = canvasGO.AddComponent<GameOverUIManager>();

        // 3. Create Dim Background
        Transform dimTrans = canvasGO.transform.Find("DimBackground");
        GameObject dimGO;
        if (dimTrans == null)
        {
            dimGO = new GameObject("DimBackground");
            dimGO.transform.SetParent(canvasGO.transform, false);
            Image dimImg = dimGO.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.75f);
            
            // Stretch to fill screen
            RectTransform dimRect = dimGO.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
        }
        else
        {
            dimGO = dimTrans.gameObject;
        }

        // 4. Create Content Container (Vertical layout for Banner + Buttons)
        Transform contentTrans = dimGO.transform.Find("Content");
        GameObject contentGO;
        if (contentTrans == null)
        {
            contentGO = new GameObject("Content");
            contentGO.transform.SetParent(dimGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(600, 400);

            VerticalLayoutGroup vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.spacing = 30f;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
        }
        else
        {
            contentGO = contentTrans.gameObject;
        }

        // Load Sprites
        Sprite bannerSprite = null;
        Sprite exitSprite = null;
        Sprite retrySprite = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/game-over.png"))
        {
            if (asset is Sprite s)
            {
                if (s.name == "game-over_0") bannerSprite = s;
                // Assuming game-over_1 is Exit (left) and game-over_2 is Retry (right) based on typical layout.
                if (s.name == "game-over_1") exitSprite = s;
                if (s.name == "game-over_2") retrySprite = s;
            }
        }

        // 5. Create Banner
        Transform bannerTrans = contentGO.transform.Find("Banner");
        GameObject bannerGO;
        if (bannerTrans == null)
        {
            bannerGO = new GameObject("Banner");
            bannerGO.transform.SetParent(contentGO.transform, false);
            Image bannerImg = bannerGO.AddComponent<Image>();
            bannerImg.sprite = bannerSprite;
            bannerImg.SetNativeSize();
        }
        else
        {
            bannerGO = bannerTrans.gameObject;
        }

        // 6. Create Buttons Container
        Transform btnContTrans = contentGO.transform.Find("ButtonsContainer");
        GameObject btnContGO;
        if (btnContTrans == null)
        {
            btnContGO = new GameObject("ButtonsContainer");
            btnContGO.transform.SetParent(contentGO.transform, false);
            HorizontalLayoutGroup hLayout = btnContGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 20f;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            btnContGO.AddComponent<RectTransform>().sizeDelta = new Vector2(600, 150);
        }
        else
        {
            btnContGO = btnContTrans.gameObject;
        }

        // 7. Create Exit Button
        Transform exitTrans = btnContGO.transform.Find("ExitButton");
        GameObject exitGO;
        if (exitTrans == null)
        {
            exitGO = new GameObject("ExitButton");
            exitGO.transform.SetParent(btnContGO.transform, false);
            Image exitImg = exitGO.AddComponent<Image>();
            exitImg.sprite = exitSprite;
            exitImg.SetNativeSize();
            exitGO.AddComponent<Button>();
        }
        else
        {
            exitGO = exitTrans.gameObject;
        }

        // 8. Create Retry Button
        Transform retryTrans = btnContGO.transform.Find("RetryButton");
        GameObject retryGO;
        if (retryTrans == null)
        {
            retryGO = new GameObject("RetryButton");
            retryGO.transform.SetParent(btnContGO.transform, false);
            Image retryImg = retryGO.AddComponent<Image>();
            retryImg.sprite = retrySprite;
            retryImg.SetNativeSize();
            retryGO.AddComponent<Button>();
        }
        else
        {
            retryGO = retryTrans.gameObject;
        }

        // 9. Wire Manager
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("gameOverPanel").objectReferenceValue = dimGO;
        so.FindProperty("retryButton").objectReferenceValue = retryGO.GetComponent<Button>();
        so.FindProperty("exitButton").objectReferenceValue = exitGO.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // 10. Hide by default
        dimGO.SetActive(false);

        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("[SetupGameOverUI] Game Over UI successfully generated!");
    }
}
