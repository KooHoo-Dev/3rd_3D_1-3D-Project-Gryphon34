using System.Collections.Generic;
using BossRaid.Data;
using BossRaid.Player;
using BossRaid.UI;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Flow
{
    public enum GearKind
    {
        Weapon,
        Armor
    }

    // 대장간 (마을 씬). [E]로 ForgePanel을 열고 닫고, 제작·강화·장착 규칙을 가지고 있습니다 (Bonfire와 같은 구조)
    // 표는 플레이어의 PlayerLoadout이 읽어 둔 것을 같이 씀
    public class Forge : MonoBehaviour
    {
        public PlayerController player;
        [SerializeField] private ForgePanel panel;
        [SerializeField] private float interactDistance = 3f;

        public ForgeTable Table => player.Loadout.ForgeTable;

        private void Awake()
        {
            // 첫 사망 전에는 대장간이 안 보임 (GDD §9). 마을 씬은 사냥에서 돌아올 때마다 새로 열려서 그때 다시 확인됨
            if (GameData.Current.forgeUnlocked == false) gameObject.SetActive(false);
        }

        private void Update()
        {
            if (panel == null) return;

            bool isNear = Vector3.Distance(player.transform.position, transform.position) <= interactDistance;
            panel.ShowPrompt(isNear);

            if (SimpleInput.GetKeyDown(Key.E) == false) return;
            if (panel.IsOpen) panel.Close();
            else if (isNear) panel.Open(this);
        }

        // 가진 단계 (없으면 -1)
        public int GetLevel(GearKind kind, string id)
        {
            return Progress.GetLevel(GetList(kind), id);
        }

        public GearValue GetValue(GearKind kind, string id, int level)
        {
            if (kind == GearKind.Weapon) return Table.GetWeapon(id, level);
            return Table.GetArmor(id, level);
        }

        // 다음에 만들 줄: 안 가졌으면 0단계(제작), 가졌으면 +1(강화). null이면 최대 강화
        public GearValue GetNext(GearKind kind, string id)
        {
            return GetValue(kind, id, GetLevel(kind, id) + 1);
        }

        public bool IsEquipped(GearKind kind, string id)
        {
            Progress progress = GameData.Current;
            return kind == GearKind.Weapon ? progress.equippedWeapon == id : progress.equippedArmor == id;
        }

        public bool CanForge(GearKind kind, string id)
        {
            GearValue next = GetNext(kind, id);
            return next != null && next.CanPay(GameData.Current);
        }

        // 제작 또는 강화. 끼고 있는 장비를 강화했으면 바로 다시 적용
        public void CraftOrUpgrade(GearKind kind, string id)
        {
            if (CanForge(kind, id) == false) return;

            Progress progress = GameData.Current;
            GearValue next = GetNext(kind, id);
            next.Pay(progress);

            List<GearLevel> list = GetList(kind);
            if (next.Level == 0) list.Add(new GearLevel(id, 0));
            else
            {
                foreach (GearLevel gear in list)
                {
                    if (gear.id == id) gear.level = next.Level;
                }
            }

            if (IsEquipped(kind, id)) player.Loadout.Apply();
            Debug.Log($"[대장간] {next.Name} {(next.Level == 0 ? "제작" : $"+{next.Level} 강화")} → 남은 소울 {progress.souls}");
        }

        // 가진 장비로 바꿔 낌
        public void Equip(GearKind kind, string id)
        {
            if (GetLevel(kind, id) < 0) return;

            Progress progress = GameData.Current;
            if (kind == GearKind.Weapon) progress.equippedWeapon = id;
            else progress.equippedArmor = id;

            player.Loadout.Apply();
            Debug.Log($"[대장간] {id} 장착");
        }

        private static List<GearLevel> GetList(GearKind kind)
        {
            Progress progress = GameData.Current;
            return kind == GearKind.Weapon ? progress.weapons : progress.armors;
        }
    }
}
