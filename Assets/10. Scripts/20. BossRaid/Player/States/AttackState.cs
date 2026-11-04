using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    public class AttackState : PlayerStatesBase
    {
        public AttackState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            Owner.Sword.ResetHash();
            Owner.Sword.PlayAttackSound();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            bool isAttack = SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left);
            if (isAttack)
            {
                if((Animator.GetBool(PlayerController.IsAttackHash) == false) &&
                   Owner.Stamina.CanUseStamina(Owner.AttackStamina))
                    Animator.SetBool(PlayerController.IsAttackHash, true);
            }
        }

        public override void Exit()
        {
        }

    }
}
