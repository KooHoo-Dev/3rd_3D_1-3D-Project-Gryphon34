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
            RoarState,
            ClawAttackRightState, // 사용 안 함 (ClawAttack 하나로 합침). 지우면 아래 값이 밀려서 남겨 둠
            ClawComboState,
            TurnLeftState,
            TurnRightState,
            BiteState,
            JumpBiteState,
            BreathState,
            FireBallState,
            HipCheckState,
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
            if (animator.IsInTransition(0)) return;

            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            // 왼쪽/오른쪽처럼 칸이 여러 개인 상태는 칸들에 같은 Tag를 붙이고 animatorStateName에 그 Tag를 적습니다
            if (currentState.IsName(animatorStateName) == false && currentState.IsTag(animatorStateName) == false) return;

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