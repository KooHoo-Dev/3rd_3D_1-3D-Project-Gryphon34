using UnityEngine;

namespace BossRaid.Player
{
    public class PlayerPotion : MonoBehaviour
    {
        // Item-Drink 상태의 AnimStateEventSender에 적힌 신호 이름과 같아야 합니다
        private const string HealStartEvent = "HealStart"; // 병이 입에 닿는 순간
        private const string HealEndEvent = "HealEnd";     // 병을 입에서 떼는 순간

        [SerializeField] private int maxCount = 3;
        [SerializeField] private float healAmount = 50f;
        [SerializeField] private float healDuration = 0.6f; // HealStart부터 몇 초 동안 나눠서 채울지
        public int MaxCount => maxCount;
        public int CurrentCount { get; private set; }
        public bool CanDrink => CurrentCount > 0;

        private PlayerHealth health;
        private AnimStateEventListener animEventListener;

        private bool isHealing;
        private float healedAmount; // 이번 한 병으로 지금까지 채운 양

        private void Awake()
        {
            CurrentCount = maxCount;
            health = GetComponent<PlayerHealth>();
            animEventListener = GetComponentInChildren<AnimStateEventListener>(); // Animator와 같은 오브젝트에 있어야 Sender가 찾음
        }

        private void OnEnable()
        {
            animEventListener.OnOccursAnimStateEvent += HandleAnimStateEvent;
        }

        private void OnDisable()
        {
            animEventListener.OnOccursAnimStateEvent -= HandleAnimStateEvent;
        }

        private void Update()
        {
            if (isHealing == false) return;
            Heal(healAmount / healDuration * Time.deltaTime);
        }

        private void HandleAnimStateEvent(string eventName, string parameter)
        {
            if (eventName == HealStartEvent)
            {
                if (ReducePotion() == false) return;
                isHealing = true;
                healedAmount = 0f;
            }
            else if (eventName == HealEndEvent && isHealing)
            {
                // 남은 양을 한 번에 채워서 합계가 항상 healAmount가 되게 합니다
                Heal(healAmount - healedAmount);
            }
        }

        public bool ReducePotion()
        {
            if (CurrentCount <= 0)
            {
                CurrentCount = 0;
                return false;
            }
            CurrentCount -= 1;
            return true;
        }

        private void Heal(float amount)
        {
            amount = Mathf.Min(amount, healAmount - healedAmount); // healAmount를 넘지 않게
            health.TakeHeal(amount);
            healedAmount += amount;
            if (healedAmount >= healAmount) isHealing = false;
        }
    }
}
