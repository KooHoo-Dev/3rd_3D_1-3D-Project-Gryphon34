using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class ClawAttack : BossStates
    {
        [SerializeField] private BossHitbox hitbox;
        [SerializeField] private float rotationSpeed = 120.0f;
        
        private bool isRotating = false;
        
        public override StateName Name => StateName.ClawAttackState;

        public override string AnimatorTrigger => "ClawsAttackLeft";

        public override void Enter()
        {
            isRotating = true;
        }

        protected override void Update()
        {
            base.Update();
            if (isRotating)
            {
                boss.transform.rotation = 
                    Quaternion.RotateTowards(boss.transform.rotation,
                        boss.transform.FlatRotationTo(boss.Player.transform), 
                        rotationSpeed * Time.deltaTime);
            }
        }
        
        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
            hitbox.gameObject.SetActive(false);
        }

        public override void Exit()
        {
            hitbox.gameObject.SetActive(false);
        }

        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            if(eventName == "HitboxOn")
            {
                isRotating = false;
                hitbox.gameObject.SetActive(true);
            }
            if(eventName == "HitboxOff") hitbox.gameObject.SetActive(false);
        }
    }
}