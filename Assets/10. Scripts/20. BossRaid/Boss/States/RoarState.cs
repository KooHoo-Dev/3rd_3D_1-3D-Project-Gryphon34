using UnityEngine;

namespace BossRaid.Boss.States
{
    public class RoarState : BossStates
    {
        [SerializeField] private AudioSource roarSource;
        [SerializeField] private AudioClip roarClip;

        public override StateName Name => StateName.RoarState;

        public override string AnimatorTrigger => "Roar";

        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            if (eventName == "Roar") PlayRoarSound();
        }

        protected override void OnAnimationEnd()
        {
            boss.ChangeState(StateName.DecideState);
        }

        private void PlayRoarSound()
        {
            if (roarSource == null || roarClip == null) return;
            roarSource.PlayOneShot(roarClip);
        }
    }
}
