namespace BossRaid.Boss.States
{
    public class ClawAttackRight : Attack
    {
        public override StateName Name => StateName.ClawAttackRightState;

        public override string AnimatorTrigger => "ClawsAttackRight";
    }
}
