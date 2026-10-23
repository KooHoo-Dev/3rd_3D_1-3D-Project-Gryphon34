using UnityEngine;

namespace BossRaid.Combat
{
    public readonly struct AttackInfo
    {
        public readonly float Damage; // 기본 데미지 x 차지 배율
        public readonly float Stamina; // 경직? 스태미너치
        public readonly Vector3 HitPosition; // 맞은 위치

        public AttackInfo(float damage, float stamina, Vector3 hitPosition)
        {
            Damage = damage;
            Stamina = stamina;
            HitPosition = hitPosition;
        }
    }
}