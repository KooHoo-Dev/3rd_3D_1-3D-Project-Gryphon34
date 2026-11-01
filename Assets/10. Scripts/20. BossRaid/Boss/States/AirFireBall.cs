using UnityEngine;

namespace BossRaid.Boss.States
{
    // 공중 불덩이: 떠서 Shoot 이벤트마다 지상 불덩이(FireBall)와 같은 불덩이를 쏨 → attackCount발 뒤 착지
    public class AirFireBall : FlyAttack
    {
        public override StateName Name => StateName.AirFireBallState;

        protected override string AttackTrigger => "AirFireBall";

        [SerializeField] private FireBall fireBall; // 투사체·입 위치·소리를 같이 씀

        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            base.ReceiveAnimEvent(eventName, parameter);
            if (eventName == "Shoot")
            {
                fireBall.Shoot();
                CountAttack();
            }
        }
    }
}
