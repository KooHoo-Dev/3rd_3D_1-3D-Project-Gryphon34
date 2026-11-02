using Study.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace BossRaid.Boss.States
{
    public class ChaseState : BossStates
    {
        [SerializeField] private DecideState decideState;
        [SerializeField, FormerlySerializedAs("rotateSpeed")]
        private float minRotateSpeed = 120f; // 거의 마주볼 때 회전 속도 (예전 rotateSpeed 값 그대로)
        [SerializeField] private float maxRotateSpeed; // 등 뒤(180도)일 때 회전 속도. minRotateSpeed보다 작으면(0) 늘 min 속도
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
            RotateToPlayer(minRotateSpeed, maxRotateSpeed);

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
