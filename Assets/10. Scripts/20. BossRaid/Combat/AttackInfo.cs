using UnityEngine;

namespace BossRaid.Combat
{
    public readonly struct AttackInfo
    {
        public readonly float Damage; // 무기 공격력 x 모션값 x 차지 배율
        public readonly float Fatigue; // 보스 피로도를 깎는 양
        public readonly Vector3 HitPosition; // 맞은 위치
        public readonly HitReaction Reaction; // 플레이어가 맞았을 때 재생할 피격 모션

        public AttackInfo(float damage, float fatigue, Vector3 hitPosition, HitReaction reaction = HitReaction.Small)
        {
            Damage = damage;
            Fatigue = fatigue;
            HitPosition = hitPosition;
            Reaction = reaction;
        }
    }
}