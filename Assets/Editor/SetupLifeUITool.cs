using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YourGame.Gameplay.UI;
using YourGame.Gameplay.Player;

public class SetupLifeUITool
{
    [MenuItem("Tools/Setup Player Life UI")]
    public static void RunSetup()
    {
        // 1. Find the Player
        PlayerHealth playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[SetupLifeUI] PlayerHealth not found in scene. Please add it to your player first.");
            return;
        }

        // 2. Setup the Canvas
        GameObject canvasGO = GameObject.Find("PlayerLifeCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("PlayerLifeCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // 3. Setup the PlayerLifeUI component
        PlayerLifeUI lifeUI = canvasGO.GetComponent<PlayerLifeUI>();
        if (lifeUI == null)
        {
            lifeUI = canvasGO.AddComponent<PlayerLifeUI>();
        }

        // 4. Create the Container for the Hearts and Portrait
        Transform containerTrans = canvasGO.transform.Find("AvatarContainer");
        GameObject container;
        if (containerTrans == null)
        {
            container = new GameObject("AvatarContainer");
            container.transform.SetParent(canvasGO.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 0); // Bottom-Right
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(1, 0);
            containerRect.anchoredPosition = new Vector2(-20, 20); // Margin
            
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
        }
        else
        {
            container = containerTrans.gameObject;
        }

        // 5. Load Sprites
        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/heart.png");
        Sprite avatarSprite = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/SpriteSheet2D.png"))
        {
            if (asset is Sprite s && s.name == "SpriteSheet2D_0") avatarSprite = s;
        }

        // 6. Generate 3 Hearts
        Image[] hearts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Transform heartTrans = container.transform.Find($"Heart_{i}");
            GameObject heartGO;
            if (heartTrans == null)
            {
                heartGO = new GameObject($"Heart_{i}");
                heartGO.transform.SetParent(container.transform, false);
                heartGO.transform.SetAsFirstSibling();
            }
            else
            {
                heartGO = heartTrans.gameObject;
            }

            Image heartImg = heartGO.GetComponent<Image>();
            if (heartImg == null) heartImg = heartGO.AddComponent<Image>();
            
            heartImg.sprite = heartSprite;
            heartImg.SetNativeSize();
            
            RectTransform hr = heartGO.GetComponent<RectTransform>();
            hr.sizeDelta = new Vector2(50, 50);
            hearts[i] = heartImg;
        }

        // 7. Connect Hearts to PlayerLifeUI
        SerializedObject so = new SerializedObject(lifeUI);
        SerializedProperty prop = so.FindProperty("_hearts");
        prop.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = hearts[i];
        }
        so.ApplyModifiedProperties();

        // 8. Generate Avatar Icon
        Transform avatarTrans = container.transform.Find("AvatarIcon");
        GameObject avatarGO;
        if (avatarTrans == null)
        {
            avatarGO = new GameObject("AvatarIcon");
            avatarGO.transform.SetParent(container.transform, false);
            avatarGO.transform.SetAsLastSibling();
        }
        else
        {
            avatarGO = avatarTrans.gameObject;
        }

        Image avatarImg = avatarGO.GetComponent<Image>();
        if (avatarImg == null) avatarImg = avatarGO.AddComponent<Image>();
        avatarImg.sprite = avatarSprite;
        avatarImg.SetNativeSize();
        
        RectTransform ar = avatarGO.GetComponent<RectTransform>();
        float ratio = ar.sizeDelta.x > 0 ? ar.sizeDelta.x / ar.sizeDelta.y : 1f;
        ar.sizeDelta = new Vector2(80f * ratio, 80f);

        // 9. Wire PlayerLifeUI back to PlayerHealth
        SerializedObject phSO = new SerializedObject(playerHealth);
        phSO.FindProperty("_lifeUI").objectReferenceValue = lifeUI;
        phSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(playerHealth);
        EditorUtility.SetDirty(lifeUI);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("[SetupLifeUI] Player Life UI successfully generated and wired to PlayerHealth!");
    }
}
