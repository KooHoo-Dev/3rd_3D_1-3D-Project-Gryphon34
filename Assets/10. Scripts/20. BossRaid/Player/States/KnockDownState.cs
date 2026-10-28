using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Player.States
{
    public class KnockDownState : PlayerStatesBase
    {
        public KnockDownState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            Owner.StopMovement(true);
            Owner.Health.IsInvincible = true;
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            // 누워 있는 동안만 받습니다. 쓰러지는 중이나 일어나는 중에 걸면 트리거가 남아서 다음 넉다운 때 바로 일어납니다
            if (stateInfo.IsName("Down") == false) return;

            bool isMoving = SimpleInput.GetMoveAxisRaw() != Vector2.zero;
            bool isDodge = SimpleInput.GetKeyDown(Key.Space);
            if (isMoving || isDodge)
                Animator.SetTrigger(PlayerController.StandUpHash);
        }

        public override void Exit()
        {
            Owner.Health.IsInvincible = false;
        }
    }
}
