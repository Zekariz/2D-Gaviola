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

        public void Respawn()
        {
            if (_player != null)
            {
                Debug.Log("[RespawnManager] Player died. Respawning...");
                _player.TeleportTo(_currentRespawnPosition);
                
                PlayDeathSound();
            }
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
