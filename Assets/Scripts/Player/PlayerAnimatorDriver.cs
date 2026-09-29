using UnityEngine;

namespace YourGame.Gameplay.Player
{
    /// <summary>
    /// Reads state from PlayerController every frame and pushes it into
    /// the Animator. Also listens for the OnJumped event to fire the
    /// jumpTrigger without a one-frame polling gap.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        private PlayerController _controller;
        private Animator         _animator;

        private static readonly int SpeedHash        = Animator.StringToHash("speed");
        private static readonly int IsGroundedHash   = Animator.StringToHash("isGrounded");
        private static readonly int VelocityYHash    = Animator.StringToHash("velocityY");
        private static readonly int IsRunningHash    = Animator.StringToHash("isRunning");
        private static readonly int FacingLeftHash   = Animator.StringToHash("facingLeft");
        private static readonly int JumpTriggerHash  = Animator.StringToHash("jumpTrigger");
        private static readonly int IsDeadHash       = Animator.StringToHash("isDead");

        private float _reportedSpeed;
        private float _lowSpeedTimer;
        private const float LOW_SPEED_TOLERANCE = 0.05f; // Tolerance for 1-frame physics snags

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _animator   = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            _controller.OnJumped += HandleJump;
        }

        private void OnDisable()
        {
            _controller.OnJumped -= HandleJump;
        }

        private void Update()
        {
            float actualSpeed = _controller.HorizontalSpeed;
            
            // Debounce the speed parameter dropping to 0 to prevent 1-frame animation flickers (Bug 1 & 3 fix)
            if (actualSpeed < 0.1f)
            {
                _lowSpeedTimer += Time.deltaTime;
                if (_lowSpeedTimer > LOW_SPEED_TOLERANCE)
                    _reportedSpeed = actualSpeed;
            }
            else
            {
                _lowSpeedTimer = 0f;
                _reportedSpeed = actualSpeed;
            }

            _animator.SetFloat(SpeedHash,       _reportedSpeed);
            _animator.SetBool (IsGroundedHash,  _controller.IsGrounded);
            _animator.SetFloat(VelocityYHash,   _controller.VelocityY);
            _animator.SetBool (IsRunningHash,   _controller.IsRunning);
            
            // ALWAYS tell the animator we are facing right, so it only plays the right-side animations (walk-r.png)
            _animator.SetBool(FacingLeftHash, false); 
            GetComponentInChildren<SpriteRenderer>().flipX = _controller.FacingLeft;
            
            _animator.SetBool (IsDeadHash,      _controller.IsDead);
        }

        private void HandleJump()
        {
            _animator.SetTrigger(JumpTriggerHash);
        }
    }
}
