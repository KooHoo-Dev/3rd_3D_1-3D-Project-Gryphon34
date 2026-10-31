using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class ChaseState : BossStates
    {
        [SerializeField] private DecideState decideState;
        [SerializeField] private float rotateSpeed = 120f;
        public override StateName Name => StateName.ChaseState;

        public override string AnimatorTrigger => "Walk";

        // 예약: 다른 상태(불덩이)가 "다음은 이 패턴, 이 거리 안에서"를 맡겨 둠
        private bool hasReserve = false;
        private StateName reservedPattern;
        private float reservedRange;

        public void Reserve(StateName pattern, float range)
        {
            hasReserve = true;
            reservedPattern = pattern;
            reservedRange = range;
        }

        protected override void Update()
        {
            base.Update();
            boss.transform.rotation =
                Quaternion.RotateTowards(boss.transform.rotation,
                    boss.transform.FlatRotationTo(boss.Player.transform),
                    rotateSpeed * Time.deltaTime);

            // 예약이 있으면 Decide보다 먼저: 사거리 안에 들어오면 예약한 패턴 (20m에 브레스 줄이 있어서 순서가 중요)
            if (hasReserve)
            {
                if (boss.transform.FlatDistanceTo(boss.Player.transform) <= reservedRange)
                {
                    hasReserve = false;
                    boss.ChangeState(reservedPattern);
                }
                return;
            }

            if(decideState.CanAttack()) boss.ChangeState(StateName.DecideState);
        }

        // 이미 걷기 칸에서 들어오면 Walk 트리거가 안 쓰이고 남음 → 나중에 Idle 칸에서 튀어나와 걸어감
        public override void Exit()
        {
            animator.ResetTrigger("Walk");
            hasReserve = false;
        }

        
    }
}
