using Study.Utilities;
using UnityEngine;

namespace BossRaid.Player.States
{
    public class DodgeState:PlayerStatesBase
    {
        public DodgeState(PlayerController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.Health.IsInvincible = false;
            Owner.StopMovement(false);
            Owner.Movement.UpdateDodgeRotation(SimpleInput.GetMoveAxisRaw());
            Owner.ClearComboBonus();
            if (Owner.Burn != null) Owner.Burn.OnDodge(); // 화염 상태: 구르기 횟수
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            Owner.Health.IsInvincible = stateInfo.normalizedTime >= 0.09f &&
                                        stateInfo.normalizedTime <= 0.423f;
            
            if(stateInfo.normalizedTime >= 0.423f)
                Owner.Health.IsInvincible = false;
        }

        public override void Exit()
        {
            Owner.Health.IsInvincible = false;
        }
        
    }
}