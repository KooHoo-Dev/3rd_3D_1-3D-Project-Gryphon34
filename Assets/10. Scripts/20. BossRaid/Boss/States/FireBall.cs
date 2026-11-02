using UnityEngine;

namespace BossRaid.Boss.States
{
    public class FireBall : Attack
    {
        public override StateName Name => StateName.FireBallState;

        public override string AnimatorTrigger => "FireBall";

        [SerializeField] private FireBallProjectile projectilePrefab;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private float aimHeight = 1f; // 발밑 대신 몸통을 노림

        // 쏜 뒤: 다음 패턴 사거리 안까지 Chase로 따라가서 다음 패턴 (불덩이 → 점프 물기)
        [SerializeField] private ChaseState chaseState;
        [SerializeField] private StateName nextPattern = StateName.JumpBiteState;
        [SerializeField] private float nextRange = 20f;

        protected override void OnAnimationEnd()
        {
            // 불덩이 중에 지쳤으면 예약(점프 물기) 대신 쉬기 → IdleState에서 쓰러짐
            if (boss.IsExhausted)
            {
                base.OnAnimationEnd();
                return;
            }
            chaseState.Reserve(nextPattern, nextRange);
            boss.ChangeState(StateName.ChaseState);
        }

        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            base.ReceiveAnimEvent(eventName, parameter);
            if (eventName == "Shoot") Shoot();
        }

        // 쏘는 순간의 플레이어 위치를 향해 입에서 불덩이를 만듭니다. 공중 불덩이(AirFireBall)도 이걸 부름
        public void Shoot()
        {
            Vector3 target = boss.Player.transform.position + Vector3.up * aimHeight;
            Quaternion rotation = Quaternion.LookRotation(target - shootPoint.position);
            FireBallProjectile projectile = Instantiate(projectilePrefab, shootPoint.position, rotation);
            projectile.SetTarget(boss.Player.transform, aimHeight);
            projectile.SetOwner(boss);
            PlaySound("Shoot"); // 이 상태(FireBall)의 Sounds 목록. 공중 불덩이도 여기 것을 씀
        }
    }
}
