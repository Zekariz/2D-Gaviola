using UnityEngine;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Core
{
    public class RespawnManager : MonoBehaviour
    {
        public static RespawnManager Instance { get; private set; }

        [SerializeField] private Transform _spawnPoint;
        private PlayerController _player;
        private float _startTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Find player if not assigned
            _player = FindObjectOfType<PlayerController>();
            _startTime = Time.time;
        }

        public void Respawn()
        {
            if (_player != null && _spawnPoint != null)
            {
                Debug.Log("[RespawnManager] Player died. Respawning at start...");
                _player.TeleportTo(_spawnPoint.position);
            }
        }

        public float GetElapsedTime()
        {
            return Time.time - _startTime;
        }
    }
}
