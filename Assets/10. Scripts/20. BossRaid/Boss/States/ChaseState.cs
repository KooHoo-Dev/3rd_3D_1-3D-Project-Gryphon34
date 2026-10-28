using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class ChaseState : BossStates
    {
        [SerializeField] private DecideState decideState;
        [SerializeField] private float rotateSpeed = 120f;
        public override StateName Name => StateName.ChaseState;

        public override string AnimatorTrigger => "Walk";

        protected override void Update()
        {
            base.Update();
            boss.transform.rotation = 
                Quaternion.RotateTowards(boss.transform.rotation,
                    boss.transform.FlatRotationTo(boss.Player.transform), 
                    rotateSpeed * Time.deltaTime);

            if(decideState.CanAttack()) boss.ChangeState(StateName.DecideState);
        }
        
        
    }
}
