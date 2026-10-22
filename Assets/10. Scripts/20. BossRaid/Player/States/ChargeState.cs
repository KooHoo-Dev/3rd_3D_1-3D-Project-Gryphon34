using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    public class ChargeState : PlayerStatesBase
    {
        public ChargeState(PlayerController owner) : base(owner) { }

        public float holdingTime = 0.0f;
        
        public override void Enter()
        {
            Animator.SetBool(PlayerController.IsAttackHash, false);
            Owner.StopMovement();
            holdingTime = 0.0f;
        }
        
        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            bool isHolding = SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left);
            Animator.SetBool(PlayerController.IsChargingHash, isHolding);
            if (stateInfo.IsTag("Charge") && isHolding)
            {
                holdingTime += Time.deltaTime;
            }
        }
    }
}