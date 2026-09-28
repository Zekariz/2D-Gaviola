using UnityEngine;
using YourGame.Gameplay.Level;

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
        
        [Header("Dynamic Bounds")]
        [SerializeField] private LevelBoundsManager _boundsManager;
        [SerializeField] private bool _useDynamicBounds = true;

        private Vector3 _velocity = Vector3.zero;
        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 targetPosition = _target.position + (Vector3)_offset;
            targetPosition.z = transform.position.z; // Keep camera Z

            if (_useDynamicBounds && _boundsManager != null && _cam != null)
            {
                Bounds b = _boundsManager.CurrentBounds;

                // Dynamic bounds clamp X only — the level's horizontal scroll limits.
                // Y follows the player freely; use _useBounds / _minBounds / _maxBounds for vertical clamping.
                if (b.size.sqrMagnitude > 0)
                {
                    float halfWidth = _cam.orthographicSize * _cam.aspect;

                    float boundsMinX = b.min.x + halfWidth;
                    float boundsMaxX = b.max.x - halfWidth;

                    if (boundsMinX > boundsMaxX)
                        targetPosition.x = b.center.x;  // level narrower than viewport — center it
                    else
                        targetPosition.x = Mathf.Clamp(targetPosition.x, boundsMinX, boundsMaxX);
                }
            }
            else if (_useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minBounds.x, _maxBounds.x);
            }

            // Always apply Y bounds when _useBounds is on (keeps player on-screen vertically).
            if (_useBounds)
            {
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minBounds.y, _maxBounds.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
        }
    }
}
