using System;
using System.Collections.Generic;
using BossRaid.Boss.States;
using BossRaid.Combat;
using BossRaid.Player;
using Study.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public bool IsDiscovered { get; private set; }
        public PlayerController Player { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);
        private float currentExhaustedTime = 0f;
        
        public Animator Animator { get; private set; }
        private Hurtbox[] hurtboxes;
        private AnimStateEventListener listener;
        
        public BossStates CurrentState { get; private set; }
        private readonly Dictionary<BossStates.StateName, BossStates> statesDic = new Dictionary<BossStates.StateName, BossStates>();
        
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
            BossStates[] states = GetComponentsInChildren<BossStates>();
            for (int i = 0; i < states.Length; i++)
            {
                statesDic.Add(states[i].Name, states[i]);
                states[i].Initialize(this);
                states[i].gameObject.SetActive(false);
            }
            
            ChangeState(BossStates.StateName.IdleState);
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

        public void SetPlayer(PlayerController player)
        {
            Player = player;
        }

        public void SetDiscover()
        {
            IsDiscovered = true;
        }

        public void ChangeState(BossStates.StateName stateName)
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
            ChangeState(BossStates.StateName.DeathState);
            OnDeath?.Invoke();
        }

        // 임시: 보스 AI 거리 테스트용. 패턴 거리 값을 정한 뒤 지웁니다
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            string stateName = CurrentState != null ? CurrentState.Name.ToString() : "-";
            string distanceText = Player != null
                ? transform.FlatDistanceTo(Player.transform).ToString("F2")
                : "Player 없음";
            string angleText = Player != null
                ? Vector3.SignedAngle(transform.forward, transform.FlatDirectionTo(Player.transform), Vector3.up).ToString("F0")
                : "-";
            GUI.Label(new Rect(Screen.width * 0.5f - 350f, 20f, 700f, 40f), $"Boss: {stateName} / 거리: {distanceText} / 각도: {angleText}", style);
        }

#if UNITY_EDITOR
        // 용 루트를 선택하면 DecideState의 패턴별 거리 범위를 바닥에 원으로 그립니다 (노랑 = 최소, 빨강 = 최대)
        private void OnDrawGizmosSelected()
        {
            var decide = GetComponentInChildren<DecideState>(true);
            if (decide == null || decide.Patterns == null) return;

            foreach (var pattern in decide.Patterns)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(transform.position, Vector3.up, pattern.MinDistance);
                Handles.color = Color.red;
                Handles.DrawWireDisc(transform.position, Vector3.up, pattern.MaxDistance);
                Handles.Label(transform.position + transform.forward * pattern.MaxDistance, pattern.PatternName.ToString());
            }
        }
#endif
    }
}