using System;
using UnityEngine;

namespace YourGame.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // ── Movement ─────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 6f;
        [SerializeField] private float _runSpeed = 10f;
        [SerializeField] private float _acceleration = 60f;
        [SerializeField] private float _deceleration = 80f;
        [SerializeField] private float _doubleTapWindow = 0.25f;   // seconds within which a second tap counts as double-tap

        // ── Jump ─────────────────────────────────────────────────
        [Header("Jump")]
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private float _coyoteTime = 0.1f;         // seconds player can still jump after walking off a ledge
        [SerializeField] private float _jumpBufferTime = 0.1f;     // seconds a jump input is remembered before landing
        [SerializeField] private float _fallGravityMultiplier = 2.5f;

        // ── Ground Detection ──────────────────────────────────────
        [Header("Grounding")]
        [SerializeField] private Transform _groundCheck;           // small empty child transform positioned at feet
        [SerializeField] private float _groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask _groundLayer;

        // ── Internal References ───────────────────────────────────
        private Rigidbody2D _rb;

        // ── Raw Input State (polled in Update only) ───────────────
        private float _moveInput;          // -1, 0, or +1 this frame
        private bool _isHoldingA;
        private bool _isHoldingD;
        private bool _jumpHeld;            // W held this frame

        // ── Double-Tap Run Detection ──────────────────────────────
        private float _lastADownTime = -999f;
        private float _lastDDownTime = -999f;

        // ── Jump Timers ───────────────────────────────────────────
        private float _coyoteCounter;
        private float _jumpBufferCounter;

        // ── Public Read-Only State (consumed by AnimatorDriver) ───
        public bool IsGrounded    { get; private set; }
        public bool IsRunning     { get; private set; }
        public bool FacingLeft    { get; private set; }
        public float HorizontalSpeed => Mathf.Abs(_rb.velocity.x);  // unsigned, for Animator "speed" parameter
        public float VelocityY       => _rb.velocity.y;

        /// <summary>Fires on the exact frame a jump is executed (after coyote/buffer checks pass).</summary>
        public event Action OnJumped;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            FacingLeft = false;
        }

        // All input polling happens here — never in FixedUpdate, which
        // runs at a fixed rate and will silently drop GetKeyDown events.
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
        }

        // ── Input ─────────────────────────────────────────────────
        private void GatherInput()
        {
            _isHoldingA = Input.GetKey(KeyCode.A);
            _isHoldingD = Input.GetKey(KeyCode.D);
            _jumpHeld   = Input.GetKey(KeyCode.W);

            // ── Double-tap detection (rolling window, no release required) ──
            // Per spec: if the player is already walking and taps the same direction
            // key again within the window, switch to Run immediately.
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

            // ── Move direction & facing ──
            // Facing updates freely in air AND on ground (Celeste/Hollow Knight style per spec).
            _moveInput = 0f;

            if (_isHoldingA && !_isHoldingD)
            {
                _moveInput  = -1f;
                FacingLeft  = true;
            }
            else if (_isHoldingD && !_isHoldingA)
            {
                _moveInput  = 1f;
                FacingLeft  = false;
            }

            // ── Jump buffer ──
            if (Input.GetKeyDown(KeyCode.W))
                _jumpBufferCounter = _jumpBufferTime;
        }

        // ── Timers ────────────────────────────────────────────────
        private void UpdateGroundAndTimers()
        {
            // OverlapCircle returns null when the layer mask has no matching colliders,
            // so make sure _groundLayer is set in the Inspector.
            IsGrounded = _groundCheck != null &&
                         Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

            if (IsGrounded)
                _coyoteCounter = _coyoteTime;   // reset coyote window whenever grounded
            else
                _coyoteCounter -= Time.deltaTime;

            _jumpBufferCounter -= Time.deltaTime;
        }

        // ── Jump ─────────────────────────────────────────────────
        private void HandleJump()
        {
            // Execute jump only when both the buffer (recent W press) and coyote (recent ground) are valid
            if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce);
                _jumpBufferCounter = 0f;
                _coyoteCounter     = 0f;        // consume coyote so the player can't double-jump
                OnJumped?.Invoke();
            }

            // Variable height: cutting vertical velocity on W-release creates short hop
            if (Input.GetKeyUp(KeyCode.W) && _rb.velocity.y > 0f)
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * 0.5f);
        }

        // ── Horizontal Movement ───────────────────────────────────
        private void ApplyHorizontalMovement()
        {
            float topSpeed   = IsRunning ? _runSpeed : _walkSpeed;
            float target     = _moveInput * topSpeed;
            float rate       = Mathf.Abs(target) > 0.01f ? _acceleration : _deceleration;

            _rb.velocity = new Vector2(
                Mathf.MoveTowards(_rb.velocity.x, target, rate * Time.fixedDeltaTime),
                _rb.velocity.y
            );
        }

        // ── Fall Gravity ──────────────────────────────────────────
        private void ApplyFallGravity()
        {
            // Apply extra downward force only while falling; this makes the arc feel snappier
            if (_rb.velocity.y < 0f)
            {
                _rb.velocity += Vector2.up * Physics2D.gravity.y
                                           * (_fallGravityMultiplier - 1f)
                                           * Time.fixedDeltaTime;
            }
        }

        // ── Editor Helper ─────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (_groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }
    }
}
