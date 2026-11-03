using UnityEngine;

namespace BossRaid.Boss
{
    // 잘린 꼬리 프리팹(Boss_Tail) 맨 위에 붙입니다. TailSever가 생성한 뒤 Launch로 날립니다
    // 프리팹 구조: Boss_Tail → Tail5(Rigidbody·Capsule) → [Tail6…, root(크기 0 = 몸이 잘린 자리로 오그라듦)]
    public class SeveredTail : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;        // 프리팹 안의 Tail5
        [SerializeField] private float maxSpinSpeed = 30f; // 기본 최대 회전 속도(7)로는 빙글빙글이 약해서 올림
        [SerializeField] private float lifeTime = 0f;    // 0 = 계속 남음 (몬헌처럼 바닥에 두기)

        private void Awake()
        {
            body.maxAngularVelocity = maxSpinSpeed;
        }

        // 보스의 꼬리 뼈(cutBone) 자리·방향·크기에 맞춰 놓고, 속도와 회전을 한 번 줍니다
        public void Launch(Transform cutBone, Vector3 velocity, Vector3 spin)
        {
            body.transform.SetPositionAndRotation(cutBone.position, cutBone.rotation);
            body.transform.localScale = cutBone.lossyScale; // 보스 크기 그대로 (Tail5가 맨 위 바로 밑이라 이걸로 맞음)

            body.linearVelocity = velocity;
            body.angularVelocity = spin;

            if (lifeTime > 0f) Destroy(gameObject, lifeTime);
        }

        // 보스 몸 콜라이더와 부딪혀 튕겨 나가지 않게
        public void IgnoreCollisions(Collider[] others)
        {
            Collider[] mine = GetComponentsInChildren<Collider>();
            foreach (Collider mineCollider in mine)
            {
                foreach (Collider other in others) Physics.IgnoreCollision(mineCollider, other);
            }
        }
    }
}
