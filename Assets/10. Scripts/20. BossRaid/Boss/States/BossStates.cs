using Study.Utilities;
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
            TurnClawState,
            AirFireBallState,
            AirSpreadFireState,
        }

        [SerializeField] protected string animatorStateName;
        [SerializeField] protected float exitTime;

        protected Boss boss;
        protected Animator animator;
        private bool wasInMyAnim; // 내 칸(animatorStateName)에 한 번이라도 들어갔음 → 빠져나가면 끝

        public virtual string AnimatorTrigger => null;
        
        public abstract StateName Name { get; }

        private bool HasEnter = false;

        public virtual void Initialize(Boss boss)
        {
            this.boss = boss;
            animator = boss.Animator;
        }

        // Enter는 자식이 base 없이 덮어써서 여기서 초기화 (ChangeState가 Enter 전에 SetActive(true))
        protected virtual void OnEnable()
        {
            wasInMyAnim = false;
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
            bool isMyTag = string.IsNullOrEmpty(animatorStateName) == false && currentState.IsTag(animatorStateName);
            if (currentState.IsName(animatorStateName) == false && isMyTag == false)
            {
                // exitTime ~ 나가는 화살표 사이를 프레임이 건너뛰면 다음 칸으로 가 버려서 위 조건을 못 봄 → 여기서 끝냄
                if (wasInMyAnim) OnAnimationEnd();
                return;
            }
            wasInMyAnim = true;

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

        // 플레이어 쪽으로 돎. 남은 각도 0도 = minSpeed, 180도 = maxSpeed, 그 사이는 비례 (초당 각도)
        // maxSpeed가 minSpeed보다 작으면(기본 0) minSpeed로 고정 = 예전처럼 늘 같은 속도
        protected void RotateToPlayer(float minSpeed, float maxSpeed)
        {
            Quaternion targetRotation = boss.transform.FlatRotationTo(boss.Player.transform);
            float angle = Quaternion.Angle(boss.transform.rotation, targetRotation);
            float speed = Mathf.Lerp(minSpeed, Mathf.Max(minSpeed, maxSpeed), angle / 180f);
            boss.transform.rotation = Quaternion.RotateTowards(boss.transform.rotation, targetRotation, speed * Time.deltaTime);
        }
    }
}