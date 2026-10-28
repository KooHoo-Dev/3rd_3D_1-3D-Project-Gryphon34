namespace BossRaid.Boss.States
{
    public class ClawCombo : MeleeAttack
    {
        public override StateName Name => StateName.ClawComboState;

        public override string AnimatorTrigger => "ClawsAttack2HitComboForward";
    }
}
