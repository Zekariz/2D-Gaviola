using UnityEngine;

namespace YourGame.Gameplay.Player
{
    /// <summary>
    /// StateMachineBehaviour attached to the four idle states:
    ///   IdleYawn_Left, IdleYawn_Right, IdleGuitar_Left, IdleGuitar_Right.
    ///
    /// On every state entry it performs a 50/50 roll and writes the result
    /// into the Animator's "idleIndex" integer parameter.
    ///
    ///   idleIndex == 0  ->  next idle cycle will be Yawn
    ///   idleIndex == 1  ->  next idle cycle will be Guitar
    ///
    /// The idle-to-idle transitions use hasExitTime = true so the switch
    /// only happens after the current clip finishes — this prevents a pop
    /// mid-animation.
    /// </summary>
    public class IdleRandomizer : StateMachineBehaviour
    {
        // Cached hash avoids per-frame string lookup overhead.
        private static readonly int IdleIndexHash = Animator.StringToHash("idleIndex");

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            // Roll a new target idle variant for when this clip finishes.
            // 0 = Yawn, 1 = Guitar.
            int next = Random.Range(0, 2);
            animator.SetInteger(IdleIndexHash, next);
        }
    }
}