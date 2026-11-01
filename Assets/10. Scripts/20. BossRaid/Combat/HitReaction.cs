namespace BossRaid.Combat
{
    // 플레이어가 맞았을 때 재생할 피격 모션. 보스 히트박스마다 인스펙터에서 정합니다
    public enum HitReaction
    {
        Small,
        Big,
        KnockDown,
        None, // 피해만 받고 모션 없음 (상쇄당한 공격). 인스펙터 값이 숫자로 저장돼서 맨 끝에 둠
    }
}
