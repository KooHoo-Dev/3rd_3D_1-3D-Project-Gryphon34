using System;
using BossRaid.Data;
using BossRaid.Weapon;
using UnityEngine;

namespace BossRaid.Player
{
    // 끼고 있는 무기·방어구와 먹은 요리(GameData.Current)를 표에서 찾아 플레이어에 넣어 줍니다
    // 씬이 시작될 때(마을·투기장) 한 번, 대장간에서 장비를 바꾸거나 냄비에서 요리를 먹을 때 Apply를 다시 부릅니다
    // 불 피해·가드·퍼펙트 가드 효과는 값만 들고 있고, 그 기능(PlayerHealth·PlayerBurn·PlayerController)이 여기서 읽어 갑니다
    public class PlayerLoadout : MonoBehaviour
    {
        [Serializable]
        public class WeaponModel
        {
            public string id;          // WeaponTable의 Id (Iron, Horn, TailBone)
            public GameObject model;   // 칼 모양만 (콜라이더·트레일은 GreatSword 쪽에 그대로 두고 메시만)
        }

        [Serializable]
        public class ArmorLook
        {
            public string id;          // ArmorTable의 Id (Rags, DragonSet)
            public GameObject[] parts; // 이 방어구를 입으면 켤 모듈 파츠 (몸통·팔·다리·투구 …)
        }

        [SerializeField] private WeaponModel[] weaponModels;
        [SerializeField] private ArmorLook[] armorLooks;

        public ForgeTable ForgeTable { get; private set; }
        public RecipeTable RecipeTable { get; private set; }

        public float FireReduce { get; private set; }          // 방어구 + 요리를 더한 값. PlayerHealth·PlayerBurn이 읽음
        public float GuardStaminaRate { get; private set; } = 1f; // 가드 스태미나에 곱함 = (1 − 방어구)(1 − 요리)(1 − 철 대검). 곱해서 아무리 쌓여도 0이 안 됨
        public float PerfectGuardFatigue { get; private set; } // 퍼펙트 가드 때 보스 피로를 더 깎는 양 (요리). PlayerController가 인스펙터 값에 더함
        public float TackleReduce { get; private set; }        // 안 씀 (태클 뺌, 11/02). 안 쓰는 태클 코드가 읽어서 남김

        public bool QuickStandUp { get; private set; } // KnockDownState가 읽음

        private GreatSword sword;
        private PlayerHealth health;
        private PlayerStamina stamina;

        private void Awake()
        {
            sword = GetComponentInChildren<GreatSword>();
            health = GetComponent<PlayerHealth>();
            stamina = GetComponent<PlayerStamina>();

            ForgeTable = new ForgeTable();
            ForgeTable.Load();
            RecipeTable = new RecipeTable();
            RecipeTable.Load();
        }

        private void Start()
        {
            Apply();
        }

        public void Apply()
        {
            Progress progress = GameData.Current;

            WeaponValue weapon = ForgeTable.GetWeapon(progress.equippedWeapon, Progress.GetLevel(progress.weapons, progress.equippedWeapon));
            ArmorValue armor = ForgeTable.GetArmor(progress.equippedArmor, Progress.GetLevel(progress.armors, progress.equippedArmor));
            RecipeValue meal = RecipeTable.Get(progress.meal);

            if (weapon != null) sword.SetWeapon(weapon.AttackRate, weapon.BrokenPartBonus);
            else
            {
                sword.SetWeapon(1f, 1f);
                Debug.LogWarning($"[PlayerLoadout] 무기 '{progress.equippedWeapon}' 줄이 없어서 배율 1 (인스펙터 공격력 그대로)");
            }
            sword.SetChargeTimeRate(meal != null ? 1f - meal.ChargeTimeReduce : 1f);

            health.SetDamageReduce(armor != null ? armor.DamageReduce : 0f);
            stamina.SetBonus(meal != null ? meal.MaxStamina : 0f, meal != null ? meal.StaminaRegen : 0f);

            FireReduce = (armor != null ? armor.FireReduce : 0f) + (meal != null ? meal.FireReduce : 0f);
            GuardStaminaRate = (1f - (armor != null ? armor.GuardStaminaReduce : 0f))
                               * (1f - (meal != null ? meal.GuardStaminaReduce : 0f))
                               * (1f - (weapon != null ? weapon.GuardBonus : 0f));
            PerfectGuardFatigue = meal != null ? meal.PerfectGuardFatigue : 0f;
            TackleReduce = meal != null ? meal.TackleReduce : 0f;
            QuickStandUp = meal != null && meal.QuickStandUp;

            ShowWeaponModel(progress.equippedWeapon);
            ShowArmorLook(progress.equippedArmor);

            Debug.Log($"[장비] 무기 {weapon?.Name} +{weapon?.Level} (공격력 {sword.BaseAttackPower} × {weapon?.AttackRate}), " +
                      $"방어구 {armor?.Name} +{armor?.Level} (피해 −{armor?.DamageReduce * 100}%), 식사 {(meal != null ? meal.Name : "없음")}");
        }

        // 임시: 대장간·냄비 테스트용. 소울 5000, 소재 10개씩, 레시피 전부 열림, 꺼져 있던 대장간·냄비 켜기
        [ContextMenu("Test Rich (대장간·냄비 열기)")]
        public void TestRich() // TestCheats F2도 부름
        {
            Progress progress = GameData.Current;
            progress.souls += 5000;
            for (int i = 0; i < progress.materials.Length; i++)
            {
                progress.materials[i] += 10;
                progress.materialFound[i] = true;
            }
            progress.forgeUnlocked = true;
            progress.stage1Cleared = true;

            foreach (Flow.Forge forge in FindObjectsByType<Flow.Forge>(FindObjectsInactive.Include, FindObjectsSortMode.None)) forge.gameObject.SetActive(true);
            foreach (Flow.CookingPot pot in FindObjectsByType<Flow.CookingPot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) pot.gameObject.SetActive(true);
            Debug.Log("[테스트] 소울 +5000, 소재 +10, 대장간·냄비 열림");
        }

        // 입은 방어구의 파츠만 켬: 어느 방어구 목록에든 들어 있는 파츠를 전부 끄고 → 입은 것의 파츠만 켬
        // 어느 목록에도 없는 파츠(얼굴 등)는 안 건드림. 목록에 없는 id면 다 그대로
        private void ShowArmorLook(string armorId)
        {
            if (armorLooks == null || armorLooks.Length == 0) return;

            ArmorLook current = null;
            foreach (ArmorLook look in armorLooks)
            {
                if (look.id == armorId) current = look;
            }
            if (current == null) return;

            foreach (ArmorLook look in armorLooks)
            {
                foreach (GameObject part in look.parts)
                {
                    if (part != null) part.SetActive(false);
                }
            }
            foreach (GameObject part in current.parts)
            {
                if (part != null) part.SetActive(true);
            }
        }

        // 끼고 있는 무기의 칼 모양만 켬 (목록에 없는 id면 다 그대로)
        private void ShowWeaponModel(string weaponId)
        {
            if (weaponModels == null || weaponModels.Length == 0) return;

            bool found = false;
            foreach (WeaponModel weaponModel in weaponModels)
            {
                if (weaponModel.id == weaponId) found = true;
            }
            if (found == false) return;

            foreach (WeaponModel weaponModel in weaponModels)
            {
                if (weaponModel.model != null) weaponModel.model.SetActive(weaponModel.id == weaponId);
            }
        }
    }
}
