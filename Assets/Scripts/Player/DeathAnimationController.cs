using UnityEngine;
using System.Collections;

namespace YourGame.Gameplay.Player
{
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

        public IEnumerator PlayAndRespawn(Vector3 respawnPosition, PlayerController player)
        {
            if (_deathSprites == null || _deathSprites.Length == 0)
            {
                Debug.LogWarning("[DeathAnimationController] No death sprites assigned! Performing instant respawn.");
                player.TeleportTo(respawnPosition);
                player.IsDead = false;
                yield break;
            }

            _animator.enabled = false;
            transform.localScale = _originalScale * _deathScale;
            _spriteRenderer.sprite = _deathSprites[0];
            yield return new WaitForSeconds(0.75f);

            int remainingFrames = _deathSprites.Length - 1;
            float frameDuration = remainingFrames > 0 ? (1.25f / remainingFrames) : 0.25f;

            for (int i = 1; i < _deathSprites.Length; i++)
            {
                _spriteRenderer.sprite = _deathSprites[i];
                yield return new WaitForSeconds(frameDuration);
            }

            transform.localScale = _originalScale;
            _animator.enabled = true;

            player.TeleportTo(respawnPosition);
            player.IsDead = false;
        }

        public IEnumerator DoHitFreeze(float freezeDuration)
        {
            if (_deathSprites == null || _deathSprites.Length == 0)
            {
                yield return new WaitForSeconds(freezeDuration);
                yield break;
            }

            _animator.enabled = false;
            transform.localScale = _originalScale * _deathScale;
            _spriteRenderer.sprite = _deathSprites[0];
            yield return new WaitForSeconds(freezeDuration);

            transform.localScale = _originalScale;
            _animator.enabled = true;
        }

        public IEnumerator PlayDeathAnimationOnly()
        {
            if (_deathSprites == null || _deathSprites.Length == 0) yield break;

            _animator.enabled = false;
            transform.localScale = _originalScale * _deathScale;
            _spriteRenderer.sprite = _deathSprites[0];
            yield return new WaitForSeconds(0.75f);

            int remainingFrames = _deathSprites.Length - 1;
            float frameDuration = remainingFrames > 0 ? (1.25f / remainingFrames) : 0.25f;

            for (int i = 1; i < _deathSprites.Length; i++)
            {
                _spriteRenderer.sprite = _deathSprites[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }
    }
}
