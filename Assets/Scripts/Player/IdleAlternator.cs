using UnityEngine;

namespace YourGame.Gameplay.Player
{
    /// <summary>
    /// Attached to Guitar_Hold_* and Yawn_Hold_* states.
    /// On exit, flips the nextIsYawn bool parameter so the idle cycle
    /// alternates: Guitar -> Yawn -> Guitar -> Yawn -> ...
    ///
    /// Guitar_Hold exits  -> sets nextIsYawn = true  (Yawn is next)
    /// Yawn_Hold   exits  -> sets nextIsYawn = false (Guitar is next)
    ///
    /// The value persists across player movement stops.
    /// </summary>
    public class IdleAlternator : StateMachineBehaviour
    {
        [Tooltip("True when this behaviour is attached to a Guitar_Hold state (sets nextIsYawn=true on exit). " +
                 "False when attached to a Yawn_Hold state (sets nextIsYawn=false on exit).")]
        public bool IsGuitarHold = true;

        private static readonly int NextIsYawnHash = Animator.StringToHash("nextIsYawn");

        public override void OnStateExit(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetBool(NextIsYawnHash, IsGuitarHold);
        }
    }
}
