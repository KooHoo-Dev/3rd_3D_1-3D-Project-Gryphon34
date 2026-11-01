using UnityEngine;

namespace BossRaid.Player.States
{
    // 상쇄 베기 (애니메이터 Tag "Counter"). 제자리에 서서 벱니다
    public class CounterState : PlayerStatesBase
    {
        public CounterState(PlayerController owner) : base(owner) { }

        public override void Enter()
        {
            Owner.StopMovement();
            // 상쇄 상자의 WeaponCollider도 "이미 때린 보스"를 비움 (AttackState.Enter와 같음. 안 하면 다음 콤보 전까지 상쇄 베기가 안 들어감)
            Owner.Sword.ResetHash();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
        }
    }
}
