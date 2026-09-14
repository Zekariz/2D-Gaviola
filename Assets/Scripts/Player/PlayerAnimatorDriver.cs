using UnityEngine;

namespace Gaviola.Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        private PlayerController _controller;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        // Hashes
        private static readonly int SpeedHash = Animator.StringToHash("speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int VelocityYHash = Animator.StringToHash("velocityY");
        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int FacingLeftHash = Animator.StringToHash("facingLeft");
        private static readonly int JumpTriggerHash = Animator.StringToHash("jumpTrigger");

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _controller.OnJumpTriggered += HandleJump;
        }

        private void OnDisable()
        {
            _controller.OnJumpTriggered -= HandleJump;
        }

        private void Update()
        {
            _animator.SetFloat(SpeedHash, Mathf.Abs(_controller.Velocity.x));
            _animator.SetBool(IsGroundedHash, _controller.IsGrounded);
            _animator.SetFloat(VelocityYHash, _controller.Velocity.y);
            _animator.SetBool(IsRunningHash, _controller.IsRunning);
            _animator.SetBool(FacingLeftHash, _controller.FacingLeft);

            // Note on SpriteRenderer.flipX:
            // Since we are explicitly using the 'SpriteSheet2D-Backwards.png' to render left-facing animations 
            // via the Animator, using flipX here would double-flip the already mirrored sprites, making them face right! 
            // Therefore, flipX is commented out. The Animator alone handles the visual direction via the 'facingLeft' bool.
            
            // if (_controller.IsGrounded)
            // {
            //     _spriteRenderer.flipX = _controller.FacingLeft;
            // }
        }

        private void HandleJump()
        {
            _animator.SetTrigger(JumpTriggerHash);
        }
    }
}
