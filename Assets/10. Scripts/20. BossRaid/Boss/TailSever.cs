using UnityEngine;

namespace BossRaid.Boss
{
    // 꼬리가 잘리면 잘린 꼬리(SeveredTail)를 그 자리에 생성해 빙글빙글 날립니다. 보스 루트에 붙입니다
    // Boss는 "깨졌다"(OnPartBroken)만 알리고, 보이는 건 여기서 (PartDecal과 같은 방식). 원래 꼬리는 Boss가 크기 0으로 숨김
    [RequireComponent(typeof(Boss))]
    public class TailSever : MonoBehaviour
    {
        [SerializeField] private BossPart tailPart;          // 꼬리 부위 (Boss의 TailPart와 같은 것)
        [SerializeField] private Transform tailBone;         // 잘리는 뼈 (MountainDragon_ Tail5, Boss의 TailPosition과 같은 것)
        [SerializeField] private SeveredTail severedTailPrefab;

        [SerializeField] private float upSpeed = 6f;   // 위로 튀는 속도
        [SerializeField] private float outSpeed = 4f;  // 몸 바깥쪽으로 튀는 속도
        [SerializeField] private float spinSpeed = 15f; // 회전 속도 (라디안/초)

        private Boss boss;

        private void Awake()
        {
            boss = GetComponent<Boss>();
        }

        private void OnEnable()
        {
            boss.OnPartBroken += HandlePartBroken;
        }

        private void OnDisable()
        {
            boss.OnPartBroken -= HandlePartBroken;
        }

        private void HandlePartBroken(BossPart part)
        {
            if (part != tailPart || severedTailPrefab == null) return;

            // 몸 가운데 → 잘린 자리 방향 (수평)
            Vector3 away = tailBone.position - transform.position;
            away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : -transform.forward;

            Vector3 velocity = Vector3.up * upSpeed + away * outSpeed;

            // 앞뒤로 공중제비 도는 축 + 조금 무작위
            Vector3 spinAxis = (Vector3.Cross(Vector3.up, away) + Random.insideUnitSphere * 0.5f).normalized;

            SeveredTail severed = Instantiate(severedTailPrefab, tailBone.position, Quaternion.identity);
            severed.IgnoreCollisions(GetComponentsInChildren<Collider>(true));
            severed.Launch(tailBone, velocity, spinAxis * spinSpeed);
        }
    }
}
