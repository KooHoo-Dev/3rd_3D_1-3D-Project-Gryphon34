using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class IdleState : BossStates
    {
        public override StateName Name => StateName.IdleState;
        [SerializeField] private float idleTime = 3.0f; // 공격 뒤가 아닐 때 기다리는 시간
        [SerializeField] private float discoverDistance = 100f;
        [SerializeField] private float getUpTime = 2.2f; // 지침: 일어나기(Death 클립 거꾸로) 길이. 쉬는 시간이 끝나기 이만큼 전에 일어나기 시작
        private float currentIdleTime = 0.0f;
        private float waitTime; // 이번에 기다릴 시간
        private bool isGettingUp;

        // 지쳤으면 지침 시간 + 쓰러짐, 아니면 바로 전이 공격이면 그 공격의 대기 시간, 아니면 idleTime
        public override void Enter()
        {
            isGettingUp = false;
            if (boss.IsExhausted)
            {
                waitTime = boss.ExhaustTime;
                animator.SetTrigger("Exhausted");
            }
            else waitTime = boss.PreviousState is Attack attack ? attack.RecoveryTime : idleTime;
        }

        protected override void Update()
        {
            base.Update();
            if (boss.IsDiscovered == false)
            {
                float distance = boss.transform.FlatDistanceTo(boss.Player.transform);
                if (distance <= discoverDistance) boss.SetDiscover();
                if (boss.IsDiscovered == false) return;
                boss.ChangeState(StateName.RoarState);
                return;
            }

            currentIdleTime += Time.deltaTime;

            // 지침: 끝나기 getUpTime 전에 일어나기 시작 → 다 일어난 순간 회복
            if (boss.IsExhausted && isGettingUp == false && currentIdleTime >= waitTime - getUpTime)
            {
                isGettingUp = true;
                animator.SetTrigger("GetUp");
            }

            if (boss.IsAIPaused) return; // 임시: P 키로 멈춤
            if (currentIdleTime < waitTime) return;

            if (boss.IsExhausted) boss.HealFatigue(); // 지침 시간을 다 쉼 → 회복
            boss.ChangeState(StateName.DecideState);
        }

        public override void Exit()
        {
            currentIdleTime = 0.0f;
            // 이미 그 칸이면 트리거가 안 쓰이고 남음 → 나중에 공격 중에 튀어나와 끊음 (Chase의 Walk와 같은 문제)
            animator.ResetTrigger("Exhausted");
            animator.ResetTrigger("GetUp");
        }

    }
}
