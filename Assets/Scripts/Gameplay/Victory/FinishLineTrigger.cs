using UnityEngine;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Victory
{
    /// <summary>
    /// Attach to the finish-flag GameObject.
    /// When the player's physics body enters the trigger, calls
    /// <see cref="VictoryScreenManager.Show"/> and freezes the player.
    /// Fires only once — subsequent touches are ignored.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FinishLineTrigger : MonoBehaviour
    {
        private bool _triggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;

            // Detect by component — no tag dependency.
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
                player = other.GetComponentInParent<PlayerController>();

            if (player == null) return;

            _triggered = true;

            // ── Freeze the player ─────────────────────────────────────────────
            player.SetInputEnabled(false);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            // ── Pause game time (animation uses unscaledDeltaTime) ────────────
            Time.timeScale = 0f;

            // ── Fade background music ─────────────────────────────────────────
            BackgroundMusicFader.FadeOut();

            // ── Show victory screen ───────────────────────────────────────────
            if (VictoryScreenManager.Instance != null)
            {
                VictoryScreenManager.Instance.Show();
            }
            else
            {
                Debug.LogWarning("[FinishLineTrigger] VictoryScreenManager not found in scene. " +
                                 "Run Tools → 2D-Gaviola → Setup → Create Victory Screen.");
            }
        }
    }
}
