using UnityEngine;

namespace BossRaid.Player.States
{
    public class DrinkState : PlayerStatesBase
    {
        public DrinkState(PlayerController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.StopMovement();
            Owner.Potion.StartDrink();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
        }
    }
}
