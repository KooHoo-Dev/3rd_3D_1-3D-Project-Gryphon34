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

        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            base.ReceiveAnimEvent(eventName, parameter);
            if (eventName == "Shoot") Shoot();
        }

        // 쏘는 순간의 플레이어 위치를 향해 입에서 불덩이를 만듭니다
        private void Shoot()
        {
            Vector3 target = boss.Player.transform.position + Vector3.up * aimHeight;
            Quaternion rotation = Quaternion.LookRotation(target - shootPoint.position);
            FireBallProjectile projectile = Instantiate(projectilePrefab, shootPoint.position, rotation);
            projectile.SetTarget(boss.Player.transform, aimHeight);
        }
    }
}
