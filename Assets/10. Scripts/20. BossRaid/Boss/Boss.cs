using System;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{
    public class Boss : MonoBehaviour
    {
        private static readonly int DieHash = Animator.StringToHash("Die");
        [SerializeField] private float maxHp = 100;
        [SerializeField] private float maxFatigue = 30;
        [SerializeField] private float exhaustTime = 3f;
        public float CurrentHp { get; private set; }
        public float CurrentFatigue { get; private set; }
        public bool IsExhausted { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);
        private float currentExhaustedTime = 0f;
        
        private Animator animator;
        private Hurtbox[] hurtboxes;

        public event Action OnDead;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            hurtboxes = GetComponentsInChildren<Hurtbox>();
            CurrentHp = maxHp;
            CurrentFatigue = maxFatigue;
        }

        private void Update()
        {
            if (IsExhausted)
            {
                currentExhaustedTime += Time.deltaTime;
                if (currentExhaustedTime >= exhaustTime)
                {
                    HealFatigue();
                    return;
                }
            }
        }
        
        
        public void ApplyHit(AttackInfo info, BossPart part)
        {
            if (IsDead) return;

            float calculateDamage = info.Damage * part.CurrentMagnitude;
            
            CurrentHp -= calculateDamage;
            if (IsExhausted == false) CurrentFatigue -= info.Fatigue; 

            if (part.CanBroken)
            {
                if(part.IsBroken == false)
                    part.ReduceDurability(calculateDamage);
            }
            
            if (CurrentHp <= 0) CurrentHp = 0;
            if(CurrentFatigue <= 0) CurrentFatigue = 0;
            
            if (IsDead)
            {
                Dead();
                return;
            }
            
            if (CurrentFatigue <= 0 && IsExhausted == false)
            {
                Exhausted();
            }
            
            Debug.Log($"{gameObject.name} hit! HP: {CurrentHp}");
            Debug.Log($"{gameObject.name} hit! Fatigue: {CurrentFatigue}");
        }

        private void Exhausted()
        {
            IsExhausted = true;
            Debug.Log("Exhausted!");
        }

        private void HealFatigue()
        {
            IsExhausted = false;
            CurrentFatigue = maxFatigue;
            currentExhaustedTime = 0.0f;
            Debug.Log("피로도 회복");
        }
        
        private void Dead()
        {
            foreach(var hurtbox in hurtboxes) hurtbox.Disable();
            Debug.Log(gameObject.name + " is dead");
            animator.SetTrigger(DieHash);
            OnDead?.Invoke();
        }
    }
}