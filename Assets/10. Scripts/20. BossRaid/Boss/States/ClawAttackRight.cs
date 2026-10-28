namespace BossRaid.Boss.States
{
    public class ClawAttackRight : MeleeAttack
    {
        public override StateName Name => StateName.ClawAttackRightState;

        public override string AnimatorTrigger => "ClawsAttackRight";
    }
}
