using UnityEngine;

namespace BossRaid.Player.States
{
    public class ExhaustedState : PlayerStatesBase
    {
        public ExhaustedState(PlayerController owner) : base(owner) { }

        public override bool BlocksStaminaRegen => false;

        public override void Enter()
        {
            Owner.StopMovement(false);
            Owner.LockRun();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            if (Owner.Stamina.CurrentStamina > 0)
                Animator.SetBool(PlayerController.IsExhaustedHash, false);
        }
    }
}
