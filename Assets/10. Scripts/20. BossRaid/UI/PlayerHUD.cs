using BossRaid.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 몬스터헌터 월드식 플레이어 HUD입니다.
    // - 왼쪽 위: HP 바, 그 아래 스태미나 바. 바 길이는 최대치에 비례합니다 (최대 HP +25 같은 버프가 붙으면 바가 길어짐)
    // - 오른쪽 아래: 아이템 칸. 큰 칸 = 지금 고른 것(R로 씀), 옆 작은 칸 = 휠로 바꿀 것 (다크소울식)
    // 값은 매 프레임 플레이어에게서 읽어 옵니다. 플레이어는 이 HUD를 모릅니다.
    // 화면 구성은 BossRaid/UI/Create Player HUD 메뉴(PlayerHUDBuilder)가 만듭니다.
    public class PlayerHUD : MonoBehaviour
    {
        [Header("바 (배경 RectTransform + 채우기 Image, Image Type: Filled)")]
        [SerializeField] private RectTransform healthBar;
        [SerializeField] private Image healthFill;
        [SerializeField] private RectTransform staminaBar;
        [SerializeField] private Image staminaFill;
        [SerializeField] private float pixelsPerPoint = 4f; // 최대치 1당 바 길이(px). HP 100이면 400px

        [Header("아이템 칸 (큰 칸 = 고른 것, 작은 칸 = 휠로 바꿀 것)")]
        [SerializeField] private CanvasGroup mainSlot;
        [SerializeField] private Image mainIcon;
        [SerializeField] private TMP_Text mainCountText;
        [SerializeField] private CanvasGroup subSlot;
        [SerializeField] private Image subIcon;
        [SerializeField] private TMP_Text subCountText;
        [SerializeField] private Color potionColor = new Color(0.8f, 0.2f, 0.25f);
        [SerializeField] private Color humanityColor = new Color(0.85f, 0.7f, 0.3f); // ItemDisplay.HumanityColor와 같은 색
        [SerializeField, Range(0f, 1f)] private float emptySlotAlpha = 0.35f; // 0개일 때 칸 투명도

        private PlayerHealth health;
        private PlayerStamina stamina;
        private PlayerPotion potion;

        // 글자는 값이 바뀔 때만 다시 씁니다
        private int shownIndex = -1;
        private int shownPotionCount = -1;
        private int shownPotionMax = -1;
        private int shownHumanity = -1;

        private void Start()
        {
            // 씬마다 플레이어가 하나라서 찾아서 씁니다.
            // PlayerController.Awake에서 컴포넌트를 찾아 두므로 Awake가 아니라 Start에서 가져옵니다.
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[PlayerHUD] 씬에 PlayerController가 없어서 HUD를 끕니다");
                enabled = false;
                return;
            }

            health = player.Health;
            stamina = player.Stamina;
            potion = player.Potion;
        }

        private void Update()
        {
            UpdateBar(healthBar, healthFill, health.CurrentHealth, health.MaxHealth);
            UpdateBar(staminaBar, staminaFill, stamina.CurrentStamina, stamina.MaxStamina);
            UpdateItemSlots();
        }

        private void UpdateBar(RectTransform bar, Image fill, float current, float max)
        {
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, max * pixelsPerPoint);
            fill.fillAmount = max > 0 ? current / max : 0f;
        }

        // 큰 칸에 고른 칸(potion.CurrentIndex), 작은 칸에 다른 칸. 0 = 회복약, 1 = 인간성
        private void UpdateItemSlots()
        {
            int selected = potion.CurrentIndex;
            int other = 1 - selected;

            bool changed = selected != shownIndex || potion.CurrentCount != shownPotionCount
                           || potion.MaxCount != shownPotionMax || potion.HumanityCount != shownHumanity;
            if (changed)
            {
                shownIndex = selected;
                shownPotionCount = potion.CurrentCount;
                shownPotionMax = potion.MaxCount;
                shownHumanity = potion.HumanityCount;
                DrawSlot(mainIcon, mainCountText, selected);
                DrawSlot(subIcon, subCountText, other);
            }

            mainSlot.alpha = CanUse(selected) ? 1f : emptySlotAlpha;
            subSlot.alpha = CanUse(other) ? 1f : emptySlotAlpha;
        }

        private void DrawSlot(Image icon, TMP_Text countText, int index)
        {
            if (index == 0)
            {
                icon.color = potionColor;
                countText.text = $"{potion.CurrentCount}/{potion.MaxCount}";
            }
            else
            {
                icon.color = humanityColor;
                countText.text = potion.HumanityCount.ToString();
            }
        }

        private bool CanUse(int index)
        {
            return index == 0 ? potion.CanDrink : potion.CanUseHumanity;
        }
    }
}
