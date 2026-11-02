using DG.Tweening;
using UnityEngine;

namespace BossRaid.Boss.States
{
    // 공중 패턴 공통: 이륙(TakeOff) → 제자리 비행 hoverTime → 자식의 공중 공격 attackCount번 → 착지(Land)
    // 착지 칸(Tag = animatorStateName)에 들어간 순간의 플레이어 자리로 landMoveTime 동안 미끄러져 내려옴
    // 비행 클립은 높이가 뼈에 들어 있어서 보스 오브젝트는 땅에 있고 몸만 뜸
    public abstract class FlyAttack : Attack
    {
        [SerializeField] private float hoverTime = 2f;        // Enter부터 공중 공격 트리거까지 (이륙 포함)
        [SerializeField] private int attackCount = 1;         // 공중 공격 횟수 (자식이 CountAttack으로 셈)
        [SerializeField] private float landMoveTime = 1f;     // 착지 칸에서 목표 자리까지 미끄러지는 시간
        [SerializeField] private float maxLandDistance = 15f; // 착지 칸에 들어간 순간 플레이어가 더 멀면 이 거리까지만
        [SerializeField] private Ease landEase = Ease.Linear; // 미끄러지는 빠르기 변화 (Linear = 늘 같은 속도, InQuad = 점점 빨라짐, OutQuad = 점점 느려짐)

        public override string AnimatorTrigger => "TakeOff";
        protected abstract string AttackTrigger { get; } // 제자리 비행 → 이 공격 칸

        private float currentTime;
        private bool isAttackStarted;
        private int currentCount;
        private bool isLanding;
        private float landTime;
        private Vector3 landStart;
        private Vector3 landTarget;

        public override void Enter()
        {
            base.Enter();
            currentTime = 0f;
            isAttackStarted = false;
            currentCount = 0;
            isLanding = false;
        }

        protected override void Update()
        {
            base.Update();

            currentTime += Time.deltaTime;
            if (isAttackStarted == false && currentTime >= hoverTime)
            {
                isAttackStarted = true;
                animator.SetTrigger(AttackTrigger);
            }

            if (isLanding == false && IsInLandingState()) StartLanding();
            if (isLanding) MoveToLandTarget();
        }

        // 공중 공격 한 번이 나갈 때 자식이 부름. 다 쓰면 착지
        protected void CountAttack()
        {
            currentCount++;
            if (currentCount >= attackCount) animator.SetTrigger("Land");
        }

        private bool IsInLandingState()
        {
            AnimatorStateInfo state = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            return state.IsTag(animatorStateName);
        }

        private void StartLanding()
        {
            isLanding = true;
            landTime = 0f;
            landStart = boss.transform.position;

            Vector3 toPlayer = boss.Player.transform.position - landStart;
            toPlayer.y = 0f;
            landTarget = landStart + Vector3.ClampMagnitude(toPlayer, maxLandDistance);
            SetRotating(true); // 내려오면서 플레이어 쪽으로 (착지 판정 HitboxOn에서 꺼짐)
        }

        private void MoveToLandTarget()
        {
            if (landTime >= landMoveTime) return;
            landTime += Time.deltaTime;
            float rate = landMoveTime > 0f ? Mathf.Clamp01(landTime / landMoveTime) : 1f;
            float easedRate = DOVirtual.EasedValue(0f, 1f, rate, landEase);
            boss.transform.position = Vector3.LerpUnclamped(landStart, landTarget, easedRate); // Unclamped: OutBack 같은 넘쳤다 돌아오는 곡선도 그대로
        }

        public override void Exit()
        {
            base.Exit();
            // 못 쓰고 남은 트리거가 나중에 다른 칸에서 튀어나오지 않게 (IdleState의 Exhausted와 같은 문제)
            animator.ResetTrigger("TakeOff");
            animator.ResetTrigger(AttackTrigger);
            animator.ResetTrigger("Land");
        }
    }
}
