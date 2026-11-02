using System.Collections.Generic;
using System.Text;
using BossRaid.Data;
using BossRaid.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 대장간 메뉴 (마을 씬). Forge가 [E]로 열고 닫고, 버튼은 Forge의 함수를 부릅니다
    // 규칙(비용, 단계)은 Forge에 있고, 이 패널은 보여 주기 + 열려 있는 동안 플레이어 입력 막기만 합니다
    // 줄: 무기 표의 장비들 → 방어구 표의 장비들 (표 순서). 줄마다 [제작/강화] [장착]
    public class ForgePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;   // "[E] 대장간" (가까이 있을 때만)
        [SerializeField] private CanvasGroup window;
        [SerializeField] private TMP_Text walletText;   // 가진 소울·소재
        [SerializeField] private Transform rowParent;
        [SerializeField] private MenuRow rowTemplate;   // 꺼 둔 줄 하나. 장비마다 복제합니다
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }

        private Forge forge;
        private readonly List<MenuRow> rows = new();

        private void Start()
        {
            SetWindowVisible(false);
            promptText.gameObject.SetActive(false);
            rowTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(Close);
        }

        public void ShowPrompt(bool show)
        {
            promptText.gameObject.SetActive(show && IsOpen == false);
        }

        public void Open(Forge owner)
        {
            forge = owner;
            IsOpen = true;
            promptText.gameObject.SetActive(false);
            SetWindowVisible(true);
            MenuLock.Set(forge.player, true);
            Refresh();
        }

        public void Close()
        {
            if (IsOpen == false) return;
            IsOpen = false;
            SetWindowVisible(false);
            MenuLock.Set(forge.player, false);
        }

        // 지금 값으로 줄을 전부 다시 만듦 (버튼을 누를 때마다)
        private void Refresh()
        {
            walletText.text = GetWalletText(GameData.Current);

            foreach (MenuRow row in rows) Destroy(row.gameObject);
            rows.Clear();

            foreach (string id in forge.Table.WeaponIds) AddRow(GearKind.Weapon, id);
            foreach (string id in forge.Table.ArmorIds) AddRow(GearKind.Armor, id);
        }

        private void AddRow(GearKind kind, string id)
        {
            int level = forge.GetLevel(kind, id);
            GearValue current = level >= 0 ? forge.GetValue(kind, id, level) : null;
            GearValue next = forge.GetNext(kind, id);
            GearValue shown = current ?? next;
            if (shown == null) return;

            string title = level > 0 ? $"{shown.Name} +{level}" : shown.Name;
            if (level < 0) title += "  (미제작)";

            string info = GetStatText(shown);
            if (current != null && next != null) info += $"\n→ +{next.Level}: {GetStatText(next)}"; // 강화하면 이렇게 바뀜

            string cost = next != null ? next.ToCostText() : "최대 강화";

            MenuRow row = Instantiate(rowTemplate, rowParent);
            row.gameObject.SetActive(true);
            row.SetText(title, info, cost);

            string mainLabel = next == null ? "최대" : level < 0 ? "제작" : "강화";
            row.SetMain(mainLabel, forge.CanForge(kind, id), () =>
            {
                forge.CraftOrUpgrade(kind, id);
                Refresh();
            });

            bool equipped = forge.IsEquipped(kind, id);
            string subLabel = level < 0 ? "" : equipped ? "장착 중" : "장착";
            row.SetSub(subLabel, level >= 0 && equipped == false, () =>
            {
                forge.Equip(kind, id);
                Refresh();
            });

            rows.Add(row);
        }

        private string GetStatText(GearValue value)
        {
            List<string> parts = new();
            if (value is WeaponValue weapon)
            {
                float attack = forge.player.Sword.BaseAttackPower * weapon.AttackRate; // 실제 공격력 = 기본 × 배율
                parts.Add($"공격력 {attack:0} (×{weapon.AttackRate})");
                if (weapon.BrokenPartBonus != 1f) parts.Add($"깨진 부위 ×{weapon.BrokenPartBonus}");
                if (weapon.GuardBonus > 0f) parts.Add($"가드 스태미나 −{weapon.GuardBonus * 100:0}%");
            }
            else if (value is ArmorValue armor)
            {
                if (armor.DamageReduce > 0f) parts.Add($"받는 피해 −{armor.DamageReduce * 100:0}%");
                if (armor.FireReduce > 0f) parts.Add($"불 피해 −{armor.FireReduce * 100:0}%");
                if (armor.GuardStaminaReduce > 0f) parts.Add($"가드 스태미나 −{armor.GuardStaminaReduce * 100:0}%");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "효과 없음";
        }

        // "소울 1200   화룡 비늘 3 · 뿔 1" (가진 소재만)
        private static string GetWalletText(Progress progress)
        {
            StringBuilder text = new StringBuilder($"{ItemDisplay.SoulName} {progress.souls}");
            bool first = true;
            for (int i = 0; i < progress.materials.Length; i++)
            {
                if (progress.materials[i] <= 0) continue;
                text.Append(first ? "   " : " · ");
                text.Append($"{ItemDisplay.GetName((MaterialType)i)} {progress.materials[i]}");
                first = false;
            }
            return text.ToString();
        }

        private void SetWindowVisible(bool visible)
        {
            window.alpha = visible ? 1f : 0f;
            window.blocksRaycasts = visible;
            window.interactable = visible;
        }
    }
}
