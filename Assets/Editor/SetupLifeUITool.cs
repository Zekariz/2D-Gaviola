#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YourGame.Gameplay.UI;
using YourGame.Gameplay.Player;

public static class SetupLifeUITool
{
    [MenuItem("Tools/Setup Player Life UI")]
    public static void GenerateUI()
    {
        // 1. Create a dedicated UI Canvas for the HUD
        GameObject canvasObj = GameObject.Find("PlayerUICanvas");
        if (canvasObj != null)
        {
            Object.DestroyImmediate(canvasObj); // Clear old one if re-running
        }
        
        canvasObj = new GameObject("PlayerUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Ensure it renders on top
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Need an EventSystem if one doesn't exist
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Load Sprites
        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/heart.png");
        
        Object[] sheetAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/SpriteSheet2D.png");
        Sprite portraitSprite = null;
        if (sheetAssets != null)
        {
            foreach (var obj in sheetAssets)
            {
                if (obj is Sprite s && s.name.EndsWith("_0")) 
                { 
                    portraitSprite = s; 
                    break; 
                }
            }
        }

        // 3. Create Container
        GameObject container = new GameObject("PlayerLifeUI");
        container.transform.SetParent(canvas.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        // Anchor to bottom-left exactly
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(0, 0); 
        
        PlayerLifeUI lifeUIScript = container.AddComponent<PlayerLifeUI>();

        // 4. Create Portrait
        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(containerRect, false);
        Image portraitImg = portraitObj.AddComponent<Image>();
        portraitImg.sprite = portraitSprite;
        // Make it stretch perfectly without distorting
        portraitImg.preserveAspect = true;
        
        RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0, 0);
        portraitRect.anchorMax = new Vector2(0, 0);
        portraitRect.pivot = new Vector2(0, 0);
        portraitRect.anchoredPosition = new Vector2(0, 0);
        // Set size to 250x250 (matches Photo 2 scale better)
        portraitRect.sizeDelta = new Vector2(250, 250);

        // 5. Create Hearts Layout
        GameObject heartsContainer = new GameObject("HeartsContainer");
        heartsContainer.transform.SetParent(containerRect, false);
        RectTransform heartsRect = heartsContainer.AddComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(0, 0);
        heartsRect.anchorMax = new Vector2(0, 0);
        heartsRect.pivot = new Vector2(0, 0);
        // Position it immediately to the right of the portrait, slightly above the bottom
        heartsRect.anchoredPosition = new Vector2(255, 30); 

        HorizontalLayoutGroup layout = heartsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.childControlHeight = false; // Don't force child height
        layout.childControlWidth = false;  // Don't force child width
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = -20f; // Negative spacing if hearts have empty transparent borders, or 10f if tightly cropped. Let's use 10f.
        layout.spacing = 15f; 
        layout.childAlignment = TextAnchor.MiddleLeft;

        // 6. Spawn 3 Hearts
        Image[] heartImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i}");
            heartObj.transform.SetParent(heartsRect, false);
            Image hImg = heartObj.AddComponent<Image>();
            hImg.sprite = heartSprite;
            hImg.preserveAspect = true;
            
            RectTransform hRect = heartObj.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(75, 75); // Fixed size

            heartImages[i] = hImg;
        }

        // 7. Serialize assignment for PlayerLifeUI
        SerializedObject so = new SerializedObject(lifeUIScript);
        SerializedProperty heartsProp = so.FindProperty("_hearts");
        heartsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = heartImages[i];
        }
        so.ApplyModifiedProperties();

        // 8. Auto-assign to PlayerHealth if Player exists
        PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            SerializedObject pso = new SerializedObject(playerHealth);
            pso.FindProperty("_lifeUI").objectReferenceValue = lifeUIScript;
            pso.ApplyModifiedProperties();
        }
        
        Selection.activeGameObject = container;
        Undo.RegisterCreatedObjectUndo(canvasObj, "Generate Player Life UI");
    }
}
#endif
