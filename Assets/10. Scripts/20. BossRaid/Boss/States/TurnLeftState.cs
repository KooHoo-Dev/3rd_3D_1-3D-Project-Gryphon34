namespace BossRaid.Boss.States
{
    public class TurnLeftState : BossStates
    {
        public override StateName Name => StateName.TurnLeftState;

        public override string AnimatorTrigger => "TurnLeft";

        protected override void OnAnimationEnd()
        {
            boss.ChangeState(StateName.DecideState);
        }
    }
}
