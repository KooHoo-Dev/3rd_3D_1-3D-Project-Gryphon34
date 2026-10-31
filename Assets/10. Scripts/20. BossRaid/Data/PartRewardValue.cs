namespace BossRaid.Data
{
    // PartRewardTable.tsv 한 줄 = 한 부위가 깨질 때 주는 소재 하나. 필드 이름 = 열 이름 (RewardValue와 같은 방식)
    [System.Serializable]
    public class PartRewardValue
    {
        public int Stage;
        public string Part;            // BossPart의 partName과 글자가 같아야 합니다
        public MaterialType Material;  // 표에 enum 이름으로 적음. 오타면 표를 읽을 때 에러
        public int Count;
    }
}
