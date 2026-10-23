using BossRaid.Weapon;
using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    public class ChargeState : PlayerStatesBase
    {
        public ChargeState(PlayerController owner) : base(owner) { }

        public float chargingingTime = 0.0f;
        
        public override void Enter()
        {
            Animator.SetBool(PlayerController.IsAttackHash, false);
            Owner.StopMovement();
            chargingingTime = 0.0f;
        }
        
        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            bool isHolding = SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left);
            Animator.SetBool(PlayerController.IsChargingHash, isHolding);
            if (stateInfo.IsTag("Charge") && isHolding)
            {
                chargingingTime += Time.deltaTime;
            }
        }

        public override void Exit()
        {
        }
    }
}