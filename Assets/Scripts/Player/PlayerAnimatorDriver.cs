using UnityEngine;

namespace YourGame.Gameplay.Player
{
    /// <summary>
    /// Reads state from PlayerController every frame and pushes it into
    /// the Animator. Also listens for the OnJumped event to fire the
    /// jumpTrigger without a one-frame polling gap.
    /// This component must live on the SAME GameObject as PlayerController
    /// and Animator.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        private PlayerController _controller;
        private Animator         _animator;

        // Pre-hash all parameter names once at load time.
        // If a hash doesn't match a parameter name in Player.controller,
        // Unity will silently ignore the Set* call — check the Animator
        // window if transitions never fire.
        private static readonly int SpeedHash        = Animator.StringToHash("speed");
        private static readonly int IsGroundedHash   = Animator.StringToHash("isGrounded");
        private static readonly int VelocityYHash    = Animator.StringToHash("velocityY");
        private static readonly int IsRunningHash    = Animator.StringToHash("isRunning");
        private static readonly int FacingLeftHash   = Animator.StringToHash("facingLeft");
        private static readonly int JumpTriggerHash  = Animator.StringToHash("jumpTrigger");

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _animator   = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            // Subscribe when enabled so the trigger fires on the exact frame the jump executes
            _controller.OnJumped += HandleJump;
        }

        private void OnDisable()
        {
            _controller.OnJumped -= HandleJump;
        }

        private void Update()
        {
            // Push all continuous parameters every frame.
            // HorizontalSpeed is always >= 0 (unsigned); the facingLeft bool
            // handles which directional clip plays — we never touch flipX.
            _animator.SetFloat(SpeedHash,       _controller.HorizontalSpeed);
            _animator.SetBool (IsGroundedHash,  _controller.IsGrounded);
            _animator.SetFloat(VelocityYHash,   _controller.VelocityY);
            _animator.SetBool (IsRunningHash,   _controller.IsRunning);
            _animator.SetBool (FacingLeftHash,  _controller.FacingLeft);
        }

        private void HandleJump()
        {
            // SetTrigger is consumed in the next Animator evaluation cycle,
            // which is guaranteed to be within 1 frame of the jump.
            _animator.SetTrigger(JumpTriggerHash);
        }
    }
}
