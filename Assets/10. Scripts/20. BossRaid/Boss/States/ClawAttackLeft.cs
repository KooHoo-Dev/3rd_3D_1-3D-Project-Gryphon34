namespace BossRaid.Boss.States
{
    public class ClawAttackLeft : Attack
    {
        public override StateName Name => StateName.ClawAttackState;

        public override string AnimatorTrigger => "ClawsAttackLeft";
    }
}
