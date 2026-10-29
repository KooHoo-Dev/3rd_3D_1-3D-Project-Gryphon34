namespace BossRaid.Boss.States
{
    public class Bite : Attack
    {
        public override StateName Name => StateName.BiteState;

        public override string AnimatorTrigger => "Bite";
    }
}
