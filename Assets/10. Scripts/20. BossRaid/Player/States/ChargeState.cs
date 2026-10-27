using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    public class ChargeState : PlayerStatesBase
    {
        public ChargeState(PlayerController owner) : base(owner) { }

        public float chargingTime = 0.0f;
        
        public override void Enter()
        {
            Animator.SetBool(PlayerController.IsAttackHash, false);
            Owner.StopMovement();
            chargingTime = 0.0f;
        }
        
        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            bool isHolding = SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left);
            Animator.SetBool(PlayerController.IsChargingHash, isHolding);
            if (stateInfo.IsTag("Charge") && isHolding)
            {
                chargingTime += Time.deltaTime;
            }
        }

        public override void Exit()
        {
            // 모은 시간으로 차지 단계를 정해서 칼에 넘겨준다 (공격 상태로 넘어가기 직전)
            Owner.Sword.ApplyCharge(chargingTime);
        }
    }
}