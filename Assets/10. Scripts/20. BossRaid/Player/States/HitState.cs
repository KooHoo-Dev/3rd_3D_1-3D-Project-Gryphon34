using UnityEngine;

namespace BossRaid.Player.States
{
    public class HitState : PlayerStatesBase
    {
        public HitState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            Owner.StopMovement(true);
            Owner.ClearComboBonus();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
        }
    }
}
