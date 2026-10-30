using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    // 할퀴기: 철산고처럼 상태 하나에서 플레이어가 어느 옆에 있는지 보고 왼발/오른발 칸을 고릅니다
    // 두 칸(ClawsAttackLeft / ClawsAttackRight)에 같은 Tag를 붙이고, 인스펙터 Animator State Name에 그 Tag를 적습니다
    public class ClawAttack : Attack
    {
        public override StateName Name => StateName.ClawAttackState;

        // 원래 Decide 목록과 같은 짝: 보스 오른쪽(+45~90) → ClawsAttackLeft, 왼쪽(-90~-45) → ClawsAttackRight
        public override string AnimatorTrigger
        {
            get
            {
                float side = Vector3.Dot(boss.transform.right, boss.transform.FlatDirectionTo(boss.Player.transform));

                if (side > 0f)
                    return "ClawsAttackLeft";
                else
                    return "ClawsAttackRight";
            }
        }
    }
}
