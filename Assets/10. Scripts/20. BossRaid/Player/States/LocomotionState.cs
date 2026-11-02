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

            // 가드: 누르고 있는 동안 IsGuarding. 누른 순간부터 퍼펙트 가드 시간을 셈 (떼고 다시 눌러야 다시 셈)
            if (SimpleInput.GetKeyDown(Key.LeftCtrl))
                Owner.MarkGuardPress();
            Animator.SetBool(PlayerController.IsGuardingHash, SimpleInput.GetKey(Key.LeftCtrl));

            if (SimpleInput.GetKeyDown(Key.R))
            {
                // 고른 칸을 쓸 수 있을 때만 (0 = 회복약, 1 = 인간성). 마시는 동작은 같음
                bool canUse = Owner.Potion.CurrentIndex == 0 ? Owner.Potion.CanDrink : Owner.Potion.CanUseHumanity;
                if (canUse)
                    Animator.SetTrigger(PlayerController.DrinkHash);
            }

            // 상쇄·태클은 뺌 (11/02). 다시 쓰려면 이 블록 대신 아래 #region의 UpdateAttackInputWithCounter()
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left)
                && Owner.Stamina.CanUseStamina(Owner.AttackStamina))
            {
                Animator.SetBool(PlayerController.IsAttackHash, true);
            }

            if (SimpleInput.GetKeyDown(Key.Space))
            {
                if(Owner.Stamina.TryUseStamina(Owner.DodgeStamina))
                    Animator.SetTrigger(PlayerController.DodgeHash);
            }
        }

        #region 안 쓰는 것 (상쇄·태클, 11/02 뺌)

        private float rightClickTime = float.NegativeInfinity; // 대기에서 우클릭을 누른 시각 (상쇄 입력)

        // 원래 좌클릭 처리: 우클릭 먼저 → 0.1초 안 좌클릭 = 상쇄, 태클 뒤 시간 안이면 다음 타 Start
        private void UpdateAttackInputWithCounter()
        {
            // 우클릭을 좌클릭보다 먼저 봄 → 같은 프레임에 둘 다 눌러도 상쇄
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Right))
                rightClickTime = Time.time;

            // 태클 뒤 시간 안이면 2·3, 아니면 0 → 애니메이터가 Combo 값으로 몇 타 Start로 갈지 고름
            Animator.SetInteger(PlayerController.ComboHash, Owner.NextCombo);

            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left))
            {
                if (Time.time - rightClickTime <= Owner.CounterInputTime)
                {
                    // 우클릭 먼저 → 0.1초 안에 좌클릭: 공격 대신 상쇄
                    rightClickTime = float.NegativeInfinity;
                    Owner.TryStartCounter();
                }
                else if(Owner.Stamina.CanUseStamina(Owner.AttackStamina))
                {
                    Animator.SetBool(PlayerController.IsAttackHash, true);
                    // 좌클릭 먼저 → 0.1초 안에 우클릭이 오면 ChargeState가 상쇄로 바꿈. 상쇄 화살표는 1타 Start에만 있어서 태클 뒤 다음 타는 빼고
                    if (Owner.NextCombo == 0)
                        Owner.MarkIdleAttack();
                }
            }
        }

        #endregion
    }
}
