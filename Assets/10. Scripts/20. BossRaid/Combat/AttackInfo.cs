using UnityEngine;

namespace BossRaid.Combat
{
    public readonly struct AttackInfo
    {
        public readonly float Damage; // 무기 공격력 x 모션값 x 차지 배율
        public readonly float Fatigue; // 보스 피로도를 깎는 양
        public readonly Vector3 HitPosition; // 맞은 위치
        public readonly HitReaction Reaction; // 플레이어가 맞았을 때 재생할 피격 모션
        public readonly float BrokenPartBonus; // 깨진 부위를 때리면 더 곱하는 배율 (용뿔 대검, 기본 1)
        public readonly AttackTag Tag; // 보스 공격의 종류 (가드 불가·가벼운 공격·화염)
        public readonly Transform Source; // 공격한 쪽 (보스 몸, 없으면 히트박스). 가드 방향과 히트스탑에 씀
        public readonly bool Ignite; // 맞으면 화염 상태 (Fire 태그, 2단계 HP 비율 아래의 근접). 태그와 따로

        public AttackInfo(float damage, float fatigue, Vector3 hitPosition, HitReaction reaction = HitReaction.Small, float brokenPartBonus = 1f,
            AttackTag tag = AttackTag.Normal, Transform source = null, bool ignite = false)
        {
            Ignite = ignite;
            Damage = damage;
            Fatigue = fatigue;
            HitPosition = hitPosition;
            Reaction = reaction;
            BrokenPartBonus = brokenPartBonus;
            Tag = tag;
            Source = source;
        }
    }
}
