using UnityEngine;

namespace BossRaid.Player.States
{
    // 서서 포션을 마시는 상태입니다. (Item-Drink)
    // 회복은 PlayerPotion이 애니메이터 신호(HealStart / HealEnd)를 받아서 합니다.
    public class DrinkState : PlayerStatesBase
    {
        public DrinkState(PlayerController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.StopMovement();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
        }
    }
}
