using System;
using UnityEngine;

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

        // ── Jump Timers ───────────────────────────────────────────────────────
        private float _coyoteCounter;
        private float _jumpBufferCounter;

        // ── Public State (consumed by PlayerAnimatorDriver) ───────────────────
        public bool  IsGrounded    { get; private set; }
        public bool  IsRunning     { get; private set; }
        public bool  FacingLeft    { get; private set; }
        public float HorizontalSpeed => Mathf.Abs(_rb.linearVelocity.x);   // always >= 0
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
        }

        // ── Input ─────────────────────────────────────────────────────────────
        private void GatherInput()
        {
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

        // ── Ground & Timers ───────────────────────────────────────────────────
        private void UpdateGroundAndTimers()
        {
            IsGrounded = _groundCheck != null &&
                         Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

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

            _rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(_rb.linearVelocity.x, target, rate * Time.fixedDeltaTime),
                _rb.linearVelocity.y
            );
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

        // ── Gizmos ────────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (_groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }
    }
}
