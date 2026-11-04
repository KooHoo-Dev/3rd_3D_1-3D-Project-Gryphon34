using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Player.States
{
    public class AttackEndState : PlayerStatesBase
    {
        public AttackEndState(PlayerController owner) : base(owner)
        {
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            bool isAttack = SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left);
            if (stateInfo.normalizedTime < 0.9f && isAttack)
            {
                if((Animator.GetBool(PlayerController.IsAttackHash) == false) &&
                   Owner.Stamina.CanUseStamina(Owner.AttackStamina))
                    Animator.SetBool(PlayerController.IsAttackHash, true);
            }

            if (stateInfo.normalizedTime >= Owner.AttackCancelTime) UpdateCancel();
        }

        // 후딜 캔슬: _End → Dodge_Front(Dodge 트리거) / Guard(IsGuarding) 화살표. 입력은 LocomotionState와 같음
        private void UpdateCancel()
        {
            if (SimpleInput.GetKeyDown(Key.LeftCtrl))
                Owner.MarkGuardPress();
            bool isGuarding = SimpleInput.GetKey(Key.LeftCtrl);
            Animator.SetBool(PlayerController.IsGuardingHash, isGuarding);

            bool isDodge = SimpleInput.GetKeyDown(Key.Space) && Owner.Stamina.TryUseStamina(Owner.DodgeStamina);
            if (isDodge)
                Animator.SetTrigger(PlayerController.DodgeHash);

            // 끊으면 눌러 둔 다음 타는 버림 (다음 타 화살표와 같이 나가지 않게)
            if (isGuarding || isDodge)
                Animator.SetBool(PlayerController.IsAttackHash, false);
        }
    }
}