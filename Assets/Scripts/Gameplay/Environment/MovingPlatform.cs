using UnityEngine;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Environment
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Tooltip("How far and in what direction the platform moves from its start position.")]
        public Vector3 moveOffset = new Vector3(3f, 0f, 0f);
        
        [Tooltip("Movement speed in units per second.")]
        public float speed = 2f;
        
        public Vector2 Velocity { get; private set; }

        private Vector3 _startPos;
        private Vector3 _targetPos;
        private Rigidbody2D _rb;
        private bool _movingToTarget = true;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.useFullKinematicContacts = true;
            
            _startPos = transform.position;
            _targetPos = _startPos + moveOffset;
        }

        private void FixedUpdate()
        {
            Vector3 target = _movingToTarget ? _targetPos : _startPos;
            Vector3 newPos = Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime);
            
            Velocity = (newPos - (Vector3)_rb.position) / Time.fixedDeltaTime;
            
            _rb.MovePosition(newPos);

            if (Vector3.Distance(_rb.position, target) < 0.01f)
            {
                _movingToTarget = !_movingToTarget;
            }
        }
    }
}
