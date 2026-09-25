using System.Collections;
using UnityEngine;

namespace YourGame.Gameplay.Obstacles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 _pointA = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 _pointB = new Vector3(5, 0, 0);
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _pauseTime = 0.5f;

        private Rigidbody2D _rb;
        private Vector3 _targetPoint;
        private bool _isPaused = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Ensure physics settings are correct for a kinematic mover
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            // Set initial target
            _targetPoint = _pointB;
            transform.position = _pointA;
        }

        private void FixedUpdate()
        {
            if (_isPaused) return;

            Vector2 newPos = Vector2.MoveTowards(_rb.position, _targetPoint, _speed * Time.fixedDeltaTime);
            _rb.MovePosition(newPos);

            if (Vector2.Distance(_rb.position, _targetPoint) < 0.01f)
            {
                StartCoroutine(PauseAndSwapTarget());
            }
        }

        private IEnumerator PauseAndSwapTarget()
        {
            _isPaused = true;
            yield return new WaitForSeconds(_pauseTime);
            
            _targetPoint = (_targetPoint == _pointA) ? _pointB : _pointA;
            _isPaused = false;
        }

        // Parent the player so they move along with the platform smoothly
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player")
            {
                // Ensure they landed on top
                if (collision.contacts[0].normal.y < -0.5f)
                {
                    collision.transform.SetParent(transform);
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player")
            {
                collision.transform.SetParent(null);
            }
        }
        
        // Helpful gizmo for level design
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_pointA, _pointB);
            Gizmos.DrawSphere(_pointA, 0.2f);
            Gizmos.DrawSphere(_pointB, 0.2f);
        }
    }
}
