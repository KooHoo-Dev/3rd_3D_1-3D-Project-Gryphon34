using BossRaid.Combat;
using Unity.VisualScripting;
using UnityEngine;

namespace BossRaid.Boss
{
    public class Boss : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100;
        [SerializeField] private float maxStamina = 30;
        [SerializeField] private float exhaustTime = 3f;
        public float CurrentHp { get; private set; }
        public float CurrentStamina { get; private set; }
        public bool IsExhausted { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);
        private float currentExhaustedTime = 0f;

        private void Awake()
        {
            CurrentHp = maxHp;
            CurrentStamina = maxStamina;
        }

        private void Update()
        {
            if (IsExhausted)
            {
                currentExhaustedTime += Time.deltaTime;
                if (currentExhaustedTime >= exhaustTime)
                {
                    HealStamina();
                    return;
                }
            }
        }
        
        
        public void ApplyHit(AttackInfo info, BossPart part)
        {
            if (IsDead) return;

            float calculateDamage = info.Damage * part.CurrentMagnitude;
            
            CurrentHp -= calculateDamage;
            if (IsExhausted == false) CurrentStamina -= info.Stamina; 

            if (part.CanBroken)
            {
                if(part.IsBroken == false)
                    part.ReduceDurability(calculateDamage);
            }
            
            if (CurrentHp <= 0) CurrentHp = 0;
            if(CurrentStamina <= 0) CurrentStamina = 0;
            
            if (IsDead)
            {
                Dead();
                return;
            }
            
            if (CurrentStamina <= 0 && IsExhausted == false)
            {
                Exhausted();
            }
            
            Debug.Log($"{gameObject.name} hit! HP: {CurrentHp}");
            Debug.Log($"{gameObject.name} hit! Stamina: {CurrentStamina}");
        }

        private void Exhausted()
        {
            IsExhausted = true;
            Debug.Log("Exhausted!");
        }

        private void HealStamina()
        {
            IsExhausted = false;
            CurrentStamina = maxStamina;
            currentExhaustedTime = 0.0f;
            Debug.Log("스태미너 회복");
        }
        
        private void Dead()
        {
            Debug.Log(gameObject.name + " is dead");
        }
    }
}