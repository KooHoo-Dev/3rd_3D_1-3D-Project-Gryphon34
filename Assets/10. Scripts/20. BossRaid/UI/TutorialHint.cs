using BossRaid.Boss.States;
using BossRaid.Data;
using BossRaid.Flow;
using BossRaid.Player;
using DG.Tweening;
using Study.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.UI
{
    // 튜토리얼 안내 (투기장 씬). 기능마다 처음 필요해지는 순간에 버튼만 한 번 알려 줍니다. 언제 쓸지(타이밍)는 안 가르침
    // - 보스 첫 공격의 Cue 칸 이벤트(때리기 직전): 가드·구르기 + 슬로모션 (Ctrl·Space를 누르거나, 맞거나, 공격이 끝나거나, maxSlowTime이 지나면 원래 속도)
    //   공격 시작에 띄우면 예비동작 중에 눌러서 구르기 무적·퍼펙트 가드가 빗나감 → HitboxOn보다 조금 앞에 Cue를 넣어 둘 것 (Cue 없는 공격은 건너뜀)
    // - 첫 일반 가드: 퍼펙트 가드
    // - 가드 없이 hitsBeforeReminder번 맞음: 가드·구르기 다시
    // - 처음 불이 붙음: 구르면 꺼진다 (슬로모션 없음. HP가 닳는 중이라 바로 굴러야 함)
    // - 보스가 처음 지침 (BossHUD 피로도 바가 다 닳음): 지금이 기회 (슬로모션 없음. 때릴 시간을 뺏지 않게)
    // - 사망: 이번 판에 안 써 본 기능 팁 (이건 매번)
    // 본 안내 id는 Progress.seenTips (사망 팁은 안 적음). 보스·플레이어·HuntManager는 이 안내를 모릅니다 (ResultPanel과 같은 구조)
    // 화면 구성은 BossRaid/UI/Create Tutorial Hint 메뉴(TutorialHintBuilder)가 만듭니다
    public class TutorialHint : MonoBehaviour
    {
        private const string GuardId = "Guard";
        private const string PerfectGuardId = "PerfectGuard";
        private const string GuardReminderId = "GuardReminder";
        private const string BurnId = "Burn";
        private const string ExhaustedId = "Exhausted";
        private const string CueEvent = "Cue"; // 보스 공격 칸의 AnimStateEventSender에 적은 신호 이름과 같아야 함
        private const string HitboxOnEvent = "HitboxOn";

        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        [Header("보이기 (실제 초. 슬로모션이어도 안 늘어남)")]
        [SerializeField] private float showTime = 5f;
        [SerializeField] private float fadeTime = 0.25f;

        [Header("첫 공격 슬로모션")]
        [SerializeField, Range(0.05f, 1f)] private float slowScale = 0.25f;
        [SerializeField] private float maxSlowTime = 2.5f; // 아무것도 안 눌러도 이 시간(실제 초) 뒤 원래 속도

        [Header("가드 없이 이만큼 맞으면 가드 안내 다시 (한 번만)")]
        [SerializeField] private int hitsBeforeReminder = 3;

        [Header("글 ([Ctrl]처럼 대괄호 안은 키 색)")]
        [SerializeField] private string keyColor = "#F0C060";
        [SerializeField] private string guardTitle = "공격이 온다!";
        [SerializeField, TextArea] private string guardBody = "[Ctrl] 누르고 있으면 가드     [Space] 구르기";
        [SerializeField] private string reminderTitle = "막거나 피할 수 있습니다";
        [SerializeField] private string perfectTitle = "가드 성공";
        [SerializeField, TextArea] private string perfectBody = "맞기 직전에 [Ctrl]을 누르면 퍼펙트 가드\n밀리지 않고 다음 공격이 강해집니다";
        [SerializeField] private string burnTitle = "불이 붙었다!";
        [SerializeField, TextArea] private string burnBody = "[Space] {0}번 구르면 불이 꺼집니다"; // {0} = PlayerBurn의 구르기 횟수
        [SerializeField] private string exhaustedTitle = "보스가 지쳤다!";
        [SerializeField, TextArea] private string exhaustedBody = "때릴수록 아래 노란 바(피로도)가 줄고, 다 닳으면 쓰러집니다\n지금이 공격할 기회예요";

        [Header("사망 팁 (가드 안 함 → 가드, 퍼펙트 안 함 → 퍼펙트, 둘 다 했으면 아래에서 하나)")]
        [SerializeField] private string deathTitle = "팁";
        [SerializeField, TextArea] private string deathGuardTip = "[Ctrl]을 누르고 있으면 가드합니다. 막으면 피해가 크게 줄어요";
        [SerializeField, TextArea] private string[] deathTips =
        {
            "보스를 계속 때리면 지쳐서 쓰러집니다. 쓰러진 동안이 기회예요",
            "부위를 부수면 소재를 얻습니다. 죽어도 받아요",
            "마을 대장간에서 소재로 장비를 만들고 강화할 수 있어요",
        };

        private PlayerController player;
        private Boss.Boss boss;
        private HuntManager hunt;

        private bool isShowing;
        private float hideTime; // Time.unscaledTime 기준

        private BossStates cueAttack; // Cue를 기다리는 공격 (첫 공격 안내를 아직 안 봤을 때만)

        private bool isSlow;
        private float slowEndTime;          // Time.unscaledTime 기준
        private BossStates slowAttack;      // 슬로모션을 건 공격. 다른 상태로 바뀌면 끝
        private float defaultFixedDeltaTime; // 슬로모션 중 물리도 같은 비율로 (안 하면 리지드바디가 뚝뚝 끊김)

        // 이번 판에 한 것 (사망 팁 고르기)
        private bool hasGuarded;
        private bool hasPerfectGuarded;
        private int hitsWithoutGuard;

        private void Start()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            defaultFixedDeltaTime = Time.fixedDeltaTime;

            // PlayerController.Awake에서 Health를 찾아 두므로 Start에서 구독
            player = FindAnyObjectByType<PlayerController>();
            boss = FindAnyObjectByType<Boss.Boss>();
            hunt = FindAnyObjectByType<HuntManager>();
            if (player == null || boss == null)
            {
                Debug.LogWarning("[TutorialHint] 씬에 플레이어나 보스가 없어서 안내를 끕니다");
                enabled = false;
                return;
            }

            boss.OnStateChanged += HandleBossStateChanged;
            boss.OnAnimEvent += HandleBossAnimEvent;
            boss.OnExhausted += HandleBossExhausted;
            player.OnGuarded += HandleGuarded;
            player.Health.OnHitTaken += HandleHitTaken;
            if (player.Burn != null) player.Burn.OnIgnited += HandleIgnited;
            if (hunt != null) hunt.OnHuntFinished += HandleHuntFinished;
        }

        private void OnDestroy()
        {
            if (boss != null)
            {
                boss.OnStateChanged -= HandleBossStateChanged;
                boss.OnAnimEvent -= HandleBossAnimEvent;
                boss.OnExhausted -= HandleBossExhausted;
            }
            if (player != null)
            {
                player.OnGuarded -= HandleGuarded;
                player.Health.OnHitTaken -= HandleHitTaken;
                if (player.Burn != null) player.Burn.OnIgnited -= HandleIgnited;
            }
            if (hunt != null) hunt.OnHuntFinished -= HandleHuntFinished;

            SetSlow(false); // 슬로모션 중에 씬이 바뀌어도 마을이 느려지지 않게
            group.DOKill();
        }

        private void Update()
        {
            if (PauseMenu.IsPaused) return; // 일시정지 중에 슬로모션 시간이 끝나 timeScale을 1로 되돌리는 것 막기

            if (isSlow)
            {
                bool pressed = SimpleInput.GetKeyDown(Key.LeftCtrl) || SimpleInput.GetKeyDown(Key.Space);
                if (pressed || Time.unscaledTime >= slowEndTime || boss.CurrentState != slowAttack) SetSlow(false);
            }

            if (isShowing && Time.unscaledTime >= hideTime) Hide();
        }

        // 보스 공격 시작 (Attack을 상속한 상태. 포효·회전·추적은 아님). 안내는 아직 안 띄우고 이 공격의 Cue를 기다림
        // 공격이 아닌 상태에서 cueAttack을 지우면 안 됨: DecideState.Enter 안에서 공격으로 바꾸므로
        // 알림이 "공격 → Decide" 순서로 와서 방금 적은 공격이 지워짐. 다른 상태의 Cue는 HandleBossAnimEvent의 CurrentState 검사가 막음
        private void HandleBossStateChanged(BossStates state)
        {
            if ((state is Attack) == false) return;
            if (GameData.Current.seenTips.Contains(GuardId)) return;

            cueAttack = state;
        }

        // 기다리던 공격의 Cue = 곧 때림 → 지금 안내 + 슬로모션. 그래서 보고 바로 누르면 무적·퍼펙트 가드 안에 맞음
        private void HandleBossAnimEvent(string eventName, string parameter)
        {
            if (cueAttack == null || boss.CurrentState != cueAttack) return;

            if (eventName == HitboxOnEvent)
            {
                // Cue 없이 때리기 시작함 → 이 공격은 건너뛰고 다음 공격을 기다림
                Debug.LogWarning($"[TutorialHint] {cueAttack.name}: HitboxOn 앞에 Cue 이벤트가 없어서 첫 공격 안내를 건너뜁니다");
                cueAttack = null;
                return;
            }
            if (eventName != CueEvent) return;

            TryMarkSeen(GuardId);
            Show(guardTitle, guardBody, showTime);
            slowAttack = cueAttack;
            cueAttack = null;
            slowEndTime = Time.unscaledTime + maxSlowTime;
            SetSlow(true);
        }

        // 처음으로 일반 가드에 성공 → 퍼펙트 가드를 알려 줌 (처음부터 퍼펙트였으면 이미 아는 것으로 봄)
        private void HandleGuarded(bool isPerfect)
        {
            hasGuarded = true;
            if (isPerfect)
            {
                hasPerfectGuarded = true;
                TryMarkSeen(PerfectGuardId);
                return;
            }
            if (TryMarkSeen(PerfectGuardId)) Show(perfectTitle, perfectBody, showTime);
        }

        private void HandleHitTaken()
        {
            SetSlow(false);
            if (hasGuarded) return;

            hitsWithoutGuard++;
            if (hitsWithoutGuard < hitsBeforeReminder) return;
            if (TryMarkSeen(GuardReminderId)) Show(reminderTitle, guardBody, showTime);
        }

        // 처음 불이 붙음 → 끄는 법. 첫 공격 안내가 떠 있었으면 덮어씀 (맞은 뒤라 그 안내는 할 일을 다 함)
        private void HandleIgnited()
        {
            if (TryMarkSeen(BurnId) == false) return;
            Show(burnTitle, string.Format(burnBody, player.Burn.DodgesToExtinguish), showTime);
        }

        // 보스가 처음 지침 → 피로도 바와 지금이 기회라는 것
        private void HandleBossExhausted()
        {
            if (TryMarkSeen(ExhaustedId)) Show(exhaustedTitle, exhaustedBody, showTime);
        }

        // 사망: 마을로 돌아갈 때까지 팁을 띄움. 처치면 떠 있던 안내만 내림
        private void HandleHuntFinished(HuntResult result, HuntReward reward)
        {
            SetSlow(false);
            if (result != HuntResult.Died)
            {
                Hide();
                return;
            }
            Show(deathTitle, PickDeathTip(), hunt.ReturnDelay + 1f);
        }

        private string PickDeathTip()
        {
            if (hasGuarded == false) return deathGuardTip;
            if (hasPerfectGuarded == false || deathTips == null || deathTips.Length == 0) return perfectBody;
            return deathTips[Random.Range(0, deathTips.Length)];
        }

        // 처음 보는 안내면 본 것으로 적고 true. 이미 봤으면 false
        private bool TryMarkSeen(string id)
        {
            var seenTips = GameData.Current.seenTips;
            if (seenTips.Contains(id)) return false;
            seenTips.Add(id);
            Debug.Log($"[Tutorial] 안내 {id}");
            return true;
        }

        private void Show(string title, string body, float duration)
        {
            titleText.text = title;
            bodyText.text = ColorKeys(body);
            group.DOKill();
            group.DOFade(1f, fadeTime).SetUpdate(true); // SetUpdate(true) = timeScale 무시 (슬로모션 중에도 제 속도)
            isShowing = true;
            hideTime = Time.unscaledTime + duration;
        }

        private void Hide()
        {
            isShowing = false;
            group.DOKill();
            group.DOFade(0f, fadeTime).SetUpdate(true);
        }

        private void SetSlow(bool slow)
        {
            if (slow == isSlow) return;
            isSlow = slow;
            Time.timeScale = slow ? slowScale : 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        }

        // "[Ctrl]" → 키 색 (TMP 리치 텍스트)
        private string ColorKeys(string text)
        {
            return text.Replace("[", $"<color={keyColor}>[").Replace("]", "]</color>");
        }
    }
}
