using UnityEngine;

namespace BossRaid.Player
{
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private float waitTime = 2.0f;
        private float currentWaitTime = 0.0f;
        
        [SerializeField] private float maxStamina = 100f;
        public float CurrentStamina { get; private set; }
        [SerializeField] private float staminaRegenRate = 35f;

        private void Awake()
        {
            CurrentStamina = maxStamina;
        }

        private void Update()
        {
            if (CurrentStamina >= maxStamina) return;
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

        private void RegenStamina()
        {
            currentWaitTime += Time.deltaTime;
            if(currentWaitTime >= waitTime) CurrentStamina +=staminaRegenRate * Time.deltaTime;
            if(CurrentStamina >= maxStamina) CurrentStamina = maxStamina;
        }

        public void ResetWaitTime()
        {
            currentWaitTime = 0.0f;
        }

#if UNITY_EDITOR
        // 임시 디버그 표시 (D-4 스태미나 바 만들면 삭제)
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            GUI.Label(new Rect(20, 20, 600, 40), $"Stamina {CurrentStamina:F1} / {maxStamina}", style);
            GUI.Label(new Rect(20, 55, 600, 40), $"Wait {currentWaitTime:F2} / {waitTime}", style);
        }
#endif
    }
}