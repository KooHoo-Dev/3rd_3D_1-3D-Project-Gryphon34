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
        [SerializeField] private float exhaustTime = 3f; // 지쳤을 때 대기(IdleState)에서 쉬는 시간. 공격 중에 지치면 그 공격이 끝난 뒤부터 셈
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

        public float CurrentHp { get; private set; }
        public float MaxHp => maxHp;
        public float CurrentFatigue { get; private set; }
        public bool IsExhausted { get; private set; }
        public float ExhaustTime => exhaustTime;
        public bool IsDiscovered { get; private set; }
        public bool IsAIPaused { get; private set; } // 임시: P 키. 켜져 있으면 Idle에서 Decide로 안 감
        public PlayerController Player { get; private set; }

        private bool IsDead =>  (CurrentHp <= 0);

        public Animator Animator { get; private set; }
        private Hurtbox[] hurtboxes;
        private AnimStateEventListener listener;
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private Material bodyMaterial; // renderer.material = 이 용만 쓰는 복사본 (에셋은 안 바뀜)

        public BossStates CurrentState { get; private set; }
        public BossStates PreviousState { get; private set; } // 바로 전 상태 (IdleState가 공격 뒤 대기 시간을 읽음)
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
            // 임시: 지침 테스트용. N 키 = 피로 0 → 바로 지침 (ApplyHit에서 0이 될 때와 같음)
            if (Keyboard.current.nKey.wasPressedThisFrame && IsDead == false && IsExhausted == false)
            {
                CurrentFatigue = 0;
                Exhausted();
            }
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
            if (IsDead || IsDiscovered == false || IsAIPaused || IsExhausted) return; // 지친 중엔 반응 안 함 (대기가 다시 시작돼서 쉬는 시간이 늘어남)

            BossStates.StateName current = CurrentState.Name;
            if (current != BossStates.StateName.IdleState && current != BossStates.StateName.ChaseState) return;
            if (transform.FlatDistanceTo(Player.transform) > drinkReactRange) return;

            // 대기를 끊고 바로 공격 고르기 → 그 자리에 맞는 패턴 (없으면 Decide가 Chase로)
            ChangeState(BossStates.StateName.DecideState);
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

        // 지침: 공격 중이면 그 공격만 끝까지 (끝나면 OnAnimationEnd → IdleState), 아니면 바로 IdleState
        // 쉬는 시간은 IdleState가 들어온 순간부터 exhaustTime만큼 세고, 다 세면 HealFatigue를 부름
        private void Exhausted()
        {
            IsExhausted = true;
            if (exhaustedEffect != null) exhaustedEffect.Play();
            if (exhaustedSound != null && audioSource != null) audioSource.PlayOneShot(exhaustedSound);
            Debug.Log("Exhausted!");

            // 쓰러지는 애니메이션(Exhausted 트리거)은 IdleState.Enter가 냄
            if (CurrentState is not Attack) ChangeState(BossStates.StateName.IdleState);
        }

        public void HealFatigue()
        {
            IsExhausted = false;
            CurrentFatigue = maxFatigue;
            if (exhaustedEffect != null) exhaustedEffect.Stop();
            Debug.Log("피로도 회복");
        }
        
        private void Dead()
        {
            foreach(var hurtbox in hurtboxes) hurtbox.Disable();
            Debug.Log(gameObject.name + " is dead");
            ChangeState(BossStates.StateName.DeathState);
            OnDeath?.Invoke();
        }
        
        #region GUI
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
            GUI.Label(new Rect(Screen.width * 0.5f - 350f, 20f, 700f, 40f), $"Boss: {stateName} / 거리: {distanceText} / 각도: {angleText}{(IsAIPaused ? " / AI 멈춤" : "")}{(IsExhausted ? " / 지침" : "")}", style);

            // 임시: 기즈모 부채꼴 색 안내 (Game 창에서 어느 색이 무슨 패턴인지)
            var decide = GetComponentInChildren<DecideState>(true);
            if (decide == null || decide.Patterns == null) return;
            var legendStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            var shown = new HashSet<BossStates.StateName>();
            float y = 100f;
            foreach (var pattern in decide.Patterns)
            {
                if (shown.Add(pattern.PatternName) == false) continue;
                legendStyle.normal.textColor = PatternColor(pattern.PatternName);
                GUI.Label(new Rect(20f, y, 300f, 26f), pattern.PatternName.ToString(), legendStyle);
                y += 24f;
            }
        }

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
            if (decide == null || decide.Patterns == null) return;

            foreach (var pattern in decide.Patterns)
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