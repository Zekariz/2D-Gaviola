using System;
using UnityEngine;
using YourGame.Gameplay.Level;

namespace YourGame.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // ── Movement ──────────────────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float _walkSpeed        = 6f;
        [SerializeField] private float _runSpeed         = 10f;
        [SerializeField] private float _acceleration     = 60f;
        [SerializeField] private float _deceleration     = 80f;
        [SerializeField] private float _doubleTapWindow  = 0.25f;

        // ── Jump ──────────────────────────────────────────────────────────────
        [Header("Jump")]
        [SerializeField] private float _jumpForce            = 12f;
        [SerializeField] private float _coyoteTime           = 0.1f;
        [SerializeField] private float _jumpBufferTime       = 0.1f;
        [SerializeField] private float _fallGravityMultiplier = 2.5f;

        // ── Ground Detection ──────────────────────────────────────────────────
        [Header("Grounding")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float     _groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask _groundLayer;

        // ── Internal References ───────────────────────────────────────────────
        private Rigidbody2D _rb;

        // ── Raw Input State ───────────────────────────────────────────────────
        private float _moveInput;
        private bool  _isHoldingA;
        private bool  _isHoldingD;

        // ── Double-Tap Run Detection ──────────────────────────────────────────
        private float _lastADownTime = -999f;
        private float _lastDDownTime = -999f;

        // ── Snag-Recovery ─────────────────────────────────────────────────────
        // PRIMARY fix: change CompositeCollider2D on the ground tilemap from
        //   Geometry Type = Polygons  ->  Geometry Type = Outlines
        // That eliminates the corner impulse at tile seams entirely.
        //

        // ── Jump Timers ───────────────────────────────────────────────────────
        private float _coyoteCounter;
        private float _jumpBufferCounter;

        // ── Public State (consumed by PlayerAnimatorDriver) ───────────────────
        public bool  IsGrounded    { get; private set; }
        public bool  IsRunning     { get; private set; }
        public bool  FacingLeft    { get; private set; }
        public float HorizontalSpeed => Mathf.Abs(_rb.linearVelocity.x - _surfaceVelocity.x);   // always >= 0
        public float VelocityY       => _rb.linearVelocity.y;

        /// <summary>Fires on the exact frame a jump executes.</summary>
        public event Action OnJumped;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb        = GetComponent<Rigidbody2D>();
            FacingLeft = false;

            // Surface this early so it doesn't silently break grounding.
            if (_groundCheck == null)
                Debug.LogWarning(
                    "[PlayerController] _groundCheck is not assigned on " + gameObject.name +
                    ". Run Tools > Fix Player Setup to wire it automatically.");
        }

        // Input polling always in Update — GetKeyDown is silently lost in FixedUpdate.
        private void Update()
        {
            GatherInput();
            UpdateGroundAndTimers();
            HandleJump();
        }

        private void FixedUpdate()
        {
            ApplyHorizontalMovement();
            ApplyFallGravity();
            ClampToBounds();
        }

        // ── Input Enable / Disable (used by VictoryScreen) ────────────────────
        private bool _inputEnabled = true;

        /// <summary>
        /// Enables or disables all player input. Call with <c>false</c> to freeze
        /// the player at the finish line while the victory screen is shown.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                // Zero out all movement state immediately
                _moveInput        = 0f;
                _isHoldingA       = false;
                _isHoldingD       = false;
                _jumpBufferCounter = 0f;
                IsRunning         = false;
            }
        }

        // ── Input ─────────────────────────────────────────────────────────────
        private void GatherInput()
        {
            // Victory / UI screen — drop all input
            if (!_inputEnabled)
            {
                _moveInput = 0f;
                _isHoldingA = false;
                _isHoldingD = false;
                return;
            }

            _isHoldingA = Input.GetKey(KeyCode.A);
            _isHoldingD = Input.GetKey(KeyCode.D);

            // ── Double-tap run (rolling window, no release required) ──
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (Time.time - _lastADownTime <= _doubleTapWindow)
                    IsRunning = true;
                _lastADownTime = Time.time;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                if (Time.time - _lastDDownTime <= _doubleTapWindow)
                    IsRunning = true;
                _lastDDownTime = Time.time;
            }

            // Cancel run the moment both movement keys are released
            if (!_isHoldingA && !_isHoldingD)
                IsRunning = false;

            // ── Direction & facing (free in air per spec) ──
            _moveInput = 0f;

            if (_isHoldingA && !_isHoldingD)
            {
                _moveInput = -1f;
                FacingLeft = true;
            }
            else if (_isHoldingD && !_isHoldingA)
            {
                _moveInput = 1f;
                FacingLeft = false;
            }

            // ── Jump buffer ──
            if (Input.GetKeyDown(KeyCode.W))
                _jumpBufferCounter = _jumpBufferTime;
        }

        private Vector2 _surfaceVelocity;

        // ── Ground & Timers ───────────────────────────────────────────────────
        private void UpdateGroundAndTimers()
        {
            Vector2 checkPos = _groundCheck != null ? (Vector2)_groundCheck.position : (Vector2)transform.position;
            
            // ALWAYS use the exact bottom of the physics collider if available, 
            // bypassing any misplaced GroundCheck child transforms.
            var myCollider = GetComponent<BoxCollider2D>();
            if (myCollider != null)
            {
                checkPos = (Vector2)transform.position + myCollider.offset + new Vector2(0, -myCollider.size.y / 2f);
            }

            // Find the first collider that is NOT part of the player and NOT a trigger
            Collider2D validHit = null;
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, _groundCheckRadius, _groundLayer);
            foreach (var h in hits)
            {
                if (h.transform.root != transform.root && !h.isTrigger)
                {
                    validHit = h;
                    break;
                }
            }
            
            if (validHit != null)
            {
                var movingPlatform = validHit.GetComponent<YourGame.Gameplay.Environment.MovingPlatform>();
                if (movingPlatform != null)
                    _surfaceVelocity = movingPlatform.Velocity;
                else if (validHit.attachedRigidbody != null)
                    _surfaceVelocity = validHit.attachedRigidbody.linearVelocity;
                else
                    _surfaceVelocity = Vector2.zero;
            }
            else
            {
                _surfaceVelocity = Vector2.zero;
            }

            // If we are moving up faster than the surface we are standing on, we must be jumping or falling up.
            if (_rb.linearVelocity.y > _surfaceVelocity.y + 0.1f)
            {
                IsGrounded = false;
            }
            else
            {
                IsGrounded = validHit != null;
            }

            if (IsGrounded)
                _coyoteCounter = _coyoteTime;
            else
                _coyoteCounter -= Time.deltaTime;

            _jumpBufferCounter -= Time.deltaTime;
        }

        // ── Jump ──────────────────────────────────────────────────────────────
        private void HandleJump()
        {
            if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
            {
                _rb.linearVelocity       = new Vector2(_rb.linearVelocity.x, _jumpForce);
                _jumpBufferCounter = 0f;
                _coyoteCounter     = 0f;
                IsGrounded         = false; // Instantly prevent Animator from cancelling jump
                OnJumped?.Invoke();
            }

            // Variable height: releasing W early cuts the arc
            if (Input.GetKeyUp(KeyCode.W) && _rb.linearVelocity.y > 0f)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * 0.5f);
        }

        // ── Horizontal Movement ───────────────────────────────────────────────
        private void ApplyHorizontalMovement()
        {
            float topSpeed = IsRunning ? _runSpeed : _walkSpeed;
            float target   = _moveInput * topSpeed;
            float rate     = Mathf.Abs(target) > 0.01f ? _acceleration : _deceleration;

            // Isolate the player's true internal velocity by stripping out the surface velocity
            float currentSelfVelocityX = _rb.linearVelocity.x - _surfaceVelocity.x;

            // Accelerate/Decelerate ONLY the internal velocity
            float newSelfVelocityX = Mathf.MoveTowards(currentSelfVelocityX, target, rate * Time.fixedDeltaTime);


            // Re-apply the surface velocity for the final world velocity
            float finalVelocityX = newSelfVelocityX + _surfaceVelocity.x;   
            
            _rb.linearVelocity = new Vector2(finalVelocityX, _rb.linearVelocity.y);
        }

        // ── Fall Gravity ──────────────────────────────────────────────────────
        private void ApplyFallGravity()
        {
            if (_rb.linearVelocity.y < 0f)
            {
                _rb.linearVelocity += Vector2.up * Physics2D.gravity.y
                                           * (_fallGravityMultiplier - 1f)
                                           * Time.fixedDeltaTime;
            }
        }

        // ── Respawn System ────────────────────────────────────────────────────
        public void TeleportTo(Vector3 position)
        {
            _rb.position = position;
            _rb.linearVelocity = Vector2.zero;
            _jumpBufferCounter = 0f;
            _coyoteCounter = 0f;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (_groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }

        // ── Map Boundary Clamping ──────────────────────────────────────────────
        // Called from FixedUpdate so it runs in the physics step — using _rb.position
        // ensures the physics engine respects the clamp on the same frame.
        private void ClampToBounds()
        {
            if (LevelBoundsManager.Instance == null || !LevelBoundsManager.Instance.HasBounds) return;

            Bounds b = LevelBoundsManager.Instance.CurrentBounds;
            Vector2 pos = _rb.position;

            // 0.5 unit padding so the sprite doesn't hang half-way off the edge.
            const float padding = 0.5f;

            if (pos.x < b.min.x + padding)
            {
                pos.x = b.min.x + padding;
                if (_rb.linearVelocity.x < 0f)
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            }
            else if (pos.x > b.max.x - padding)
            {
                pos.x = b.max.x - padding;
                if (_rb.linearVelocity.x > 0f)
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            }

            _rb.position = pos;
        }
    }
}
