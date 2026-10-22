using UnityEngine;

namespace BossRaid.Player.States
{
    // 실제로 베는 구간(Attack Tag) 상태입니다.
    // 이 상태에 있는 동안만 무기 판정을 켜고, 맞았다는 이벤트를 여기서 처리합니다.
    // Enter / Exit은 전환이 "시작되는" 순간에 불리므로, 판정은 Attack 클립으로 넘어가기 시작할 때 켜지고
    // End 클립으로 넘어가기 시작할 때 꺼집니다.
    public class AttackState : PlayerStatesBase
    {
        public AttackState(PlayerController owner) : base(owner) { }
        

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
        }
        
    }
}
