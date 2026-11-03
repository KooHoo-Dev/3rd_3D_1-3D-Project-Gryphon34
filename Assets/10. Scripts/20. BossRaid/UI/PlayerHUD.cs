using BossRaid.Data;
using BossRaid.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 다크소울식 플레이어 HUD입니다.
    // - 왼쪽 위: HP 바, 그 아래 스태미나 바. 바 길이는 최대치에 비례합니다 (최대 HP +25 같은 버프가 붙으면 바가 길어짐)
    //   맞으면 HP 바 뒤에 깎인 만큼 흰 잔상이 남았다가 따라 줄어듦
    // - 왼쪽 아래: 아이템 칸. 큰 칸 = 지금 고른 것(R로 씀), 옆 작은 칸 = 휠로 바꿀 것
    // - 오른쪽 아래: 가진 소울 (GameData.Current.souls). 늘면 위에 +N을 띄우고 잠깐 뒤 주르륵 더함 (다크소울처럼). 줄면 바로
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

        [Header("HP 잔상 (비우면 안 씀)")]
        [SerializeField] private Image healthDelayFill;        // HP 채우기 뒤에 깔린 Filled 이미지
        [SerializeField] private float delayWaitTime = 0.8f;   // 맞고 나서 잔상이 줄기 시작할 때까지
        [SerializeField] private float maxHold = 1.2f;         // 연달아 맞아도 잔상이 기다리는 시간은 합쳐서 여기까지
        [SerializeField] private float delayDrainSpeed = 30f;  // 1초에 줄어드는 HP (보스와 달리 비율이 아님. 바가 길어져도 같은 속도)

        [Header("아이템 칸 (큰 칸 = 고른 것, 작은 칸 = 휠로 바꿀 것)")]
        [SerializeField] private CanvasGroup mainSlot;
        [SerializeField] private Image mainIcon;
        [SerializeField] private TMP_Text mainCountText;
        [SerializeField] private TMP_Text mainNameText; // 비우면 이름을 안 씀
        [SerializeField] private CanvasGroup subSlot;
        [SerializeField] private Image subIcon;
        [SerializeField] private TMP_Text subCountText;
        [SerializeField] private TMP_Text subNameText;  // 비우면 이름을 안 씀
        [SerializeField] private Sprite potionSprite;   // 넣으면 그림 그대로(흰색), 비우면 아래 색만
        [SerializeField] private Sprite humanitySprite;
        [SerializeField] private Color potionColor = new Color(0.8f, 0.2f, 0.25f);
        [SerializeField] private Color humanityColor = new Color(0.85f, 0.7f, 0.3f); // ItemDisplay.HumanityColor와 같은 색
        [SerializeField, Range(0f, 1f)] private float emptySlotAlpha = 0.35f; // 0개일 때 칸 투명도

        [Header("소울 (비우면 안 씀)")]
        [SerializeField] private TMP_Text soulsText;
        [SerializeField] private TMP_Text soulsGainText;       // 소울 칸 위 +N. 비우면 주르륵 없이 바로 바뀜
        [SerializeField] private float soulsCountDelay = 0.8f; // +N을 보여 준 뒤 세기 시작할 때까지
        [SerializeField] private float soulsCountTime = 1f;    // 다 세는 데 걸리는 시간 (양이 많아도 같음)

        private PlayerHealth health;
        private PlayerStamina stamina;
        private PlayerPotion potion;

        // 글자는 값이 바뀔 때만 다시 씁니다
        private int shownIndex = -1;
        private int shownPotionCount = -1;
        private int shownPotionMax = -1;
        private int shownHumanity = -1;
        private int shownSouls = -1;
        private int shownGain = -1;

        // 소울 주르륵: 화면 숫자(countingSouls)가 목표(countTarget)까지 countSpeed로 올라감
        private float countingSouls = -1f;
        private int countTarget;
        private float countSpeed;
        private float countWaitTimer;

        // HP 잔상: 비율이 아니라 HP 값으로 기억 (최대 HP가 늘어 비율만 줄 때 맞은 것처럼 보이지 않게)
        private float delayedHealth; // 잔상이 가리키는 HP
        private float lastHealth;    // 지난 프레임 HP (줄었으면 새로 맞은 것)
        private float delayTimer;
        private float heldTime;      // 이번 잔상이 지금까지 기다린 시간

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
            delayedHealth = health.CurrentHealth;
            lastHealth = health.CurrentHealth;
        }

        private void Update()
        {
            UpdateBar(healthBar, healthFill, health.CurrentHealth, health.MaxHealth);
            UpdateHealthDelay(health.CurrentHealth, health.MaxHealth);
            UpdateBar(staminaBar, staminaFill, stamina.CurrentStamina, stamina.MaxStamina);
            UpdateItemSlots();
            UpdateSouls();
        }

        private void UpdateSouls()
        {
            if (soulsText == null) return;
            int souls = GameData.Current.souls;

            // 씬 처음, 소울을 씀(대장간·냄비), +N 글자가 없음 → 바로 바꿈
            if (countingSouls < 0f || souls < countingSouls || soulsGainText == null)
            {
                countingSouls = souls;
                countTarget = souls;
            }
            else if (souls != countTarget) // 새로 들어옴 (주움, 사냥 결과) → +N을 보여 주고 잠깐 기다림. 세는 중에 또 들어오면 합쳐서 다시
            {
                countTarget = souls;
                countWaitTimer = soulsCountDelay;
                countSpeed = (souls - countingSouls) / soulsCountTime;
            }

            // 히트스톱·슬로모션 중에도 같은 속도로 세게 unscaled 시간
            if (countingSouls < countTarget)
            {
                if (countWaitTimer > 0f) countWaitTimer -= Time.unscaledDeltaTime;
                else countingSouls = Mathf.MoveTowards(countingSouls, countTarget, countSpeed * Time.unscaledDeltaTime);
            }

            int shown = (int)countingSouls;
            if (shown != shownSouls)
            {
                shownSouls = shown;
                soulsText.text = shown.ToString();
            }

            if (soulsGainText == null) return;
            int gain = countTarget - shown; // 세는 만큼 +N이 줄어듦
            if (gain == shownGain) return;
            shownGain = gain;
            soulsGainText.enabled = gain > 0;
            soulsGainText.text = $"+{gain}";
        }

        private void UpdateBar(RectTransform bar, Image fill, float current, float max)
        {
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, max * pixelsPerPoint);
            fill.fillAmount = max > 0 ? current / max : 0f;
        }

        // 히트스톱·튜토리얼 슬로모션 중에도 같은 속도로 줄게 unscaled 시간
        private void UpdateHealthDelay(float current, float max)
        {
            if (healthDelayFill == null) return;

            if (current < lastHealth) // 새로 맞음 → 다시 기다림. 연달아 맞아도 합쳐서 maxHold까지만 (불 피해는 매 프레임이라 없으면 안 줆)
            {
                if (delayedHealth <= lastHealth) heldTime = 0f; // 잔상이 다 따라왔었으면 새로 시작
                delayTimer = Mathf.Min(delayWaitTime, Mathf.Max(0f, maxHold - heldTime));
            }
            lastHealth = current;

            if (delayedHealth <= current) // HP가 참(회복약, 부활) → 잔상 없음
            {
                delayedHealth = current;
            }
            else if (delayTimer > 0f)
            {
                delayTimer -= Time.unscaledDeltaTime;
                heldTime += Time.unscaledDeltaTime;
            }
            else
            {
                delayedHealth = Mathf.MoveTowards(delayedHealth, current, delayDrainSpeed * Time.unscaledDeltaTime);
            }

            healthDelayFill.fillAmount = max > 0 ? delayedHealth / max : 0f;
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
                DrawSlot(mainIcon, mainCountText, mainNameText, selected);
                DrawSlot(subIcon, subCountText, subNameText, other);
            }

            mainSlot.alpha = CanUse(selected) ? 1f : emptySlotAlpha;
            subSlot.alpha = CanUse(other) ? 1f : emptySlotAlpha;
        }

        private void DrawSlot(Image icon, TMP_Text countText, TMP_Text nameText, int index)
        {
            if (index == 0)
            {
                DrawIcon(icon, potionSprite, potionColor);
                countText.text = $"{potion.CurrentCount}/{potion.MaxCount}";
                if (nameText != null) nameText.text = ItemDisplay.PotionName;
            }
            else
            {
                DrawIcon(icon, humanitySprite, humanityColor);
                countText.text = potion.HumanityCount.ToString();
                if (nameText != null) nameText.text = ItemDisplay.HumanityName;
            }
        }

        // 그림이 있으면 그 그림을 원래 색(흰색)으로, 없으면 지금 그림에 색만 입힘 (아이콘 그림이 생기기 전 임시)
        private void DrawIcon(Image icon, Sprite sprite, Color fallbackColor)
        {
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
            }
            else
            {
                icon.color = fallbackColor;
            }
        }

        private bool CanUse(int index)
        {
            return index == 0 ? potion.CanDrink : potion.CanUseHumanity;
        }
    }
}
