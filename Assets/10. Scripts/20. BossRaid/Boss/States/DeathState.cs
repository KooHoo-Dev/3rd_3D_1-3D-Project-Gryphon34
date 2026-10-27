using UnityEngine;

namespace BossRaid.Boss.States
{
    public class DeathState : BossStates
    {
        public override StateName Name => StateName.DeathState;

        public override string AnimatorTrigger => "Death";
    }
}