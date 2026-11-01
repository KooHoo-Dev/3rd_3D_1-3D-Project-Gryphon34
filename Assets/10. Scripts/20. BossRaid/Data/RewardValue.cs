namespace BossRaid.Data
{
    [System.Serializable]
    public class RewardValue
    {
        public int Stage;
        public int Souls;          // 처치 소울 (사망 소울 계산의 기준이기도 함)
        public int ScaleMin;       // 화룡 비늘
        public int ScaleMax;
        public int RageMin;        // 분노 비늘
        public int RageMax;
        public float RageChance;   // 분노 비늘이 나올 확률 (0~1). 나오면 RageMin~RageMax개
        public float HeartChance;  // 화룡 심장이 나올 확률 (0~1)
        public float DeathRatio;   // 사망 소울 = Souls × 준 피해 비율 × DeathRatio
        public float HumanityChance; // 첫 처치가 아닐 때 인간성 1개가 나올 확률 (0~1)
        public float PartBrokenHumanityChance; // 부위가 깨질 때마다 인간성 1개가 나올 확률 (0~1)
        public float HpRate;           // 체력 구간 간격 (0.25 → 75/50/25%마다)
        public int HpRateReward;       // 구간 하나를 넘을 때 주는 개수
        public MaterialType RewardPart; // 구간마다 주는 소재 종류
    }
}
