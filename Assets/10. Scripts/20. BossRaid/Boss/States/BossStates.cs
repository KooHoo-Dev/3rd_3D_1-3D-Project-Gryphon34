using UnityEngine;

namespace BossRaid.Boss.States
{
    public abstract class BossStates : MonoBehaviour
    {
        public enum StateName
        {
            IdleState,
            ClawAttackState,
            DeathState,
            DecideState,
            ChaseState,
        }

        [SerializeField] protected string animatorStateName;
        [SerializeField] protected float exitTime;

        protected Boss boss;
        protected Animator animator;

        public virtual string AnimatorTrigger => null;
        
        public abstract StateName Name { get; }

        public virtual void Initialize(Boss boss)
        {
            this.boss = boss;
            animator = boss.Animator;
        }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        protected virtual void Update()
        {
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName(animatorStateName) == false) return;

            if (currentState.normalizedTime > exitTime)
            {
                OnAnimationEnd();
            }
        }

        public virtual void ReceiveAnimEvent(string eventName, string parameter)
        {
            
        }

        protected virtual void OnAnimationEnd()
        {
            boss.ChangeState(StateName.IdleState);
        }
    }
}