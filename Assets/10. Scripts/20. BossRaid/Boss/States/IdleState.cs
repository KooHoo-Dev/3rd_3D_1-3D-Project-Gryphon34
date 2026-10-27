using UnityEngine;

namespace BossRaid.Boss.States
{
    public class IdleState : BossStates
    {
        public override StateName Name => StateName.IdleState;
        [SerializeField] private float idleTime = 5.0f;
        private float currentIdleTime = 0.0f;

        private void Update()
        {
            currentIdleTime += Time.deltaTime;
            if (boss.IsExhausted) return;
            if (currentIdleTime >= idleTime) boss.ChangeState(StateName.ClawAttackState);
        }

        public override void Exit()
        {
            currentIdleTime = 0.0f;
        }
        
    }
}