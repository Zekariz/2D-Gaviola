using UnityEngine;
using System.Collections;
using YourGame.Gameplay.UI;
using YourGame.Gameplay.Core;

namespace YourGame.Gameplay.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _maxLives = 3;
        [SerializeField] private float _invincibilityDuration = 2f;
        [SerializeField] private float _flickerInterval = 0.1f;
        
        [Header("References")]
        [SerializeField] private PlayerLifeUI _lifeUI;
        
        private SpriteRenderer _spriteRenderer;
        private int _currentLives;
        private bool _isInvincible = false;
        
        private PlayerController _playerController;
        private DeathAnimationController _deathAnimController;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _deathAnimController = GetComponent<DeathAnimationController>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            
            _currentLives = _maxLives;
        }

        private void Start()
        {
            if (_lifeUI != null)
            {
                _lifeUI.Initialize(_maxLives);
            }
        }

        public void TakeDamage(bool teleportToRespawn = false)
        {
            if (_isInvincible || _playerController.IsDead) return;

            _currentLives--;
            if (_lifeUI != null) _lifeUI.LoseLife(_currentLives);

            if (_currentLives <= 0)
            {
                StartCoroutine(HandleGameOverSequence());
            }
            else
            {
                StartCoroutine(HandleHitSequence(teleportToRespawn));
            }
        }

        private IEnumerator HandleGameOverSequence()
        {
            _playerController.IsDead = true;

            if (RespawnManager.Instance != null)
                RespawnManager.Instance.PlayDeathSound();

            if (_deathAnimController != null)
                yield return StartCoroutine(_deathAnimController.PlayDeathAnimationOnly());
            else
                yield return new WaitForSeconds(2f);

            if (YourGame.Gameplay.UI.GameOverUIManager.Instance != null)
            {
                YourGame.Gameplay.UI.GameOverUIManager.Instance.Show();
            }
            else
            {
                RespawnManager.Instance.ForceRespawn();
                _currentLives = _maxLives;
                if (_lifeUI != null) _lifeUI.Initialize(_maxLives);
            }
        }

        private IEnumerator HandleHitSequence(bool teleportToRespawn)
        {
            _isInvincible = true;
            _playerController.SetInputEnabled(false);

            if (RespawnManager.Instance != null)
                RespawnManager.Instance.PlayDeathSound();
            
            if (_deathAnimController != null)
            {
                yield return StartCoroutine(_deathAnimController.DoHitFreeze(0.5f));
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            if (teleportToRespawn)
            {
                Vector3 respawnPos = RespawnManager.Instance.GetSafeRespawnPosition();
                _playerController.TeleportTo(respawnPos);
            }
            
            _playerController.SetInputEnabled(true);

            StartCoroutine(FlickerSprite(_invincibilityDuration));
            yield return new WaitForSeconds(_invincibilityDuration);
            
            _isInvincible = false;
        }

        private IEnumerator FlickerSprite(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_spriteRenderer != null)
                    _spriteRenderer.enabled = !_spriteRenderer.enabled;
                
                yield return new WaitForSeconds(_flickerInterval);
                elapsed += _flickerInterval;
            }
            
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = true;
        }
    }
}
 




