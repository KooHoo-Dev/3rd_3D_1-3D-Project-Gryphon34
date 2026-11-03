using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 다크소울식 보스 바 (화면 아래 가운데): 이름, HP 바, 그 아래 얇은 피로도 바
    // - 보스가 플레이어를 발견하면 나타나고, 죽으면 사라짐
    // - 피로도 바는 때릴수록 줄고, 다 닳아 지친 동안은 바 테두리가 깜빡임 (쉬고 나면 Boss.HealFatigue가 다시 채움)
    // - HP 바 뒤 노란 잔상: 맞으면 깎인 만큼 잠깐 남았다가 천천히 따라 줄어듦 (다크소울처럼). 계속 때리면 계속 기다림
    // 값은 매 프레임 보스에게서 읽어 옵니다. 보스는 이 HUD를 모릅니다 (PlayerHUD와 같은 구조)
    // 화면 구성은 BossRaid/UI/Create Boss HUD 메뉴(BossHUDBuilder)가 만듭니다
    public class BossHUD : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private string bossName = "산룡";

        [Header("바 (채우기 Image, Image Type: Filled)")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image fatigueFill;
        [SerializeField] private Image fatigueBack; // 지친 동안 깜빡일 바 배경

        [Header("HP 잔상 (비우면 안 씀)")]
        [SerializeField] private Image healthDelayFill;        // HP 채우기 뒤에 깔린 Filled 이미지
        [SerializeField] private float delayWaitTime = 0.6f;   // 맞고 나서 잔상이 줄기 시작할 때까지
        [SerializeField] private float delayDrainSpeed = 0.5f; // 1초에 줄어드는 양 (바 전체 = 1)

        [Header("지침 깜빡임")]
        [SerializeField] private Color exhaustedFlashColor = new Color(1f, 0.85f, 0.3f, 0.9f);
        [SerializeField] private float flashSpeed = 4f; // 1초에 몇 번 왕복하는지
        [SerializeField] private float fadeTime = 0.5f;

        private Boss.Boss boss;
        private Color fatigueBackColor; // 깜빡이지 않을 때 원래 색
        private bool isShown;
        private float lastHealthRate = 1f; // 지난 프레임 HP 비율 (줄었으면 새로 맞은 것)
        private float delayTimer;

        private void Start()
        {
            group.alpha = 0f;
            fatigueBackColor = fatigueBack.color;
            nameText.text = bossName;

            boss = FindAnyObjectByType<Boss.Boss>();
            if (boss == null)
            {
                Debug.LogWarning("[BossHUD] 씬에 보스가 없어서 HUD를 끕니다");
                enabled = false;
                return;
            }
            boss.OnDeath += HandleBossDeath;
        }

        private void OnDestroy()
        {
            if (boss != null) boss.OnDeath -= HandleBossDeath;
            group.DOKill();
        }

        private void Update()
        {
            // 발견 전에는 안 보임. OnDiscovered는 Start보다 먼저 올 수도 있어서 값으로 봄
            if (isShown == false && boss.IsDiscovered && boss.CurrentHp > 0)
            {
                isShown = true;
                group.DOFade(1f, fadeTime);
            }

            float healthRate = boss.MaxHp > 0 ? boss.CurrentHp / boss.MaxHp : 0f;
            healthFill.fillAmount = healthRate;
            UpdateHealthDelay(healthRate);
            float maxFatigue = boss.Stage.MaxFatigue;
            fatigueFill.fillAmount = maxFatigue > 0 ? boss.CurrentFatigue / maxFatigue : 0f;

            if (boss.IsExhausted)
            {
                float t = Mathf.PingPong(Time.time * flashSpeed * 2f, 1f);
                fatigueBack.color = Color.Lerp(fatigueBackColor, exhaustedFlashColor, t);
            }
            else fatigueBack.color = fatigueBackColor;
        }

        // 히트스톱·튜토리얼 슬로모션 중에도 같은 속도로 줄게 unscaled 시간
        private void UpdateHealthDelay(float healthRate)
        {
            if (healthDelayFill == null) return;

            if (healthRate < lastHealthRate) delayTimer = delayWaitTime; // 새로 맞음 → 다시 기다림
            lastHealthRate = healthRate;

            if (healthDelayFill.fillAmount <= healthRate) // 처음이거나 HP가 참 → 잔상 없음
            {
                healthDelayFill.fillAmount = healthRate;
                return;
            }

            if (delayTimer > 0f) delayTimer -= Time.unscaledDeltaTime;
            else healthDelayFill.fillAmount = Mathf.MoveTowards(healthDelayFill.fillAmount, healthRate, delayDrainSpeed * Time.unscaledDeltaTime);
        }

        private void HandleBossDeath()
        {
            group.DOKill();
            group.DOFade(0f, fadeTime);
        }
    }
}
