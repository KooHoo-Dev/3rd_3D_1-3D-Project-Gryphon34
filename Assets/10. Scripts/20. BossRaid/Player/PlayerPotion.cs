using System;
using Study.Utilities;
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
        [SerializeField] private float healDuration = 0.6f;
        public int MaxCount => maxCount;
        public int CurrentCount { get; private set; }
        public bool CanDrink => CurrentCount > 0;

        // 아이템 칸: 0 = 회복약, 1 = 인간성. 휠로 고름 (끝에서 멈춤)
        private int currentIndex;
        private int drinkIndex; // 마시기 시작한 순간의 칸. 마시는 도중에 휠을 굴려도 이 칸이 쓰임
        public int CurrentIndex => currentIndex;

        // 인간성 개수는 Progress를 모르니 씬 시작 담당(HuntManager/Bonfire)이 넘겨줌 (SetMaxPotionCount와 같은 방식)
        private int humanityCount;
        public int HumanityCount => humanityCount;
        public bool CanUseHumanity => humanityCount > 0;

        public event Action OnDrinkStarted;
        public event Action OnHumanityUsed; // 인간성을 마신 순간 (HealStart). 듣는 쪽이 Progress를 바꾸고 SetMaxHealth(true)

        private PlayerHealth health;
        private AnimStateEventListener animEventListener;

        private bool isHealing;
        private float healedAmount; // 이번 한 병으로 지금까지 채운 양

        private void Awake()
        {
            CurrentCount = maxCount;
            health = GetComponent<PlayerHealth>();
            animEventListener = GetComponentInChildren<AnimStateEventListener>();
        }

        // 씬 시작(저장된 값)과 화톳불에서 태우는 순간에 부름. Awake가 먼저 3으로 채워 두므로 여기서 다시 채움
        public void SetMaxPotionCount(int count)
        {
            maxCount = count;
            CurrentCount = maxCount;
        }

        public void SetHumanityCount(int count)
        {
            humanityCount = count;
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
            if (UI.PauseMenu.IsPaused) return; // 일시정지 중엔 휠·마시기 키를 안 받음
            UpdateIndex();

            if (isHealing == false) return;
            Heal(healAmount / healDuration * Time.deltaTime);
        }

        private void HandleAnimStateEvent(string eventName, string parameter)
        {
            if (eventName == HealStartEvent)
            {
                if (drinkIndex == 1)
                {
                    // 인간성: 한 번에 전부. 실제로 줄이고 사람 상태로 만드는 건 듣는 쪽(HuntManager/Bonfire)
                    if (CanUseHumanity) OnHumanityUsed?.Invoke();
                    return;
                }

                if (ReducePotion() == false) return;
                isHealing = true;
                healedAmount = 0f;
            }
            else if (eventName == HealEndEvent && isHealing)
            {
                Heal(healAmount - healedAmount);
            }
        }

        // DrinkState.Enter에서 부릅니다
        public void StartDrink()
        {
            drinkIndex = currentIndex; // 이 순간의 칸으로 고정
            OnDrinkStarted?.Invoke();   // 회복약이든 인간성이든 보스가 브레스 (GDD)
        }

        // 휠 내리기 = 다음 칸(+1), 올리기 = 이전 칸(−1). 0~1 끝에서 멈춤 (급할 때 돌아서 지나치지 않게)
        private void UpdateIndex()
        {
            float scroll = SimpleInput.GetMouseScrollDelta();
            if (scroll == 0f) return;

            int next = Mathf.Clamp(currentIndex + (scroll < 0f ? 1 : -1), 0, 1);
            currentIndex = next; // HUD가 읽어서 큰 칸에 보여 줌
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
