using UnityEngine;

namespace YourGame.Gameplay.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0.2f;
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1f);

        [Header("Bounds")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minBounds = new Vector2(-10f, -5f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(100f, 20f);

        private Vector3 _velocity = Vector3.zero;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 targetPosition = _target.position + (Vector3)_offset;
            targetPosition.z = transform.position.z; // Keep camera Z

            if (_useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minBounds.x, _maxBounds.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minBounds.y, _maxBounds.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
        }
    }
}
