using UnityEngine;

namespace BossRaid.Boss.States
{
    public class ClawAttack : BossState
    {
        [SerializeField] private BossHitbox hitbox;
        
        public override StateName Name => StateName.ClawAttackState;

        public override string AnimatorTrigger => "ClawsAttackLeft";

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
            hitbox.gameObject.SetActive(false);
        }

        public override void Exit()
        {
            hitbox.gameObject.SetActive(false);
        }

        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            if(eventName == "HitboxOn") hitbox.gameObject.SetActive(true);
            if(eventName == "HitboxOff") hitbox.gameObject.SetActive(false);
        }
    }
}