using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    public class AttackEndState : PlayerStatesBase
    {
        public AttackEndState(PlayerController owner) : base(owner)
        {
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            bool isAttack = SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left);
            if (stateInfo.normalizedTime < 0.9f && isAttack)
            {
                Animator.SetBool(PlayerController.IsAttackHash, true);
            }
        }
    }
}