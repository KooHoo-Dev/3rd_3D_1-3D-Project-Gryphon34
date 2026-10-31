using System;
using System.Collections.Generic;
using BossRaid.Boss.States;
using BossRaid.Combat;
using BossRaid.Player;
using DG.Tweening;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private float drinkReactRange = 20f;

        [Header("부위파괴 번쩍임 (셰이더 _FlashColor)")]
        [SerializeField, ColorUsage(false, true)] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.2f; // 갔다가 돌아오는 데 전체 시간

        [Header("꼬리 잘림")]
        [SerializeField] private Transform TailPosition; // 크기를 0으로 만들 뼈 (MountainDragon_ Tail5). 이 뼈부터 꼬리 끝까지 안 보임
        [SerializeField] private BossPart TailPart;      // 꼬리 부위 (MountainDragon_ Tail). IsBroken으로 잘렸는지 봄

        public float CurrentHp { get; private set; }
        public float MaxHp => maxHp;
        public float CurrentFatigue { get; private set; }
        public bool IsExhausted { get; private set; }
        public bool IsDiscovered { get; private set; }
        public bool IsAIPaused { get; private set; } // 임시: P 키. 켜져 있으면 Idle에서 Decide로 안 감
        public PlayerController Player { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);
        private float currentExhaustedTime = 0f;
        
        public Animator Animator { get; private set; }
        private Hurtbox[] hurtboxes;
        private AnimStateEventListener listener;
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private Material bodyMaterial; // renderer.material = 이 용만 쓰는 복사본 (에셋은 안 바뀜)

        public BossStates CurrentState { get; private set; }
        private readonly Dictionary<BossStates.StateName, BossStates> statesDic = new Dictionary<BossStates.StateName, BossStates>();
        
        public event Action OnDeath;
        public event Action<BossPart> OnPartBroken; // 부위가 깨진 순간 (한 판에 부위마다 한 번)
        public event Action OnHpReduce; // 맞아서 HP가 깎인 순간 (체력 구간은 듣는 쪽이 CurrentHp / MaxHp로 계산)

        private void Awake()
        {
            Animator = GetComponent<Animator>();
            listener = GetComponent<AnimStateEventListener>();
            hurtboxes = GetComponentsInChildren<Hurtbox>();
            bodyMaterial = GetComponentInChildren<SkinnedMeshRenderer>().material;
            CurrentHp = maxHp;
            CurrentFatigue = maxFatigue;
        }

        private void OnDestroy()
        {
            bodyMaterial.DOKill();
            Destroy(bodyMaterial); // .material로 만든 복사본은 직접 지워야 함
        }

        // 꼬리가 잘렸으면 매 프레임 TailPosition 크기 0. Animator가 Update 뒤에 뼈를 애니메이션 값(1)으로 덮어써서 LateUpdate에서 함
        private void LateUpdate()
        {
            if (TailPart == null || TailPosition == null) return;
            if (TailPart.IsBroken) TailPosition.localScale = Vector3.zero;
        }

        // 부위파괴 순간 몸 전체가 잠깐 번쩍임. 연달아 불리면 이전 번쩍임을 끊고 검정(효과 없음)에서 다시 시작
        private void HitEffect()
        {
            bodyMaterial.DOKill();
            bodyMaterial.SetColor(FlashColorId, Color.black);
            bodyMaterial.DOColor(flashColor, FlashColorId, flashDuration * 0.5f).SetLoops(2, LoopType.Yoyo);
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
            // 임시: 브레스 테스트용. B 키로 바로 브레스. 브레스를 패턴 목록에 넣은 뒤 지웁니다
            if (Keyboard.current.bKey.wasPressedThisFrame) ChangeState(BossStates.StateName.BreathState);
            // 임시: 불덩이 테스트용. F 키
            if (Keyboard.current.fKey.wasPressedThisFrame) ChangeState(BossStates.StateName.FireBallState);
            // 임시: 철산고 테스트용. H 키
            if (Keyboard.current.hKey.wasPressedThisFrame) ChangeState(BossStates.StateName.HipCheckState);
            // 임시: 점프 물기 사거리 재기용. P 키 = AI 멈춤 켜기/끄기, J 키 = 점프 물기
            if (Keyboard.current.pKey.wasPressedThisFrame) IsAIPaused = !IsAIPaused;
            if (Keyboard.current.jKey.wasPressedThisFrame) ChangeState(BossStates.StateName.JumpBiteState);
            // 임시: 처치 보상 테스트용. K 키 = 바로 처치 (HP 0 → Dead → OnDeath → HuntManager 보상)
            if (Keyboard.current.kKey.wasPressedThisFrame && IsDead == false)
            {
                CurrentHp = 0;
                Dead();
            }
            // 임시: 부위 소재 테스트용. L 키 = 깰 수 있는 부위 중 아직 안 깨진 것 하나를 바로 깸 (HP는 안 깎음, 알림은 ApplyHit과 같음)
            if (Keyboard.current.lKey.wasPressedThisFrame && IsDead == false)
            {
                foreach (BossPart part in GetComponentsInChildren<BossPart>())
                {
                    if (part.CanBroken == false || part.IsBroken) continue;
                    if (part.ReduceDurability(part.CurrentDurability))
                    {
                        OnPartBroken?.Invoke(part);
                        HitEffect();
                        if (part.PartName == "Tail" && TailPosition != null)
                        {
                            foreach (Collider tailCollider in TailPosition.GetComponentsInChildren<Collider>()) tailCollider.enabled = false;
                        }
                    }
                    break;
                }
            }

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
                if (part.IsBroken == false)
                {
                    bool broke = part.ReduceDurability(calculateDamage);
                    if (broke)
                    {
                        OnPartBroken?.Invoke(part); // 소재 등은 듣는 쪽(HuntManager)이 처리
                        HitEffect();
                        // 꼬리: 안 보이게 될 부분의 콜라이더를 한 번 꺼 둠 (크기 0만으로는 Animator 타이밍에 따라 맞을 수 있어서)
                        if (part.PartName == "Tail" && TailPosition != null)
                        {
                            foreach (Collider tailCollider in TailPosition.GetComponentsInChildren<Collider>()) tailCollider.enabled = false;
                        }
                    }
                }
            }
            
            if (CurrentHp <= 0) CurrentHp = 0;
            if(CurrentFatigue <= 0) CurrentFatigue = 0;

            OnHpReduce?.Invoke(); // Dead()보다 먼저 → 마지막 한 방으로 넘은 구간도 처치 전에 들어감

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
            if (Player != null) Player.Potion.OnDrinkStarted -= HandleDrinkStarted;
            Player = player;
            Player.Potion.OnDrinkStarted += HandleDrinkStarted;
        }
        
        private void HandleDrinkStarted()
        {
            if (IsDead || IsDiscovered == false || IsAIPaused) return;

            BossStates.StateName current = CurrentState.Name;
            if (current != BossStates.StateName.IdleState && current != BossStates.StateName.ChaseState) return;
            if (transform.FlatDistanceTo(Player.transform) > drinkReactRange) return;

            ChangeState(BossStates.StateName.BreathState);
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
            if (Player != null) Player.Potion.OnDrinkStarted -= HandleDrinkStarted;
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
            GUI.Label(new Rect(Screen.width * 0.5f - 350f, 20f, 700f, 40f), $"Boss: {stateName} / 거리: {distanceText} / 각도: {angleText}{(IsAIPaused ? " / AI 멈춤" : "")}", style);
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