using UnityEngine;

namespace BossRaid.Combat
{
    public readonly struct AttackInfo
    {
        public readonly float Damage; // 무기 공격력 x 모션값 x 차지 배율
        public readonly float Fatigue; // 보스 피로도를 깎는 양
        public readonly Vector3 HitPosition; // 맞은 위치

        public AttackInfo(float damage, float fatigue, Vector3 hitPosition)
        {
            Damage = damage;
            Fatigue = fatigue;
            HitPosition = hitPosition;
        }
    }
}