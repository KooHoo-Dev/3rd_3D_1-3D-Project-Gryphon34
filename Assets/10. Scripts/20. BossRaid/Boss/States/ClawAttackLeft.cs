namespace BossRaid.Boss.States
{
    public class ClawAttackLeft : MeleeAttack
    {
        public override StateName Name => StateName.ClawAttackState;

        public override string AnimatorTrigger => "ClawsAttackLeft";
    }
}
