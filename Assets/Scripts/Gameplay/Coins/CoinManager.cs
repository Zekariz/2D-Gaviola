using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YourGame.Gameplay.Coins
{
    /// <summary>
    /// Singleton that manages the coin counter UI and coin collection sound.
    ///
    /// Sound behaviour:
    ///  - Plays up to 0.80 seconds of the clip.
    ///  - If another coin is collected while the sound is still playing,
    ///    the current playback is cut and restarted from the beginning.
    ///
    /// UI is generated fully at runtime — no Inspector wiring needed.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CoinManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static CoinManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Assets (auto-assigned by SetupCoinSystemTool)")]
        public Sprite coinSprite;
        public AudioClip coinSound;

        [Header("Sound")]
        [Tooltip("Max number of seconds the coin SFX is allowed to play.")]
        public float maxSoundDuration = 0.80f;

        [Header("State (read-only)")]
        public int currentCoins = 0;

        // ── Private ───────────────────────────────────────────────────────────────
        private AudioSource _audioSource;
        private Text _coinText;
        private Coroutine _stopSoundCoroutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            SetupUI();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Call this whenever a Coin is collected.</summary>
        public void AddCoin()
        {
            currentCoins++;
            UpdateCoinText();
            PlayCoinSound();
        }

        // ── Sound ─────────────────────────────────────────────────────────────────
        private void PlayCoinSound()
        {
            if (coinSound == null || _audioSource == null) return;

            // Stop any previous play (cuts ongoing sound) and restart
            if (_stopSoundCoroutine != null)
                StopCoroutine(_stopSoundCoroutine);

            _audioSource.Stop();
            _audioSource.clip = coinSound;
            _audioSource.Play();

            // Schedule a stop after maxSoundDuration seconds
            _stopSoundCoroutine = StartCoroutine(StopAfterDelay(maxSoundDuration));
        }

        private IEnumerator StopAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay); // unscaled so pausing doesn't affect it
            _audioSource.Stop();
            _stopSoundCoroutine = null;
        }

        // ── UI ────────────────────────────────────────────────────────────────────
        private void SetupUI()
        {
            // Canvas
            GameObject canvasGO = new GameObject("CoinCanvas");
            canvasGO.transform.SetParent(transform);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Container — anchored to top-left
            GameObject container = new GameObject("CoinContainer");
            container.transform.SetParent(canvasGO.transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot    = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(20, -20);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment    = TextAnchor.MiddleLeft;
            layout.spacing           = 10f;
            layout.childControlWidth  = false;
            layout.childControlHeight = false;

            // Coin icon
            GameObject iconGO = new GameObject("CoinIcon");
            iconGO.transform.SetParent(container.transform, false);
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = coinSprite;
            iconImage.SetNativeSize();

            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            float ratio = iconRect.sizeDelta.x > 0 ? iconRect.sizeDelta.x / iconRect.sizeDelta.y : 1f;
            iconRect.sizeDelta = new Vector2(60f * ratio, 60f);

            // Count text
            GameObject textGO = new GameObject("CoinText");
            textGO.transform.SetParent(container.transform, false);

            _coinText = textGO.AddComponent<Text>();
            _coinText.font     = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _coinText.fontSize = 40;
            _coinText.color    = Color.white;

            Shadow shadow = textGO.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);

            ContentSizeFitter fitter = textGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            UpdateCoinText();
        }

        private void UpdateCoinText()
        {
            if (_coinText != null)
                _coinText.text = "x " + currentCoins;
        }
    }
}
