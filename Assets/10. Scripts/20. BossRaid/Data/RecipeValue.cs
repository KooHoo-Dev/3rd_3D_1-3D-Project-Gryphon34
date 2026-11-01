namespace BossRaid.Data
{
    // 요리 한 줄. 비용의 Souls = 기본 재료(빵·마른 고기·들풀)값의 합, TailMeat·DragonHeart = 보스 재료
    [System.Serializable]
    public class RecipeValue : CostValue
    {
        public string Id;
        public string Name;
        public string Unlock;            // 이 소재를 한 번이라도 얻으면 열림 (MaterialType 이름). 비어 있으면 처음부터

        public float MaxStamina;         // 최대 스태미나 +
        public float StaminaRegen;       // 스태미나 회복 속도 + (0.15 = 15%)
        public float ChargeTimeReduce;   // 차지 단계 도달 시간 − (0.2 = 20% 빨리)
        public bool QuickStandUp;        // 넘어지면 바로 기상
        public float TackleReduce;       // 태클 중 받는 피해 감소. 태클이 생기면 연결
        public float FireReduce;         // 불 피해 감소. 화염이 생기면 연결
        public float GuardStaminaReduce; // 가드 시 스태미나 소모 감소. 가드가 생기면 연결
    }
}
