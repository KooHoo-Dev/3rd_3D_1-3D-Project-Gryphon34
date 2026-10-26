using UnityEngine;

namespace BossRaid.Boss.States
{
    public class DeathState : BossState
    {
        public override StateName Name => StateName.DeathState;

        public override string AnimatorTrigger => "Death";
    }
}