namespace BossRaid.Data
{
    [System.Serializable]
    public class WeaponValue : GearValue
    {
        public float AttackRate;       // 기본 공격력(GreatSword 인스펙터 attackPower)에 곱하는 배율 (철 대검 = 1)
        public float BrokenPartBonus;  // 깨진 부위에 더 곱하는 배율 (용뿔 대검, 1 = 효과 없음)
        public float GuardBonus;       // 가드 강함 (철 대검). 가드가 생기면 연결
    }
}
