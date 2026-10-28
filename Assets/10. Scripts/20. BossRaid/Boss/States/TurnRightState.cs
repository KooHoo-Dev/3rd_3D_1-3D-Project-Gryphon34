namespace BossRaid.Boss.States
{
    public class TurnRightState : BossStates
    {
        public override StateName Name => StateName.TurnRightState;

        public override string AnimatorTrigger => "TurnRight";

        protected override void OnAnimationEnd()
        {
            boss.ChangeState(StateName.DecideState);
        }
    }
}
