namespace BossRaid.Boss.States
{
    public class ClawCombo : Attack
    {
        public override StateName Name => StateName.ClawComboState;

        public override string AnimatorTrigger => "ClawsAttack2HitComboForward";
    }
}
