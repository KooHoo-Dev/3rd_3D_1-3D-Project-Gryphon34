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
            if (stateInfo.normalizedTime < 0.8f && isAttack)
            {
                if((Animator.GetBool(PlayerController.IsAttackHash) == false) &&
                   Owner.Stamina.CanUseStamina(Owner.AttackStamina)) 
                    Animator.SetBool(PlayerController.IsAttackHash, true);
            }
        }
    }
}