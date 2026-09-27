using UnityEngine;
using YourGame.Gameplay.Core;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        private bool _isActivated = false;
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!_isActivated && collision.GetComponent<PlayerController>() != null)
            {
                _isActivated = true;
                RespawnManager.Instance.SetRespawnPoint(transform.position);
                Debug.Log("Checkpoint activated!");
                
                if (_animator != null)
                {
                    _animator.SetTrigger("Activate"); // Optional if they add animation
                }
            }
        }
    }
}
