namespace BossRaid.Boss.States
{
    public class JumpBite : Attack
    {
        public override StateName Name => StateName.JumpBiteState;

        public override string AnimatorTrigger => "JumpBite";
    }
}
