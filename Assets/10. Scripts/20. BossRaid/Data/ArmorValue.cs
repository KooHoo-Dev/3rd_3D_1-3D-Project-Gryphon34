namespace BossRaid.Data
{
    [System.Serializable]
    public class ArmorValue : GearValue
    {
        public float DamageReduce;        // 받는 피해 감소 (0.15 = 15%)
        public float FireReduce;          // 불 피해 감소. 화염이 생기면 연결
        public float GuardStaminaReduce;  // 가드 시 스태미나 소모 감소. 가드가 생기면 연결
    }
}
