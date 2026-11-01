using DG.Tweening;
using UnityEngine;

namespace BossRaid.Combat
{
    public static class HitStop
    {
        public static void Play(Animator attacker, Animator target, float duration)
        {
            Stop(attacker, duration);
            Stop(target, duration);
        }

        private static void Stop(Animator animator, float duration)
        {
            if (animator == null) return;

            DOTween.Kill(animator);
            animator.speed = 0f;
            DOVirtual.DelayedCall(duration, () =>
            {
                if (animator != null) animator.speed = 1f;
            }).SetId(animator);
        }
    }
}
