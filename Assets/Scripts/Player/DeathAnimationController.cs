using UnityEngine;
using System.Collections;

namespace YourGame.Gameplay.Player
{
    /// <summary>
    /// Handles the death animation by directly swapping sprites via code,
    /// bypassing Unity's Animator entirely for the death sequence.
    /// Attach to the Player GameObject.
    ///
    /// Setup in Inspector:
    ///   1. Assign death0 through death5 sprites (in order) to Death Sprites.
    ///   2. Adjust Death Scale in the Inspector until the size matches the walking character.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Animator))]
    public class DeathAnimationController : MonoBehaviour
    {
        [Header("Death Sprites (assign death0 – death5 in order)")]
        [SerializeField] private Sprite[] _deathSprites;

        [Header("Scale during death animation (tweak until size matches walk)")]
        [SerializeField] private float _deathScale = 1.35f;

        private SpriteRenderer _spriteRenderer;
        private Animator       _animator;
        private Vector3        _originalScale;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _animator       = GetComponent<Animator>();
            _originalScale  = transform.localScale;
        }

        /// <summary>
        /// Called by RespawnManager. Plays the full death animation,
        /// then teleports the player to respawnPosition and unfreezes them.
        /// </summary>
        public IEnumerator PlayAndRespawn(Vector3 respawnPosition, PlayerController player)
        {
            if (_deathSprites == null || _deathSprites.Length == 0)
            {
                Debug.LogWarning("[DeathAnimationController] No death sprites assigned! Performing instant respawn.");
                player.TeleportTo(respawnPosition);
                player.IsDead = false;
                yield break;
            }

            // 1. Disable animator so it doesn't fight us for sprite control.
            _animator.enabled = false;

            // 2. Scale up to compensate for transparent padding in death sprites.
            transform.localScale = _originalScale * _deathScale;

            // 3. Frame 0 – freeze for 0.75 seconds.
            _spriteRenderer.sprite = _deathSprites[0];
            yield return new WaitForSeconds(0.75f);

            // 4. Frames 1-5 – spread evenly across 1.25 seconds.
            int remainingFrames = _deathSprites.Length - 1;
            float frameDuration = remainingFrames > 0 ? (1.25f / remainingFrames) : 0.25f;

            for (int i = 1; i < _deathSprites.Length; i++)
            {
                _spriteRenderer.sprite = _deathSprites[i];
                yield return new WaitForSeconds(frameDuration);
            }

            // 5. Restore scale and re-enable animator.
            transform.localScale = _originalScale;
            _animator.enabled = true;

            // 6. Teleport to checkpoint and give back control.
            player.TeleportTo(respawnPosition);
            player.IsDead = false;
        }
    }
}
