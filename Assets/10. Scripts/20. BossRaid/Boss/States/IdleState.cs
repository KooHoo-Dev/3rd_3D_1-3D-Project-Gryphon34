using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class IdleState : BossStates
    {
        public override StateName Name => StateName.IdleState;
        [SerializeField] private float idleTime = 3.0f;
        [SerializeField] private float discoverDistance = 100f;
        private float currentIdleTime = 0.0f;

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
            if (boss.IsExhausted) return;
            if (currentIdleTime >= idleTime) boss.ChangeState(StateName.DecideState);
        }

        public override void Exit()
        {
            currentIdleTime = 0.0f;
        }
        
    }
}