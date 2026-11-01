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
            Owner.Sword.StartTrail();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
        }

        public override void Exit()
        {
            Owner.Sword.StopTrail();
        }

    }
}
