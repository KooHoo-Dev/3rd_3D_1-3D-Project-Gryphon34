using BossRaid.Combat;
using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    // 차지 (Tag Start·Charge). 태클(Tag Tackle)도 원래 같은 상태였음 → 11/02 상쇄·태클을 빼서 아래 #region으로
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

            // 퍼펙트 가드 성공 뒤 첫 타: 차지를 보너스 단계 시간부터 셈 (짧게 떼도 그 단계, 더 누르면 위로)
            int bonusLevel = Owner.UseComboBonus();
            if (bonusLevel > 0)
            {
                chargingTime = Owner.Sword.GetChargeTime(bonusLevel);
                chargeLevel = bonusLevel;
                Owner.Sword.ShowChargeLevel(bonusLevel);
            }

            CameraZoom.Play(Owner.ChargeZoomOffset, Owner.ChargeZoomOutTime); // 차지 중 카메라 멀어짐
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
            CameraZoom.Play(0f, Owner.ChargeZoomReturnTime); // 떼면 원래 거리로 (맞아서 나갈 때도)
            // 모은 시간으로 차지 단계를 정해서 칼에 넘겨준다 (공격 상태로 넘어가기 직전)
            Owner.Sword.ApplyCharge(chargingTime);
        }

        #region 안 쓰는 것 (상쇄·태클, 11/02 뺌)
        // 다시 쓰려면: Enter 끝에 EnterTackle(), UpdateState 맨 앞에 UpdateCounterCancel(…)이 true면 return 하고 UpdateTackle(…),
        // 차지 시간 조건을 (Charge || Tackle)로, Exit 맨 앞에 ExitTackle(). PlayerController Awake에서 RegisterCounterTackleStates

        private bool tackleUsed; // 차지 한 번에 태클 한 번 (태클로 다음 타 Charge로 넘어가도 Enter가 안 불려서 그 타에선 못 씀)
        private int tackleNextCombo; // 지난 프레임에 태클 칸이었으면 다음 타 번호 (2·3), 아니면 0. Exit에서 씀

        private void EnterTackle()
        {
            Animator.ResetTrigger(PlayerController.TackleHash);
            tackleUsed = false;
            tackleNextCombo = 0;
            Owner.SetNextCombo(0); // 태클 뒤 다음 타로 시작했으면 다 씀
        }

        // 대기에서 좌클릭 먼저 → 0.1초 안에 우클릭: 쓴 공격 스태미나를 돌려주고 상쇄 (이 0.1초 뒤의 우클릭은 태클 자리)
        private bool UpdateCounterCancel(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.IsTag("Start") && Owner.CanCancelToCounter
                && SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Right))
            {
                Owner.Stamina.RestoreStamina(Owner.AttackStamina);
                Owner.TryStartCounter();
                // Start 칸은 Counter 트리거 화살표가 끊음 (애니메이터)
                return true;
            }
            return false;
        }

        private void UpdateTackle(AnimatorStateInfo stateInfo)
        {
            // 태클 칸 진행률 0 ~ TackleArmorEnd = 슈퍼아머 (넘어가기 시작한 순간부터. UpdateAnimState가 다음 칸 Tag를 봄)
            bool isTackle = stateInfo.IsTag("Tackle");
            Owner.IsTackleArmor = isTackle && stateInfo.normalizedTime <= Owner.TackleArmorEnd;
            // 1타 태클 → 2타, 2·3타 태클 → 3타 (3타가 마지막)
            tackleNextCombo = isTackle == false ? 0 : stateInfo.IsName("Combo1_Tackle") ? 2 : 3;

            // 태클: Windup·Hold 중 우클릭, 차지 한 번에 한 번. 끝나면 애니메이터가 다음 타의 Charge(또는 뗐으면 Attack)로 보냄
            if (isTackle == false && tackleUsed == false
                && SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Right)
                && Owner.Stamina.TryUseStamina(Owner.TackleStamina))
            {
                tackleUsed = true;
                Animator.SetTrigger(PlayerController.TackleHash);
                Debug.Log("[Player] 태클");
            }
        }

        private void ExitTackle()
        {
            Owner.IsTackleArmor = false;
            // 태클 칸에서 바로 나감 = 뗀 채 끝나서 Locomotion으로 (다음 타 Charge면 같은 상태라 Exit가 안 불림). 맞아서 나가면 HitState가 지움
            if (tackleNextCombo > 0)
                Owner.SetNextCombo(tackleNextCombo);
            Animator.ResetTrigger(PlayerController.TackleHash); // 못 쓰고 남은 태클 트리거가 다음 타에서 나가지 않게
        }

        #endregion
    }
}
