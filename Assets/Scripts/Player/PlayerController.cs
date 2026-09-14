using System;
using UnityEngine;

namespace Gaviola.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 80f;
        [SerializeField] private float doubleTapWindow = 0.25f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float fallGravityMultiplier = 2.5f;
        
        [Header("Grounding")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        // References
        private Rigidbody2D _rb;

        // Input state
        private float _moveInput;
        private bool _isJumpPressed;
        private bool _isJumpHeld;

        // Double tap state
        private float _lastADownTime = -1f;
        private float _lastDDownTime = -1f;
        private bool _isHoldingA;
        private bool _isHoldingD;

        // Logic state
        private float _coyoteCounter;
        private float _jumpBufferCounter;

        // Public properties for Animator Driver
        public bool IsGrounded { get; private set; }
        public bool IsRunning { get; private set; }
        public bool FacingLeft { get; private set; }
        public Vector2 Velocity => _rb.linearVelocity;
        public event Action OnJumpTriggered;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            FacingLeft = false; // Default facing right
        }

        private void Update()
        {
            GatherInput();
            UpdateTimers();
            HandleJump();
        }

        private void FixedUpdate()
        {
            ApplyHorizontalMovement();
            ApplyBetterGravity();
        }

        private void GatherInput()
        {
            // Reset input
            _moveInput = 0f;
            _isHoldingA = Input.GetKey(KeyCode.A);
            _isHoldingD = Input.GetKey(KeyCode.D);

            // Double tap logic for Run
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (Time.time - _lastADownTime <= doubleTapWindow)
                    IsRunning = true;
                _lastADownTime = Time.time;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (Time.time - _lastDDownTime <= doubleTapWindow)
                    IsRunning = true;
                _lastDDownTime = Time.time;
            }

            // Cancel run if keys are released
            if (!_isHoldingA && !_isHoldingD)
            {
                IsRunning = false;
            }

            // Determine move direction and facing
            if (_isHoldingA && !_isHoldingD)
            {
                _moveInput = -1f;
                if (IsGrounded) FacingLeft = true;
            }
            else if (_isHoldingD && !_isHoldingA)
            {
                _moveInput = 1f;
                if (IsGrounded) FacingLeft = false;
            }

            // Jump inputs (W instead of Space)
            if (Input.GetKeyDown(KeyCode.W))
            {
                _jumpBufferCounter = jumpBufferTime;
            }
            _isJumpHeld = Input.GetKey(KeyCode.W);
        }

        private void UpdateTimers()
        {
            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (IsGrounded)
                _coyoteCounter = coyoteTime;
            else
                _coyoteCounter -= Time.deltaTime;

            _jumpBufferCounter -= Time.deltaTime;
        }

        private void HandleJump()
        {
            // Execute jump
            if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                _jumpBufferCounter = 0f;
                _coyoteCounter = 0f;
                OnJumpTriggered?.Invoke();
            }

            // Variable jump height: release early = shorter jump
            if (Input.GetKeyUp(KeyCode.W) && _rb.linearVelocity.y > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * 0.5f);
            }
        }

        private void ApplyHorizontalMovement()
        {
            float currentMaxSpeed = IsRunning ? runSpeed : walkSpeed;
            float targetSpeed = _moveInput * currentMaxSpeed;
            
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

            _rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime),
                _rb.linearVelocity.y
            );
        }

        private void ApplyBetterGravity()
        {
            if (_rb.linearVelocity.y < 0f)
            {
                _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
