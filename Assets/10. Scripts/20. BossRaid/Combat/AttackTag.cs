namespace BossRaid.Combat
{
    // 보스 공격의 종류 (GDD §4 "공격 종류별 대응"). 보스 히트박스마다 인스펙터에서 정합니다
    // 상쇄 가능은 BossHitbox.Counterable 체크로 따로 봅니다 (상쇄 상자가 몸에 닿기 전에 읽어야 해서)
    public enum AttackTag
    {
        Normal,      // 구르기·가드 가능, 태클 불가
        Light,       // 가벼운 공격. 태클로 버팀
        Unguardable, // 가드 불가 (가드하면 일반 피격)
        Fire,        // 화염. 가드하면 스태미나를 더 씀
    }
}
