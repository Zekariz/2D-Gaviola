using UnityEngine;

namespace Gaviola.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 80f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float fallGravityMultiplier = 2.5f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _sprite;

        private float _moveInput;
        private bool _isGrounded;
        private float _coyoteCounter;
        private float _jumpBufferCounter;

        // Animator parameter hashes (faster than string lookups)
        private static readonly int SpeedHash = Animator.StringToHash("speed");
        private static readonly int GroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int VelocityYHash = Animator.StringToHash("velocityY");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            GatherInput();
            UpdateTimers();
            HandleJump();
            UpdateAnimator();
            FlipSprite();
        }

        private void FixedUpdate()
        {
            ApplyHorizontalMovement();
            ApplyBetterGravity();
        }

        private void GatherInput()
        {
            _moveInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetButtonDown("Jump"))
                _jumpBufferCounter = jumpBufferTime;
        }

        private void UpdateTimers()
        {
            _isGrounded = Physics2D.OverlapCircle(
                groundCheck.position, groundCheckRadius, groundLayer);

            if (_isGrounded)
                _coyoteCounter = coyoteTime;
            else
                _coyoteCounter -= Time.deltaTime;

            _jumpBufferCounter -= Time.deltaTime;
        }

        private void HandleJump()
        {
            if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
                _jumpBufferCounter = 0f;
                _coyoteCounter = 0f;
            }

            // Variable jump height: release early = shorter jump
            if (Input.GetButtonUp("Jump") && _rb.velocity.y > 0f)
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * 0.5f);
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = _moveInput * moveSpeed;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

            _rb.velocity = new Vector2(
                Mathf.MoveTowards(_rb.velocity.x, targetSpeed, rate * Time.fixedDeltaTime),
                _rb.velocity.y);
        }

        private void ApplyBetterGravity()
        {
            if (_rb.velocity.y < 0f)
                _rb.velocity += Vector2.up * Physics2D.gravity.y
                                * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        private void UpdateAnimator()
        {
            _animator.SetFloat(SpeedHash, Mathf.Abs(_rb.velocity.x));
            _animator.SetBool(GroundedHash, _isGrounded);
            _animator.SetFloat(VelocityYHash, _rb.velocity.y);
        }

        private void FlipSprite()
        {
            if (Mathf.Abs(_moveInput) > 0.01f)
                _sprite.flipX = _moveInput < 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
