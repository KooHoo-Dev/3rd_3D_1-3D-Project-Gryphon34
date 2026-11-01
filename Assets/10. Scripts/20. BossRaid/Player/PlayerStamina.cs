using UnityEngine;

namespace BossRaid.Player
{
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private float waitTime = 2.0f;
        private float currentWaitTime = 0.0f;

        [SerializeField] private float maxStamina = 100f;
        public float MaxStamina => maxStamina + bonusMaxStamina; // 요리로 +. maxStamina(인스펙터)는 안 바꿈
        public float CurrentStamina { get; private set; }
        [SerializeField] private float staminaRegenRate = 35f;

        private float bonusMaxStamina; // 요리: 최대 스태미나 + (PlayerLoadout이 넣어 줌)
        private float bonusRegenRate;  // 요리: 회복 속도 + (0.15 = 15% 빨리)

        private void Awake()
        {
            CurrentStamina = MaxStamina;
        }

        private void Update()
        {
            if (CurrentStamina >= MaxStamina) return;
            RegenStamina();
        }

        public bool CanUseStamina(float amount)
        {
            if ((CurrentStamina > 0) == false) return false;
            return true;
        }

        public bool TryUseStamina(float amount)
        {
            if ((CurrentStamina > 0) == false) return false;
            CurrentStamina -= amount;
            if (CurrentStamina <= 0) CurrentStamina = 0;
            currentWaitTime = 0.0f;
            return true;
        }

        // 쓴 스태미나를 돌려줌 (시작한 공격을 상쇄로 바꿀 때)
        public void RestoreStamina(float amount)
        {
            CurrentStamina += amount;
            if (CurrentStamina >= MaxStamina) CurrentStamina = MaxStamina;
        }

        private void RegenStamina()
        {
            currentWaitTime += Time.deltaTime;
            if(currentWaitTime >= waitTime) CurrentStamina += staminaRegenRate * (1f + bonusRegenRate) * Time.deltaTime;
            if(CurrentStamina >= MaxStamina) CurrentStamina = MaxStamina;
        }

        public void ResetWaitTime()
        {
            currentWaitTime = 0.0f;
        }

        // 먹은 요리의 스태미나 효과. PlayerLoadout이 씬 시작과 요리를 먹을 때 부름. 다 채워 줌
        public void SetBonus(float maxBonus, float regenBonus)
        {
            bonusMaxStamina = maxBonus;
            bonusRegenRate = regenBonus;
            CurrentStamina = MaxStamina;
        }
    }
}
