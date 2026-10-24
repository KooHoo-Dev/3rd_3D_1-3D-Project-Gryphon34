using System.Collections.Generic;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Player
{
    public class WeaponCollider : MonoBehaviour
    {
        [SerializeField] private LayerMask mask;
        
        private HashSet<object> bossHash = new HashSet<object>();
        private float damage;
        private float stamina;

        public void SetAttackValue(float swordDamage, float swordStamina)
        {
            damage = swordDamage;
            stamina = swordStamina;
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

            AttackInfo info = new AttackInfo(damage, stamina, other.transform.position);
            damageable.TakeDamage(info);
        }
    }
}