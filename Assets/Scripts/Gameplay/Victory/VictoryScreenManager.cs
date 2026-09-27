using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YourGame.Gameplay.Victory
{
    /// <summary>
    /// Singleton manager for the victory screen.
    /// Call <see cref="Show"/> once when the player reaches the finish flag.
    /// All animation uses <see cref="Time.unscaledDeltaTime"/> so it continues
    /// to run even after <c>Time.timeScale</c> is set to 0.
    /// </summary>
    public class VictoryScreenManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static VictoryScreenManager Instance { get; private set; }

        // ── Serialized References (wired by SetupVictoryScreenTool) ──────────
        [Header("UI References")]
        [SerializeField] private Image         _darkOverlay;
        [SerializeField] private RectTransform _victoryImageRect;

        [Header("Timing")]
        [Tooltip("How long the dark overlay fades from 0 to target opacity.")]
        [SerializeField] private float _overlayFadeDuration  = 0.5f;

        [Tooltip("Delay before the victory sprite starts rising.")]
        [SerializeField] private float _riseDelay            = 0.3f;

        [Tooltip("How long the victory sprite takes to rise to center.")]
        [SerializeField] private float _riseDuration         = 1.5f;

        [Header("Appearance")]
        [Tooltip("Target opacity for the dark overlay (0–1).")]
        [SerializeField] [Range(0f, 1f)] private float _overlayTargetAlpha = 0.7f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fires once after the victory sprite has settled at the center.</summary>
        public event Action OnVictoryShown;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _isShowing = false;

        // ── Unity Messages ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            HideImmediate();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the victory sequence. Idempotent — subsequent calls are no-ops.
        /// </summary>
        public void Show()
        {
            if (_isShowing) return;
            _isShowing = true;
            StartCoroutine(RunVictorySequence());
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void HideImmediate()
        {
            if (_darkOverlay != null)
            {
                Color c = _darkOverlay.color;
                c.a = 0f;
                _darkOverlay.color = c;
                _darkOverlay.gameObject.SetActive(false);
            }

            if (_victoryImageRect != null)
            {
                _victoryImageRect.anchoredPosition = StartPosition();
                _victoryImageRect.gameObject.SetActive(false);
            }
        }

        /// <summary>Y position below the canvas where the sprite starts.</summary>
        private Vector2 StartPosition()
        {
            // The canvas height at runtime — use a large fixed offset so we are always off-screen.
            return new Vector2(0f, -1200f);
        }

        private IEnumerator RunVictorySequence()
        {
            // ── Activate hidden elements ──────────────────────────────────────
            if (_darkOverlay != null) _darkOverlay.gameObject.SetActive(true);
            if (_victoryImageRect != null)
            {
                _victoryImageRect.anchoredPosition = StartPosition();
                _victoryImageRect.gameObject.SetActive(true);
            }

            // ── Phase 1: Fade overlay in ──────────────────────────────────────
            float elapsed = 0f;
            while (elapsed < _overlayFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // works at timeScale = 0
                float t = Mathf.Clamp01(elapsed / _overlayFadeDuration);

                if (_darkOverlay != null)
                {
                    Color c = _darkOverlay.color;
                    c.a = Mathf.Lerp(0f, _overlayTargetAlpha, t);
                    _darkOverlay.color = c;
                }

                yield return null;
            }

            // Snap overlay to final alpha
            if (_darkOverlay != null)
            {
                Color final = _darkOverlay.color;
                final.a = _overlayTargetAlpha;
                _darkOverlay.color = final;
            }

            // ── Phase 2: Wait before sprite starts rising ─────────────────────
            // Use the remainder of the delay (overlay fade already consumed some time).
            float remainingDelay = _riseDelay - _overlayFadeDuration;
            if (remainingDelay > 0f)
            {
                float waited = 0f;
                while (waited < remainingDelay)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // ── Phase 3: Slide sprite up to center ───────────────────────────
            Vector2 from   = StartPosition();
            Vector2 to     = Vector2.zero; // center (anchor is already 0.5, 0.5)
            elapsed = 0f;

            while (elapsed < _riseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _riseDuration);

                // Cubic ease-out: decelerates as it approaches center
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                if (_victoryImageRect != null)
                    _victoryImageRect.anchoredPosition = Vector2.LerpUnclamped(from, to, easedT);

                yield return null;
            }

            // Snap to exact center
            if (_victoryImageRect != null)
                _victoryImageRect.anchoredPosition = to;

            OnVictoryShown?.Invoke();
        }
    }
}
