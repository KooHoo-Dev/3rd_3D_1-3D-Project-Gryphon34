namespace BossRaid.Boss.States
{
    public class JumpBite : MeleeAttack
    {
        public override StateName Name => StateName.JumpBiteState;

        public override string AnimatorTrigger => "JumpBite";
    }
}
