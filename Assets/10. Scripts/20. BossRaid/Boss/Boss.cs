using System;
using System.Collections.Generic;
using BossRaid.Boss.States;
using BossRaid.Combat;
using Study3D.FpsGame;
using UnityEngine;

namespace BossRaid.Boss
{
    public class Boss : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100;
        [SerializeField] private float maxFatigue = 30;
        [SerializeField] private float exhaustTime = 3f;
        public float CurrentHp { get; private set; }
        public float CurrentFatigue { get; private set; }
        public bool IsExhausted { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);
        private float currentExhaustedTime = 0f;
        
        public Animator Animator { get; private set; }
        private Hurtbox[] hurtboxes;
        private AnimStateEventListener listener;
        
        public BossState CurrentState { get; private set; }
        private readonly Dictionary<BossState.StateName, BossState> statesDic = new Dictionary<BossState.StateName, BossState>();
        
        public event Action OnDeath;

        private void Awake()
        {
            Animator = GetComponent<Animator>();
            listener = GetComponent<AnimStateEventListener>();
            hurtboxes = GetComponentsInChildren<Hurtbox>();
            CurrentHp = maxHp;
            CurrentFatigue = maxFatigue;
        }

        private void Start()
        {
            BossState[] states = GetComponentsInChildren<BossState>();
            for (int i = 0; i < states.Length; i++)
            {
                statesDic.Add(states[i].Name, states[i]);
                states[i].Initialize(this);
                states[i].gameObject.SetActive(false);
            }
            
            ChangeState(BossState.StateName.IdleState);
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

        public void ChangeState(BossState.StateName stateName)
        {
            if (CurrentState != null)
            {
                var prev = CurrentState;
                prev.Exit();
                prev.gameObject.SetActive(false);
            }

            var target = statesDic[stateName];
            CurrentState = target;
            
            if(target.AnimatorTrigger != null) Animator.SetTrigger(target.AnimatorTrigger);
            
            target.gameObject.SetActive(true);
            target.Enter();
        }

        private void OnEnable()
        {
            listener.OnOccursAnimStateEvent += HandleAnimEvent;
        }

        private void OnDisable()
        {
            listener.OnOccursAnimStateEvent -= HandleAnimEvent;
        }

        private void HandleAnimEvent(string eventName, string parameter)
        {
            CurrentState.ReceiveAnimEvent(eventName, parameter);
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
            ChangeState(BossState.StateName.DeathState);
            OnDeath?.Invoke();
        }
    }
}