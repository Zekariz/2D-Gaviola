using UnityEngine;

namespace YourGame.Gameplay.Player
{
    public class IdleWaitRandomizer : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetInteger("idleIndex", Random.Range(0, 2));
        }
    }
}
