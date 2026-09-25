using System.Collections;
using UnityEngine;

namespace YourGame.Gameplay.Obstacles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class FallingPlatform : MonoBehaviour
    {
        [SerializeField] private float _fallDelay = 0.4f;
        [SerializeField] private float _destroyDelay = 1.5f;

        private Rigidbody2D _rb;
        private Vector3 _startPosition;
        private bool _isFalling = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _startPosition = transform.position;
            
            // Ensure starting state is Kinematic
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isFalling) return;

            // Simple check to ensure it's the player and they landed on top
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player")
            {
                // Only trigger if player is above the platform
                if (collision.contacts[0].normal.y < -0.5f)
                {
                    StartCoroutine(FallSequence());
                }
            }
        }

        private IEnumerator FallSequence()
        {
            _isFalling = true;
            yield return new WaitForSeconds(_fallDelay);

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1f;

            yield return new WaitForSeconds(_destroyDelay);

            // Instead of Destroy, we reset so the course can be retried without scene reload
            ResetPlatform();
        }

        public void ResetPlatform()
        {
            StopAllCoroutines();
            _isFalling = false;
            
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            transform.position = _startPosition;
            transform.rotation = Quaternion.identity;
        }
    }
}
