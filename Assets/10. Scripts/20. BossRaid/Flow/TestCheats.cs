using BossRaid.Data;
using BossRaid.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Flow
{
    // 임시: 플레이 테스트용 치트 + 화면 정보창. 에디터에서만 동작 (빌드에선 빈 컴포넌트)
    // 영상 녹화용 빌드: Assets/csc.rsp(-define:RECORDING_CHEATS)가 있으면 빌드에서도 동작 (정보창은 처음에 꺼짐, F1로 켬). 녹화 끝나면 csc.rsp 삭제
    // 클래스는 빌드에도 남겨 둠: 통째로 빼면 씬에 붙인 컴포넌트가 Missing Script가 됨
    // F1 정보창 / F2 Test Rich / F3 무적 / F4 보스 처치 / F5 지금 판을 2단계로 / F6 부위 하나 깨기 / F7 보스 지치게 / F8 부위 깨지기 직전
    // 두 씬에 빈 오브젝트 하나씩 만들어 붙이면 됨. 플레이어·보스·HuntManager는 알아서 찾음 (마을엔 보스가 없음)
    public class TestCheats : MonoBehaviour
    {
#if UNITY_EDITOR || RECORDING_CHEATS
        private bool showInfo; // 칸으로 두지 않음: #if 안의 칸은 빌드와 저장 모양이 달라짐. 에디터에선 켜고 시작 (Start)

        private PlayerController player;
        private Boss.Boss boss;
        private HuntManager huntManager;

        private float fightStartTime = -1f; // 보스가 플레이어를 발견한 시각. -1 = 아직
        private float fightEndTime = -1f;   // 보스·플레이어가 죽은 시각. -1 = 아직 싸우는 중
        private int exhaustCount;
        private float lastBossHp;
        private float lastDealt;            // 마지막 한 대에 준 피해
        private float lastPlayerHp;
        private float lastTaken;            // 마지막 한 대에 받은 피해 (불 지속 피해는 뺌)

        private GUIStyle style;

        private void Start()
        {
            showInfo = Application.isEditor; // 녹화 빌드에선 화면에 안 보이게 꺼 두고 시작
            player = FindAnyObjectByType<PlayerController>();
            boss = FindAnyObjectByType<Boss.Boss>();
            huntManager = FindAnyObjectByType<HuntManager>();

            if (player != null) lastPlayerHp = player.Health.CurrentHealth;
            if (boss != null)
            {
                lastBossHp = boss.CurrentHp;
                boss.OnDiscovered += HandleBossDiscovered;
                boss.OnExhausted += HandleBossExhausted;
                boss.OnHpReduce += HandleBossHpReduce;
                boss.OnDeath += HandleFightEnded;
            }
            if (player != null) player.Health.OnDeath += HandleFightEnded;
        }

        private void OnDestroy()
        {
            if (boss != null)
            {
                boss.OnDiscovered -= HandleBossDiscovered;
                boss.OnExhausted -= HandleBossExhausted;
                boss.OnHpReduce -= HandleBossHpReduce;
                boss.OnDeath -= HandleFightEnded;
            }
            if (player != null) player.Health.OnDeath -= HandleFightEnded;
        }

        private void HandleBossDiscovered()
        {
            if (fightStartTime < 0f) fightStartTime = Time.time;
        }

        private void HandleBossExhausted()
        {
            exhaustCount++;
        }

        private void HandleBossHpReduce()
        {
            lastDealt = lastBossHp - boss.CurrentHp;
            lastBossHp = boss.CurrentHp;
        }

        private void HandleFightEnded()
        {
            if (fightEndTime < 0f) fightEndTime = Time.time;
        }

        private void Update()
        {
            UpdateTakenDamage();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) showInfo = !showInfo;

            if (keyboard.f2Key.wasPressedThisFrame && player != null)
                player.Loadout.TestRich();

            if (keyboard.f3Key.wasPressedThisFrame && player != null)
            {
                player.Health.IsInvincible = !player.Health.IsInvincible;
                Debug.Log($"[테스트] 무적 {(player.Health.IsInvincible ? "켬" : "끔")} (불 지속 피해는 무적이어도 깎임)");
            }

            if (keyboard.f4Key.wasPressedThisFrame && huntManager != null)
                huntManager.BossKilled();

            if (keyboard.f6Key.wasPressedThisFrame && boss != null)
                boss.TestBreakNextPart();

            if (keyboard.f7Key.wasPressedThisFrame && boss != null)
                boss.ReduceFatigue(boss.CurrentFatigue); // 남은 피로를 다 깎음 → 퍼펙트 가드로 지칠 때와 같은 길 (이미 지쳤으면 안 함)

            if (keyboard.f8Key.wasPressedThisFrame && boss != null)
                boss.TestNearBreakParts();

            if (keyboard.f5Key.wasPressedThisFrame && boss != null)
            {
                // 보상·소울도 2단계로 맞추려고 GameData도 같이 바꿈
                GameData.Current.currentStage = 2;
                boss.SetStage(2);
                lastBossHp = boss.CurrentHp;
                fightStartTime = -1f;
                fightEndTime = -1f;
                exhaustCount = 0;
                Debug.Log("[테스트] 지금 판을 2단계로 (HP·피로 다시 채움, 판 시간·지침 횟수 처음부터)");
            }
        }

        // 받은 피해: 이벤트에 피해 값이 없어서 HP 차이로 잼. 불이 붙은 동안 매 프레임 조금씩 깎이는 건 빼려고 1 이상만
        private void UpdateTakenDamage()
        {
            if (player == null) return;

            float hp = player.Health.CurrentHealth;
            float drop = lastPlayerHp - hp;
            if (drop >= 1f) lastTaken = drop;
            lastPlayerHp = hp;
        }

        private void OnGUI()
        {
            if (showInfo == false) return;

            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15, richText = true };
            GUI.Box(new Rect(10f, 10f, 360f, 250f), BuildInfoText(), style);
        }

        private string BuildInfoText()
        {
            Progress progress = GameData.Current;
            string text = "<b>[테스트] F1 끄기 · F2 부자 · F3 무적 · F4 처치 · F5 2단계 · F6 부위 · F7 지침 · F8 부위 직전</b>\n";

            if (boss != null)
            {
                float fightTime = fightStartTime < 0f ? 0f : (fightEndTime < 0f ? Time.time : fightEndTime) - fightStartTime;
                text += $"판 시간 {(int)fightTime / 60}:{(int)fightTime % 60:00} · 지침 {exhaustCount}번 · {boss.StageNumber}단계\n";
                text += $"보스 HP {boss.CurrentHp:0} / {boss.MaxHp:0} · 피로 {boss.CurrentFatigue:0}\n";
                text += $"마지막에 준 피해 {lastDealt:0.#}\n";
            }

            if (player != null)
            {
                PlayerHealth health = player.Health;
                text += $"HP {health.CurrentHealth:0} / {health.MaxHealth:0}{(health.IsInvincible ? " <color=#FFD54A>무적</color>" : "")}\n";
                text += $"마지막에 받은 피해 {lastTaken:0.#}\n";

                ForgeTable forgeTable = player.Loadout.ForgeTable;
                WeaponValue weapon = forgeTable.GetWeapon(progress.equippedWeapon, Progress.GetLevel(progress.weapons, progress.equippedWeapon));
                ArmorValue armor = forgeTable.GetArmor(progress.equippedArmor, Progress.GetLevel(progress.armors, progress.equippedArmor));
                RecipeValue meal = player.Loadout.RecipeTable.Get(progress.meal);

                float baseAttack = player.Sword.BaseAttackPower;
                text += weapon != null
                    ? $"무기 {weapon.Name} +{weapon.Level} · 공격력 {baseAttack:0.#} × {weapon.AttackRate} = {baseAttack * weapon.AttackRate:0.#}\n"
                    : $"무기 {progress.equippedWeapon} (표에 없음)\n";
                text += armor != null
                    ? $"방어구 {armor.Name} +{armor.Level} · 피해 −{armor.DamageReduce * 100:0}% · 불 −{player.Loadout.FireReduce * 100:0}%\n"
                    : $"방어구 {progress.equippedArmor} (표에 없음)\n";
                text += $"요리 {(meal != null ? meal.Name : "없음")}\n";
            }

            text += $"소울 {progress.souls}";
            return text;
        }
#endif
    }
}
