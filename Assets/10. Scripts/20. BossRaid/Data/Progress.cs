using System;

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
    }
}
