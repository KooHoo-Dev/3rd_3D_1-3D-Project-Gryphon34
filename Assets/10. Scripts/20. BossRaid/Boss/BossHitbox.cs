using System.Collections.Generic;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{
    public class BossHitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask mask;
        [SerializeField] private float damage = 20;
        [SerializeField] public string id;
        [SerializeField] private HitReaction reaction; // 이 히트박스에 맞은 플레이어의 피격 모션

        private float fatigueDamage = 0;
        
        private HashSet<object> playerHash = new HashSet<object>();

        private void OnEnable()
        {
            ResetHash();
        }
        
        public void ResetHash()
        {
            playerHash.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (LayerMaskExtensions.Contains(mask, other.gameObject) == false) return;
            if (other.TryGetComponent(out IDamageable damageable) == false) return;
            if (playerHash.Contains(damageable.Owner)) return;
            playerHash.Add(damageable.Owner);

            AttackInfo info = new AttackInfo(damage, fatigueDamage, other.transform.position, reaction);
            damageable.TakeDamage(info);
        }
    }
}