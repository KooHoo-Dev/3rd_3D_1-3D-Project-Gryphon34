using System;
using System.Collections.Generic;

namespace BossRaid.Data
{
    // 씬을 넘어 남아야 하는 진행 상황 한 벌 (소울, 소재, 인간성 …)
    // GameData.Current로 하나만 들고 있고, 나중에 JsonWriter로 통째로 저장
    [Serializable]
    public class Progress
    {
        public int souls;
        public int humanity;

        public int currentStage = 1; // 지금 도전하는 단계

        // 단계별 첫 처치 (첫 처치 때만 인간성 1개, 1단계 첫 처치 = 2단계 해금)
        public bool stage1Cleared;
        public bool stage2Cleared;

        // 소재 개수. 크기는 MaterialType 개수만큼 (소재가 늘어도 여기는 안 고침)
        public int[] materials = new int[Enum.GetValues(typeof(MaterialType)).Length];

        // 죽은 자리에 떨어진 소울 (amount 0 = 없음). 한 번에 하나만 (또 죽으면 덮어씀)
        public DroppedSoul droppedSoul = new DroppedSoul();

        // 인간성: 태우면 회복약 최대 개수 +1 (최대 5), 사용하면 사람 상태 (최대 HP +25, 죽으면 끔)
        public int currentMaxPotion = 3;
        public bool isHuman;

        // 소재를 한 번이라도 얻은 적 있는지 (MaterialType 칸 번호). 요리 레시피 열림 조건 (꼬리 고기, 화룡 심장)
        public bool[] materialFound = new bool[Enum.GetValues(typeof(MaterialType)).Length];

        // 대장간: 첫 사망 때 열림 (HuntManager.Finish). 냄비는 stage1Cleared(2단계 해금)로 열림
        public bool forgeUnlocked;

        // 가진 장비 (id + 강화 단계)와 끼고 있는 장비 id. id는 ForgeTable의 Id 칸
        public List<GearLevel> weapons = new() { new GearLevel("Iron", 0) };
        public List<GearLevel> armors = new() { new GearLevel("Rags", 0) };
        public string equippedWeapon = "Iron";
        public string equippedArmor = "Rags";

        // 지금 먹은 요리 id ("" = 안 먹음). 사냥 한 번이 끝나면(처치든 사망이든) 비움
        public string meal = "";

        // 헌터북에서 읽은 페이지 id (HunterBookPanel.Page.id). 열렸는데 여기 없으면 NEW + 마을 도착 알림
        public List<string> readPages = new();

        // 그 장비를 몇 단계로 가지고 있는지 (없으면 -1)
        public static int GetLevel(List<GearLevel> list, string id)
        {
            foreach (GearLevel gear in list)
            {
                if (gear.id == id) return gear.level;
            }
            return -1;
        }
    }
}
