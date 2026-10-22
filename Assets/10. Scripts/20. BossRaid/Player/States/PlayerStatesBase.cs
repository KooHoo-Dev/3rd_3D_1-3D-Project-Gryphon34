using UnityEngine;

namespace BossRaid.Player.States
{
    // 플레이어 상태 클래스들의 공통 부모입니다 (상태 패턴).
    // - 전이(Transition)를 결정하지 않습니다. 전이는 애니메이터가 합니다.
    // - PlayerController가 매 프레임 애니메이터의 상태 Tag를 읽고,
    //   그에 맞는 상태의 Enter / UpdateState / Exit 을 호출해 줍니다.
    // - MonoBehaviour가 아닌 순수 C# 클래스라서 new로 만들어 PlayerController가 들고 있습니다.
    public abstract class PlayerStatesBase
    {
        protected PlayerController Owner { get; }
        protected Animator Animator => Owner.Animator;

        protected PlayerStatesBase(PlayerController owner)
        {
            Owner = owner;
        }

        // 이 상태로 "진입한 순간" 한 번 호출됩니다.
        public virtual void Enter() { }
        // 이 상태에 머무는 동안 매 프레임 호출됩니다. (자식이 반드시 구현)
        public abstract void UpdateState(AnimatorStateInfo stateInfo);
        // 이 상태에서 "떠나는 순간" 한 번 호출됩니다.
        public virtual void Exit() { }
    }
}
