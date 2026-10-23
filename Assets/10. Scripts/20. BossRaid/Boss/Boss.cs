using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{
    public class Boss : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100;
        public float CurrentHp { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);

        private void Awake()
        {
            CurrentHp = maxHp;
        }
        
        public void ApplyHit(AttackInfo info, BossPart part)
        {
            if (IsDead) return;

            float calculateDamage = info.Damage * part.CurrentMagnitude;
            
            CurrentHp -= calculateDamage;

            if (part.CanBroken)
            {
                if(part.IsBroken == false)
                    part.ReduceDurability(calculateDamage);
            }
            
            if (CurrentHp <= 0) CurrentHp = 0;
            
            if (IsDead)
            {
                Dead();
                return;
            }
            
            Debug.Log($"{gameObject.name} hit! HP: {CurrentHp}");
        }

        private void Dead()
        {
            Debug.Log(gameObject.name + " is dead");
        }
    }
}