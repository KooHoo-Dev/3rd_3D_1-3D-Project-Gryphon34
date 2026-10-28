using UnityEngine;

namespace BossRaid.Boss.States
{
    public class RoarState : BossStates
    {
        public override StateName Name => StateName.RoarState;

        public override string AnimatorTrigger => "Roar";

        protected override void OnAnimationEnd()
        {
            boss.ChangeState(StateName.DecideState);
        }
    }
}
