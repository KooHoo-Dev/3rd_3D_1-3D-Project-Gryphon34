using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    // 대기 / 걷기 / 달리기 (Locomotion 블렌드 트리) 상태입니다.
    // 이동 입력을 넘겨주고, 좌클릭이 들어오면 공격을 시작하라고 애니메이터에 알립니다.
    public class LocomotionState : PlayerStatesBase
    {
        public LocomotionState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            Animator.SetBool(PlayerController.IsAttackHash, false);
            Animator.SetBool(PlayerController.IsChargingHash, false);
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            Owner.HandleMovement();

            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left))
            {
                Animator.SetBool(PlayerController.IsAttackHash, true);
            }
        }
    }
}
