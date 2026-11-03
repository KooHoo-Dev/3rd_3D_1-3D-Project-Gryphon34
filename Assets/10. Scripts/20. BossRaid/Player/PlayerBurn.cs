using System;
using UnityEngine;

namespace BossRaid.Player
{
    // 화염 상태: 불이 붙는 공격에 맞으면 켜짐 → HP가 주르륵 닳음 → 구르기 N번이나 시간이 다 되면 꺼짐
    // 지속 피해는 피격이 아님 (행동을 안 끊고, 가드·태클·무적과 상관없음)
    public class PlayerBurn : MonoBehaviour
    {
        [SerializeField] private float burnDamagePerSecond;
        [SerializeField] private float burnTime;              // 안 굴러도 이 시간이 지나면 꺼짐
        [SerializeField] private int dodgesToExtinguish = 3;  // 이만큼 구르면 꺼짐
        [SerializeField] private ParticleSystem burnEffect;   // 타는 동안 켜 둠 (비면 안 함)

        public bool IsBurning { get; private set; }
        public int DodgesToExtinguish => dodgesToExtinguish;
        public event Action OnIgnited; // 꺼져 있다가 불이 붙은 순간만 (장판 위에서 매 프레임 다시 붙는 건 안 셈). TutorialHint가 봄

        private PlayerController player;
        private PlayerHealth playerHealth;
        private float remainTime;
        private int dodgeCount;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        // player.Health는 PlayerController.Awake에서 채워져서 컴포넌트 순서에 따라 아직 null일 수 있음 → 직접 가져온 것으로
        private void OnEnable()
        {
            playerHealth.OnDeath += Extinguish;
        }

        private void OnDisable()
        {
            playerHealth.OnDeath -= Extinguish;
        }

        // 불이 붙음. 이미 타고 있으면 시간·구르기 횟수를 처음부터 (화염 장판 위에선 매 프레임 불림)
        public void Ignite()
        {
            if (player.Health.IsDead) return;
            bool wasBurning = IsBurning;
            if (wasBurning == false) Debug.Log($"[화염] 불이 붙음 ({burnTime}초, 구르기 {dodgesToExtinguish}번)");
            IsBurning = true;
            remainTime = burnTime;
            dodgeCount = 0;
            if (burnEffect != null) burnEffect.Play();
            if (wasBurning == false) OnIgnited?.Invoke();
        }

        // 구르기를 시작할 때 DodgeState가 부름
        public void OnDodge()
        {
            if (IsBurning == false) return;
            dodgeCount++;
            if (dodgeCount >= dodgesToExtinguish) Extinguish();
        }

        private void Update()
        {
            if (IsBurning == false) return;

            float fireReduce = player.Loadout != null ? player.Loadout.FireReduce : 0f;
            player.Health.TakeBurnDamage(burnDamagePerSecond * Time.deltaTime * (1f - fireReduce));

            remainTime -= Time.deltaTime;
            if (remainTime <= 0f) Extinguish();
        }

        private void Extinguish()
        {
            if (IsBurning == false) return;
            IsBurning = false;
            if (burnEffect != null) burnEffect.Stop();
            Debug.Log("[화염] 꺼짐");
        }
    }
}
