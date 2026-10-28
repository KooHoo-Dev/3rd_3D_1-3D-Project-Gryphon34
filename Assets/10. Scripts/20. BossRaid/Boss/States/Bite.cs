namespace BossRaid.Boss.States
{
    public class Bite : MeleeAttack
    {
        public override StateName Name => StateName.BiteState;

        public override string AnimatorTrigger => "Bite";
    }
}
