using UnityEngine;

namespace BossRaid.Boss
{
    // 앞으로 날아가다가 explodeMask 레이어에 닿거나 수명이 끝나면 폭발 이펙트를 남기고 사라집니다
    // 피해는 직격이 아니라 폭발 히트박스가 한 번만 줍니다 (직격이어도 그 자리에서 터져서 폭발에 맞음)
    [RequireComponent(typeof(Rigidbody))]
    public class FireBallProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float homingSpeed = 30f; // 목표 쪽으로 초당 몇 도까지 꺾이는지
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private LayerMask explodeMask;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private float explosionLifeTime = 3f;
        [SerializeField] private BossHitbox explosionHitboxPrefab;
        [SerializeField] private float explosionHitTime = 0.1f; // 폭발 판정이 켜져 있는 시간

        private Rigidbody projectileRigidbody;
        private Transform target;
        private float aimHeight;
        private float currentLifeTime = 0f;
        private bool isExploded = false;

        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
            projectileRigidbody.isKinematic = true;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // 쏜 쪽(FireBall)이 생성 직후 불러서 따라갈 목표를 알려 줍니다
        public void SetTarget(Transform target, float aimHeight)
        {
            this.target = target;
            this.aimHeight = aimHeight;
        }

        private void FixedUpdate()
        {
            Quaternion rotation = projectileRigidbody.rotation;
            if (target != null)
            {
                Vector3 targetPosition = target.position + Vector3.up * aimHeight;
                Quaternion lookRotation = Quaternion.LookRotation(targetPosition - projectileRigidbody.position);
                rotation = Quaternion.RotateTowards(rotation, lookRotation, homingSpeed * Time.fixedDeltaTime);
                projectileRigidbody.MoveRotation(rotation);
            }

            Vector3 forward = rotation * Vector3.forward;
            projectileRigidbody.MovePosition(projectileRigidbody.position + forward * (speed * Time.fixedDeltaTime));
        }

        private void Update()
        {
            currentLifeTime += Time.deltaTime;
            if (currentLifeTime >= lifeTime) Explode();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (LayerMaskExtensions.Contains(explodeMask, other.gameObject) == false) return;
            Explode();
        }

        private void Explode()
        {
            if (isExploded) return;
            isExploded = true;

            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(explosion, explosionLifeTime);
            }

            BossHitbox explosionHitbox = Instantiate(explosionHitboxPrefab, transform.position, Quaternion.identity);
            Destroy(explosionHitbox.gameObject, explosionHitTime);

            Destroy(gameObject);
        }
    }
}
