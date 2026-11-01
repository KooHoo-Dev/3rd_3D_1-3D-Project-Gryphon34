using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
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
