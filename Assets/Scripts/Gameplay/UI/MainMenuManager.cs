using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YourGame.Gameplay.UI
{
    /// <summary>
    /// Fully self-contained Main Menu manager.
    /// Creates its own Canvas and all UI elements at runtime — no Editor tool required.
    /// Attach this script to any GameObject in the scene (e.g. "MainMenuManager").
    /// Assign the 6 sprites in the Inspector, then press Play.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static MainMenuManager Instance { get; private set; }

        // ── Inspector fields ─────────────────────────────────────────────────────
        [Header("Sprites — assign in Inspector")]
        [SerializeField] private Sprite _bgSprite;            // main-menu_0  (background panel)
        [SerializeField] private Sprite _hamburgerSprite;     // main-menu_1  (open button)
        [SerializeField] private Sprite _resumeSprite;        // main-menu_2
        [SerializeField] private Sprite _rfpSprite;           // Restart From Checkpoint button
        [SerializeField] private Sprite _restartSprite;       // main-menu_3
        [SerializeField] private Sprite _settingsSprite;      // main-menu_4
        [SerializeField] private Sprite _exitSprite;          // main-menu_5

        [Header("Animation")]
        [SerializeField] private float _animDuration        = 0.45f;
        [SerializeField] [Range(0f, 1f)] private float _overlayAlpha = 0.6f;

        [Header("UI Scaling")]
        [Tooltip("Width and Height of the background panel (main-menu_0). Adjust this to scale the menu!")]
        [SerializeField] private Vector2 _panelSize = new Vector2(560f, 600f);

        // ── Runtime UI refs (built in Awake) ─────────────────────────────────────
        private Image          _overlay;
        private RectTransform  _panel;
        private GameObject     _menuContainer;

        // ── State ────────────────────────────────────────────────────────────────
        private bool _isOpen      = false;
        private bool _isAnimating = false;

        private static readonly Vector2 _hiddenPos = new Vector2(0f, -2000f);
        private static readonly Vector2 _shownPos  = Vector2.zero;

        // ── Unity Messages ───────────────────────────────────────────────────────
        private void Awake()
        {
            // Singleton guard — destroy any duplicate
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUI();
        }

        private void Start()
        {
            // Ensure the menu is fully hidden when the game starts
            _menuContainer.SetActive(false);
            SetOverlayAlpha(0f);
            _overlay.gameObject.SetActive(false);
            _panel.anchoredPosition = _hiddenPos;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Toggle();
        }

        // ── Public API ───────────────────────────────────────────────────────────
        public void Toggle()
        {
            if (_isAnimating) return;
            if (_isOpen) ResumeGame();
            else         OpenMenu();
        }

        public void ResumeGame()
        {
            if (_isAnimating) return;
            _isOpen = false;
            StartCoroutine(AnimateMenu(opening: false));
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void SettingsGame()
        {
            Debug.Log("[MainMenuManager] Settings — On Hold");
        }

        public void QuitGame()
        {
            Debug.Log("[MainMenuManager] Quit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Private: open ────────────────────────────────────────────────────────
        private void OpenMenu()
        {
            _isOpen = true;
            Time.timeScale = 0f;
            _menuContainer.SetActive(true);
            _overlay.gameObject.SetActive(true);
            SetOverlayAlpha(0f);
            _panel.anchoredPosition = _hiddenPos;
            StartCoroutine(AnimateMenu(opening: true));
        }

        // ── Private: animation ───────────────────────────────────────────────────
        private IEnumerator AnimateMenu(bool opening)
        {
            _isAnimating = true;

            Vector2 startPos   = opening ? _hiddenPos : _shownPos;
            Vector2 targetPos  = opening ? _shownPos  : _hiddenPos;
            float   startAlpha = opening ? 0f         : _overlayAlpha;
            float   endAlpha   = opening ? _overlayAlpha : 0f;
            float   elapsed    = 0f;

            while (elapsed < _animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t      = Mathf.Clamp01(elapsed / _animDuration);
                float eased  = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out

                _panel.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
                SetOverlayAlpha(Mathf.Lerp(startAlpha, endAlpha, eased));
                yield return null;
            }

            _panel.anchoredPosition = targetPos;
            SetOverlayAlpha(endAlpha);

            if (!opening)
            {
                _menuContainer.SetActive(false);
                _overlay.gameObject.SetActive(false);
                Time.timeScale = 1f;
            }

            _isAnimating = false;
        }

        private void SetOverlayAlpha(float a)
        {
            Color c = _overlay.color;
            c.a = a;
            _overlay.color = c;
        }

        // ── Private: UI construction (runs once in Awake) ────────────────────────
        private void BuildUI()
        {
            // Root canvas
            var canvasGO = new GameObject("_MainMenuCanvas");
            canvasGO.transform.SetParent(transform, false); // child of this GO for cleanliness
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Hamburger button — always visible, top-right corner
            var hGO   = MakeImage("HamburgerButton", canvasGO.transform, _hamburgerSprite);
            var hRect = hGO.GetComponent<RectTransform>();
            hRect.anchorMin        = new Vector2(1f, 1f);
            hRect.anchorMax        = new Vector2(1f, 1f);
            hRect.pivot            = new Vector2(1f, 1f);
            hRect.anchoredPosition = new Vector2(-30f, -30f);
            hRect.sizeDelta        = new Vector2(90f, 90f);
            var hBtn = hGO.AddComponent<Button>();
            hBtn.onClick.AddListener(Toggle);         // ← reliable C# listener

            // Dark overlay (full-screen, visual only)
            var overlayGO = MakeImage("DarkOverlay", canvasGO.transform, null);
            _overlay = overlayGO.GetComponent<Image>();
            _overlay.color        = new Color(0f, 0f, 0f, 0f);
            _overlay.raycastTarget = false;
            Stretch(overlayGO.GetComponent<RectTransform>());

            // Container (hidden at start)
            _menuContainer = new GameObject("MenuContainer");
            _menuContainer.transform.SetParent(canvasGO.transform, false);
            Stretch(_menuContainer.AddComponent<RectTransform>());

            // Background panel
            var panelGO = MakeImage("MenuPanel", _menuContainer.transform, _bgSprite);
            panelGO.GetComponent<Image>().type = Image.Type.Simple;
            _panel                 = panelGO.GetComponent<RectTransform>();
            _panel.anchorMin       = new Vector2(0.5f, 0.5f);
            _panel.anchorMax       = new Vector2(0.5f, 0.5f);
            _panel.pivot           = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta       = _panelSize;

            // Vertical layout for buttons
            var layoutGO   = new GameObject("ButtonsLayout");
            layoutGO.transform.SetParent(panelGO.transform, false);
            var layoutRect = layoutGO.AddComponent<RectTransform>();
            layoutRect.anchorMin   = Vector2.zero;
            layoutRect.anchorMax   = Vector2.one;
            layoutRect.offsetMin   = new Vector2(50f, 50f);
            layoutRect.offsetMax   = new Vector2(-50f, -140f); // Top offset of 140 to make room for MAIN-MENU banner
            var vg = layoutGO.AddComponent<VerticalLayoutGroup>();
            vg.childAlignment      = TextAnchor.UpperCenter;
            vg.spacing             = 15f;
            vg.childControlWidth   = true; 
            vg.childControlHeight  = true; 
            vg.childForceExpandWidth  = false;
            vg.childForceExpandHeight = false;

            // The 5 buttons — using C# AddListener (never breaks at runtime)
            MakeButton("ResumeButton",   _resumeSprite,   layoutGO.transform, ResumeGame);
            MakeButton("RFPButton",      _rfpSprite,      layoutGO.transform, RestartFromCheckpointGame);
            MakeButton("RestartButton",  _restartSprite,  layoutGO.transform, RestartGame);
            MakeButton("SettingsButton", _settingsSprite, layoutGO.transform, SettingsGame);
            MakeButton("ExitButton",     _exitSprite,     layoutGO.transform, QuitGame);
        }

        public void RestartFromCheckpointGame()
        {
            Debug.Log("[MainMenuManager] Restarting from Checkpoint");
            Time.timeScale = 1f;

            if (YourGame.Gameplay.Core.RespawnManager.Instance != null)
            {
                YourGame.Gameplay.Core.RespawnManager.Instance.ForceRespawn();
            }

            _isOpen = false;
            StartCoroutine(AnimateMenu(opening: false));
        }

        // ── UI helpers ───────────────────────────────────────────────────────────
        private static GameObject MakeImage(string goName, Transform parent, Sprite sprite)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            if (sprite != null) 
            {
                img.sprite = sprite;
                img.SetNativeSize();
            }
            return go;
        }

        private static GameObject MakeButton(string goName, Sprite sprite, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var go  = MakeImage(goName, parent, sprite);
            
            if (sprite != null)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = 300f; // Exactly ~120px scaled up to fit the 560px width panel
                le.preferredHeight = 80f; // Exactly ~32px scaled up
            }

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(action);   // ← 100 % reliable runtime wiring
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
        }
    }
}
