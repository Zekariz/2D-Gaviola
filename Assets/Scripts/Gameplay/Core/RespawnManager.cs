using UnityEngine;
using System.Collections;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Core
{
    public class RespawnManager : MonoBehaviour
    {
        public static RespawnManager Instance { get; private set; }

        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private AudioClip _deathSound;
        
        private PlayerController _player;
        private float _startTime;
        private AudioSource _audioSource;

        private Vector3 _currentRespawnPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        private void Start()
        {
            // Find player if not assigned
            _player = FindAnyObjectByType<PlayerController>();
            _startTime = Time.time;
            
            if (_spawnPoint != null)
                _currentRespawnPosition = _spawnPoint.position;
        }

        public void SetRespawnPoint(Vector3 newPosition)
        {
            _currentRespawnPosition = newPosition;
        }

        public Vector3 GetSafeRespawnPosition()
        {
            return _currentRespawnPosition;
        }

        public void Respawn()
        {
            if (_player != null && !_player.IsDead)
            {
                Debug.Log("[RespawnManager] Player died. Playing death animation...");
                _player.IsDead = true;
                PlayDeathSound();

                var deathAnim = _player.GetComponent<DeathAnimationController>();
                if (deathAnim != null)
                {
                    StartCoroutine(deathAnim.PlayAndRespawn(_currentRespawnPosition, _player));
                }
                else
                {
                    // Fallback if script isn't attached: instant respawn after 2s.
                    Debug.LogWarning("[RespawnManager] DeathAnimationController not found on Player!");
                    StartCoroutine(FallbackRespawn());
                }
            }
        }

        public void ForceRespawn()
        {
            if (_player != null)
            {
                if (_player.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = Vector2.zero;
                }
                _player.TeleportTo(_currentRespawnPosition);
                _player.IsDead = false;
            }
        }

        private IEnumerator FallbackRespawn()
        {
            yield return new WaitForSeconds(2.0f);
            _player.TeleportTo(_currentRespawnPosition);
            _player.IsDead = false;
        }

        private void PlayDeathSound()
        {
            if (_deathSound != null)
            {
                _audioSource.clip = _deathSound;
                _audioSource.time = 0.30f; // Start exactly at 0.30 seconds
                _audioSource.Play();
                
                StopAllCoroutines();
                // Play until the 3 second mark (3.0s - 0.3s = 2.7s duration)
                StartCoroutine(StopSoundAfterDelay(2.7f));
            }
        }

        private IEnumerator StopSoundAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        public float GetElapsedTime()
        {
            return Time.time - _startTime;
        }
    }
}


 

