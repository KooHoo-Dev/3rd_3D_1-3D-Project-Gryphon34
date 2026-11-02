using System;
using System.Collections.Generic;
using BossRaid.Boss.States;
using BossRaid.Combat;
using BossRaid.Player;
using DG.Tweening;
using Study.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BossRaid.Boss
{
    public class Boss : MonoBehaviour
    {
        [SerializeField] private StageValue[] stages; // 0번 = 1단계. HuntManager가 SetStage로 고름
        [SerializeField] private float exhaustTime = 3f; // 지쳤을 때 대기(IdleState)에서 쉬는 시간. 대기에 들어온 순간부터 셈
        [SerializeField] private float drinkReactRange = 20f;

        [Header("소리 (크기·피치·2D/3D는 AudioSource에서)")]
        [SerializeField] private AudioSource audioSource;

        [Header("지침 알림 (비어 있으면 안 함)")]
        [SerializeField] private ParticleSystem exhaustedEffect; // 지친 동안 켜 둠
        [SerializeField] private AudioClip exhaustedSound;       // 지친 순간 한 번

        [Header("부위파괴 번쩍임 (셰이더 _FlashColor)")]
        [SerializeField, ColorUsage(false, true)] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.2f; // 갔다가 돌아오는 데 전체 시간

        [Header("꼬리 잘림")]
        [SerializeField] private Transform TailPosition; // 크기를 0으로 만들 뼈 (MountainDragon_ Tail5). 이 뼈부터 꼬리 끝까지 안 보임
        [SerializeField] private BossPart TailPart;      // 꼬리 부위 (MountainDragon_ Tail). IsBroken으로 잘렸는지 봄

        [Header("날개 (전부 깨지면 needWings 패턴 안 나옴)")]
        [SerializeField] private BossPart[] wingParts; // Wing_L, Wing_R

        [Header("근접 불 (단계 rageFireHpRate 아래, 비어 있으면 이펙트 안 함)")]
        [SerializeField] private ParticleSystem[] rageFireEffects; // 넘는 순간 켜서 계속 켜 둠 (양손·입 등)

        public float CurrentHp { get; private set; }
        public StageValue Stage { get; private set; } // 지금 단계의 값
        public int StageNumber { get; private set; }  // 지금 단계 (1부터). DecideState가 패턴의 fromStage와 비교
        public float MaxHp => Stage.MaxHp;
        public float AttackRate => Stage.AttackRate;
        public float CurrentFatigue { get; private set; }
        public bool IsExhausted { get; private set; }
        public bool IsRageFire { get; private set; } // 근접(rageFire 히트박스)에도 불이 붙음

        // 날개가 하나라도 안 깨졌으면 날 수 있음 (wingParts가 비어 있어도 true). DecideState가 needWings 줄을 거를 때 봄
        public bool CanFly
        {
            get
            {
                if (wingParts == null || wingParts.Length == 0) return true;
                foreach (BossPart wing in wingParts)
                {
                    if (wing != null && wing.IsBroken == false) return true;
                }
                return false;
            }
        }
        public float ExhaustTime => exhaustTime;
        public bool IsDiscovered { get; private set; }
        public bool IsAIPaused { get; private set; } // 임시: P 키. 켜져 있으면 Idle에서 Decide로 안 감
        public PlayerController Player { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);

        public Animator Animator { get; private set; }
        private Hurtbox[] hurtboxes;
        private AnimStateEventListener listener;
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private SkinnedMeshRenderer bodyRenderer;
        private Material originalMaterial; // 프리팹 머티리얼 (단계에 머티리얼이 없으면 이걸로)
        private Material bodyMaterial; // renderer.material = 이 용만 쓰는 복사본 (에셋은 안 바뀜)
        private bool isRageRoarReserved; // 근접 불이 켜진 뒤 다음 대기에서 한 번 포효

        public BossStates CurrentState { get; private set; }
        public BossStates PreviousState { get; private set; } // 바로 전 상태 (IdleState가 공격 뒤 대기 시간을 읽음)
        private readonly Dictionary<BossStates.StateName, BossStates> statesDic = new Dictionary<BossStates.StateName, BossStates>();
        
        public event Action OnDeath;
        public event Action<BossPart> OnPartBroken; // 부위가 깨진 순간 (한 판에 부위마다 한 번)
        public event Action OnHpReduce; // 맞아서 HP가 깎인 순간 (체력 구간은 듣는 쪽이 CurrentHp / MaxHp로 계산)
        public event Action OnDiscovered; // 플레이어를 처음 발견한 순간 (한 판에 한 번). MusicPlayer가 전투 음악으로 바꿈

        private void Awake()
        {
            Animator = GetComponent<Animator>();
            listener = GetComponent<AnimStateEventListener>();
            hurtboxes = GetComponentsInChildren<Hurtbox>();
            bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            originalMaterial = bodyRenderer.sharedMaterial;
            SetStage(1); // HuntManager가 없는 씬에서도 1단계로 돌게
        }

        private void OnDestroy()
        {
            bodyMaterial.DOKill();
            Destroy(bodyMaterial); // .material로 만든 복사본은 직접 지워야 함
        }

        // 사냥할 단계(1부터)의 HP·피로·공격력 배율·머티리얼로 바꿈. 투기장 시작 때 HuntManager가 부름
        public void SetStage(int stage)
        {
            int index = Mathf.Clamp(stage - 1, 0, stages.Length - 1);
            if (index != stage - 1) Debug.LogWarning($"[보스] {stage}단계 값이 없어서 {index + 1}단계로 합니다");
            Stage = stages[index];
            StageNumber = index + 1;

            CurrentHp = Stage.MaxHp;
            CurrentFatigue = Stage.MaxFatigue;
            IsRageFire = false;
            isRageRoarReserved = false;

            // 머티리얼을 바꾸고 번쩍임용 복사본을 다시 만듦 (이전 복사본은 지움)
            if (bodyMaterial != null)
            {
                bodyMaterial.DOKill();
                Destroy(bodyMaterial);
            }
            bodyRenderer.sharedMaterial = Stage.BodyMaterial != null ? Stage.BodyMaterial : originalMaterial;
            bodyMaterial = bodyRenderer.material;
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

        public void ApplyHit(AttackInfo info, BossPart part)
        {
            if (IsDead) return;

            float calculateDamage = info.Damage * part.CurrentMagnitude;
            if (part.IsBroken) calculateDamage *= info.BrokenPartBonus; // 용뿔 대검: 깨진 부위에 더 아픔
            
            CurrentHp -= calculateDamage;
            if (IsExhausted == false) CurrentFatigue -= info.Fatigue; 

            if (part.CanBreakAt(StageNumber)) // 날개처럼 2단계부터 깨지는 부위는 1단계에선 내구도가 안 깎임
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

            if (IsRageFire == false && Stage.RageFireHpRate > 0 && CurrentHp <= MaxHp * Stage.RageFireHpRate) StartRageFire();

            if (CurrentFatigue <= 0 && IsExhausted == false)
            {
                Exhausted();
            }
            
            Debug.Log($"{gameObject.name} hit! HP: {CurrentHp}");
            Debug.Log($"{gameObject.name} hit! Fatigue: {CurrentFatigue}{(IsExhausted ? " (지침 중 → 안 깎음, 쉬고 나면 회복)" : "")}");
        }

        // HP가 단계 비율 아래로 처음 내려감: 근접에도 불, 이펙트 켬, 다음 대기에서 포효 (하던 공격은 안 끊음)
        private void StartRageFire()
        {
            IsRageFire = true;
            isRageRoarReserved = true;
            if (rageFireEffects != null)
                foreach (ParticleSystem effect in rageFireEffects) effect.Play();
            Debug.Log($"[보스] HP {Stage.RageFireHpRate * 100}% 아래 → 근접에 불");
        }

        // IdleState가 Decide로 가기 전에 부름. 예약된 포효가 있으면 true (한 번만)
        public bool ConsumeRageRoar()
        {
            if (isRageRoarReserved == false) return false;
            isRageRoarReserved = false;
            return true;
        }

        public void SetPlayer(PlayerController player)
        {
            if (Player != null) Player.Potion.OnDrinkStarted -= HandleDrinkStarted;
            Player = player;
            Player.Potion.OnDrinkStarted += HandleDrinkStarted;
        }
        
        private void HandleDrinkStarted()
        {
            if (IsDead || IsDiscovered == false || IsAIPaused || IsExhausted) return; // 지친 중엔 반응 안 함 (대기가 다시 시작돼서 쉬는 시간이 늘어남)

            BossStates.StateName current = CurrentState.Name;
            if (current != BossStates.StateName.IdleState && current != BossStates.StateName.ChaseState) return;
            if (transform.FlatDistanceTo(Player.transform) > drinkReactRange) return;

            // 대기를 끊고 바로 공격 고르기 → 그 자리에 맞는 패턴 (없으면 Decide가 Chase로)
            ChangeState(BossStates.StateName.DecideState);
        }

        public void SetDiscover()
        {
            if (IsDiscovered) return; // IdleState가 범위 안에서 매 프레임 부름 → 처음 한 번만
            IsDiscovered = true;
            OnDiscovered?.Invoke();
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
            PreviousState = CurrentState;
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

        // 지침: 공격 중이어도 끊고 바로 IdleState
        // 쉬는 시간은 IdleState가 들어온 순간부터 exhaustTime만큼 세고, 다 세면 HealFatigue를 부름
        private void Exhausted()
        {
            IsExhausted = true;
            if (exhaustedEffect != null) exhaustedEffect.Play();
            if (exhaustedSound != null && audioSource != null) audioSource.PlayOneShot(exhaustedSound);
            Debug.Log("Exhausted!");

            // 쓰러지는 애니메이션(Exhausted 트리거)은 IdleState.Enter가 냄
            ChangeState(BossStates.StateName.IdleState);
        }

        // 퍼펙트 가드(상쇄도 썼음): 피로만 깎음 (HP·부위는 안 깎음). 0이면 공격을 끊고 쓰러짐
        public void ReduceFatigue(float amount)
        {
            if (IsDead || IsExhausted) return;

            CurrentFatigue -= amount;
            if (CurrentFatigue <= 0) CurrentFatigue = 0;
            Debug.Log($"{gameObject.name} 피로 -{amount} → Fatigue: {CurrentFatigue}");

            if (CurrentFatigue <= 0) Exhausted();
        }

        public void HealFatigue()
        {
            IsExhausted = false;
            CurrentFatigue = Stage.MaxFatigue;
            if (exhaustedEffect != null) exhaustedEffect.Stop();
            Debug.Log($"피로도 회복 → Fatigue: {CurrentFatigue}");
        }
        
        private void Dead()
        {
            foreach(var hurtbox in hurtboxes) hurtbox.Disable();
            Debug.Log(gameObject.name + " is dead");
            ChangeState(BossStates.StateName.DeathState);
            OnDeath?.Invoke();
        }
        
        #region GUI
        // 임시: 패턴마다 기즈모 색 (왼쪽·오른쪽 줄은 같은 색)
        private static Color PatternColor(BossStates.StateName name)
        {
            switch (name)
            {
                case BossStates.StateName.ClawAttackState: return Color.red;
                case BossStates.StateName.ClawComboState: return new Color(1f, 0.5f, 0f);
                case BossStates.StateName.BiteState: return Color.yellow;
                case BossStates.StateName.JumpBiteState: return Color.green;
                case BossStates.StateName.BreathState: return Color.cyan;
                case BossStates.StateName.FireBallState: return new Color(0.3f, 0.5f, 1f);
                case BossStates.StateName.HipCheckState: return Color.magenta;
                case BossStates.StateName.TurnClawState: return Color.white;
                case BossStates.StateName.TurnLeftState:
                case BossStates.StateName.TurnRightState: return Color.gray;
                default: return Color.black;
            }
        }

        #endregion
#if UNITY_EDITOR
        // 임시: DecideState의 패턴 줄마다 거리·각도 범위를 바닥에 부채꼴로 항상 그립니다 (색 = PatternColor)
        // 용을 안 골라도 보임. Game 창은 위쪽 Gizmos 버튼을 켜면 보임 (이름 글자는 Scene 창에만, Game 창은 왼쪽 위 색 안내)
        private void OnDrawGizmos()
        {
            var decide = GetComponentInChildren<DecideState>(true);
            if (decide == null) return;

            foreach (var pattern in decide.AllPatterns)
            {
                Gizmos.color = PatternColor(pattern.PatternName);
                DrawGroundSector(pattern.MinDistance, pattern.MaxDistance, pattern.MinAngle, pattern.MaxAngle);

                float midAngle = (pattern.MinAngle + pattern.MaxAngle) / 2f;
                Handles.color = Gizmos.color;
                Handles.Label(transform.position + GroundDirection(midAngle) * pattern.MaxDistance, pattern.PatternName.ToString());
            }
        }

        // 최소·최대 거리 호 두 개 + 양 끝 각도 선 두 개
        private void DrawGroundSector(float minDistance, float maxDistance, float minAngle, float maxAngle)
        {
            int segments = Mathf.Max(2, Mathf.CeilToInt((maxAngle - minAngle) / 5f));
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.Lerp(minAngle, maxAngle, i / (float)segments);
                float a1 = Mathf.Lerp(minAngle, maxAngle, (i + 1) / (float)segments);
                Gizmos.DrawLine(transform.position + GroundDirection(a0) * minDistance, transform.position + GroundDirection(a1) * minDistance);
                Gizmos.DrawLine(transform.position + GroundDirection(a0) * maxDistance, transform.position + GroundDirection(a1) * maxDistance);
            }
            Gizmos.DrawLine(transform.position + GroundDirection(minAngle) * minDistance, transform.position + GroundDirection(minAngle) * maxDistance);
            Gizmos.DrawLine(transform.position + GroundDirection(maxAngle) * minDistance, transform.position + GroundDirection(maxAngle) * maxDistance);
        }

        // Decide와 같은 각도 (앞 0°, 오른쪽 +)를 바닥 방향으로
        private Vector3 GroundDirection(float angle)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return Quaternion.AngleAxis(angle, Vector3.up) * forward.normalized;
        }
#endif
    }
}