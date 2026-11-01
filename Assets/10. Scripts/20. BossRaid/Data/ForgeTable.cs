using System.Collections.Generic;
using Jay.FileIO;
using UnityEngine;

namespace BossRaid.Data
{
    // 무기 표 + 방어구 표 (대장간에서 만들고 강화하는 것)
    public class ForgeTable
    {
        private const string WeaponTableName = "ForgeTable/WeaponTable.tsv";
        private const string ArmorTableName = "ForgeTable/ArmorTable.tsv";

        private readonly List<WeaponValue> weapons = new();
        private readonly List<ArmorValue> armors = new();

        public WeaponValue GetWeapon(string id, int level) => Find(weapons, id, level);
        public ArmorValue GetArmor(string id, int level) => Find(armors, id, level);

        // 표에 나온 순서대로 장비 이름 (같은 장비는 한 번만) → 대장간 목록 순서
        public List<string> WeaponIds => GetIds(weapons);
        public List<string> ArmorIds => GetIds(armors);

        public void Load()
        {
            weapons.Clear();
            armors.Clear();

            List<WeaponValue> weaponRows = TSVReader.ReadTable<WeaponValue>(Application.streamingAssetsPath + "/" + WeaponTableName);
            if (weaponRows != null) weapons.AddRange(weaponRows);

            List<ArmorValue> armorRows = TSVReader.ReadTable<ArmorValue>(Application.streamingAssetsPath + "/" + ArmorTableName);
            if (armorRows != null) armors.AddRange(armorRows);
        }

        // 그 장비의 그 단계 줄 (없으면 null = 그 단계가 없음 → 최대 강화)
        private static T Find<T>(List<T> list, string id, int level) where T : GearValue
        {
            foreach (T value in list)
            {
                if (value.Id == id && value.Level == level) return value;
            }
            return null;
        }

        private static List<string> GetIds<T>(List<T> list) where T : GearValue
        {
            List<string> ids = new();
            foreach (T value in list)
            {
                if (ids.Contains(value.Id) == false) ids.Add(value.Id);
            }
            return ids;
        }
    }
}
