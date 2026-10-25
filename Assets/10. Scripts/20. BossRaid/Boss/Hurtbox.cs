using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{

    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour, IDamageable
    {
        [SerializeField] private BossPart part;

        private Collider[] hurtColliders;

        public object Owner => part.Owner;

        private void Awake()
        {
            hurtColliders = GetComponents<Collider>();
        }

        public void TakeDamage(AttackInfo info)
        {
            part.TakeDamage(info);
        }
        
        public void Disable()
        {
            foreach (Collider hurtCollider in hurtColliders)
            {
                hurtCollider.enabled = false;
            }
        }
    }
}
