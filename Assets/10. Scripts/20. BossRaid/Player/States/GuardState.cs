using BossRaid.Combat;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Player.States
{
    // 가드 (애니메이터 Tag "Guard": 가드 자세 Guard + 막았을 때 GuardHit).
    // 제자리에서 이동 입력 방향으로 돌기만 하고, 막았으면 밀립니다. 막는 판정은 PlayerController.TryGuard (PlayerHealth가 부름)
    public class GuardState : PlayerStatesBase
    {
        public GuardState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            Owner.StopMovement();
            CameraZoom.Play(Owner.GuardZoomOffset, Owner.GuardZoomInTime); // 가드 중 카메라 가까워짐
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            Owner.UpdateGuardPush();
            Owner.Movement.RotateTowardsInput(SimpleInput.GetMoveAxisRaw(), Owner.GuardTurnSpeed);

            // Ctrl을 떼거나 스태미나가 0이면 가드를 풂 (0이면 Locomotion에서 지침으로)
            bool isHolding = SimpleInput.GetKey(Key.LeftCtrl) && Owner.Stamina.CurrentStamina > 0;
            Animator.SetBool(PlayerController.IsGuardingHash, isHolding);

            // 막는 동작(GuardHit) 중에는 안 받음. 걸어 두면 트리거가 남아서 끝난 뒤 저절로 나감
            if (stateInfo.IsName("Guard") == false) return;

            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left) && Owner.Stamina.CanUseStamina(Owner.AttackStamina))
                Animator.SetBool(PlayerController.IsAttackHash, true);

            if (SimpleInput.GetKeyDown(Key.Space) && Owner.Stamina.TryUseStamina(Owner.DodgeStamina))
                Animator.SetTrigger(PlayerController.DodgeHash);
        }

        public override void Exit()
        {
            Owner.StopGuardPush();
            CameraZoom.Play(0f, Owner.GuardZoomReturnTime); // 풀면 원래 거리로 (차지로 넘어가면 차지 Enter가 다시 덮음)
            Animator.SetBool(PlayerController.IsGuardingHash, false); // 아직 누르고 있으면 Locomotion이 다시 켬
        }
    }
}
