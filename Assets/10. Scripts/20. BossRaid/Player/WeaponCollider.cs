using System.Collections.Generic;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Player
{
    public class WeaponCollider : MonoBehaviour
    {
        [SerializeField] private LayerMask mask;
        [SerializeField] private float motionValue = 0.1f;   // 이 동작의 데미지 비율 (무기 공격력에 곱함)
        [SerializeField] private float motionFatigue = 5f;   // 이 동작이 보스 피로도를 깎는 기본량

        private HashSet<object> bossHash = new HashSet<object>();
        private float swordDamage;  // 무기 공격력 x 차지 배율 (GreatSword가 넣어줌)
        private float fatigueRate;  // 차지 배율 (GreatSword가 넣어줌)

        public void SetAttackValue(float swordDamage, float fatigueRate)
        {
            this.swordDamage = swordDamage;
            this.fatigueRate = fatigueRate;
        }

        public void ResetHash()
        {
            bossHash.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (LayerMaskExtensions.Contains(mask, other.gameObject) == false) return;
            if (other.TryGetComponent(out IDamageable damageable) == false) return;
            if (bossHash.Contains(damageable.Owner)) return;
            bossHash.Add(damageable.Owner);

            AttackInfo info = new AttackInfo(swordDamage * motionValue, motionFatigue * fatigueRate, other.transform.position);
            damageable.TakeDamage(info);
        }
    }
}