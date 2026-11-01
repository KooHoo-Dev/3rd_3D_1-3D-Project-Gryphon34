using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    public class ChargeState : PlayerStatesBase
    {
        public ChargeState(PlayerController owner) : base(owner) { }

        public float chargingTime = 0.0f;
        private int chargeLevel; // 지금까지 오른 단계. 넘는 순간을 잡으려고 기억

        public override void Enter()
        {
            Animator.SetBool(PlayerController.IsAttackHash, false);
            Owner.Stamina.TryUseStamina(Owner.AttackStamina);
            Owner.StopMovement();
            chargingTime = 0.0f;
            chargeLevel = 0;
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            bool isHolding = SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left);
            Animator.SetBool(PlayerController.IsChargingHash, isHolding);
            if (stateInfo.IsTag("Charge") && isHolding)
            {
                chargingTime += Time.deltaTime;

                int level = Owner.Sword.GetChargeLevel(chargingTime);
                if (level > chargeLevel)
                {
                    chargeLevel = level;
                    Owner.Sword.ShowChargeLevel(level);
                }
            }
        }

        public override void Exit()
        {
            Owner.Sword.StopChargeEffect();
            // 모은 시간으로 차지 단계를 정해서 칼에 넘겨준다 (공격 상태로 넘어가기 직전)
            Owner.Sword.ApplyCharge(chargingTime);
        }
    }
}