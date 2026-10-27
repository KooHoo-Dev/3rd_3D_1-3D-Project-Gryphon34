using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Player.States
{
    // 대기 / 걷기 / 달리기 (Locomotion 블렌드 트리) 상태입니다.
    // 이동 입력을 넘겨주고, 좌클릭이 들어오면 공격을 시작하라고 애니메이터에 알립니다.
    public class LocomotionState : PlayerStatesBase
    {
        public LocomotionState(PlayerController owner) : base(owner) { }

        
        public override bool BlocksStaminaRegen => false;

        public override void Enter()
        {
            Animator.SetBool(PlayerController.IsAttackHash, false);
            Animator.SetBool(PlayerController.IsChargingHash, false);
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            if (Owner.Stamina.CurrentStamina <= 0)
            {
                Animator.SetBool(PlayerController.IsExhaustedHash, true);
                return;
            }

            Owner.HandleMovement();
            
            if (SimpleInput.GetKeyDown(Key.R))
            {
                if (Owner.Potion.CanDrink)
                    Animator.SetTrigger(PlayerController.DrinkHash);
            }

            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left))
            {
                if(Owner.Stamina.CanUseStamina(Owner.AttackStamina)) 
                    Animator.SetBool(PlayerController.IsAttackHash, true);
            }

            if (SimpleInput.GetKeyDown(Key.Space))
            {
                if(Owner.Stamina.TryUseStamina(Owner.DodgeStamina)) 
                    Animator.SetTrigger(PlayerController.DodgeHash);
            }
        }
    }
}
