using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class TurnClawState : Attack
    {
        public override StateName Name => StateName.TurnClawState;

        public override string AnimatorTrigger
        {
            get
            {
                float side = Vector3.Dot(boss.transform.right, boss.transform.FlatDirectionTo(boss.Player.transform));

                if (side > 0f)
                    return "TurnClawRight";
                else
                    return "TurnClawLeft";
            }
        }
    }
}
