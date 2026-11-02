using System.Collections.Generic;
using BossRaid.Combat;
using BossRaid.Player.States;
using BossRaid.Weapon;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Player
{
    [RequireComponent(typeof(ThirdPersonController))]
    public class PlayerController : MonoBehaviour
    {
        // 애니메이터 상태 Tag. PlayerGreatSword.controller의 상태 Tag와 같아야 합니다.
        private const string TagLocomotion = "Locomotion";
        private const string TagStart = "Start";
        private const string TagCharge = "Charge";
        private const string TagAttack = "Attack";
        private const string TagEnd = "End";
        private const string TagDodge = "Dodge";
        private const string TagExhausted = "Exhausted";
        private const string TagDrink = "Drink";
        private const string TagHit = "Hit";
        private const string TagDown = "Down";
        private const string TagCounter = "Counter";
        private const string TagGuard = "Guard";
        private const string TagTackle = "Tackle";

        // 애니메이터 파라미터 해시. 상태 클래스들도 이 값을 씁니다.
        public static readonly int SpeedHash = Animator.StringToHash("Speed");
        public static readonly int IsAttackHash = Animator.StringToHash("IsAttack");
        public static readonly int IsChargingHash = Animator.StringToHash("IsCharging");
        public static readonly int DodgeHash = Animator.StringToHash("Dodge");
        public static readonly int IsExhaustedHash = Animator.StringToHash("IsExhausted");
        public static readonly int DrinkHash = Animator.StringToHash("Drink");
        public static readonly int StandUpHash = Animator.StringToHash("StandUp");
        public static readonly int CounterHash = Animator.StringToHash("Counter");
        public static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");
        public static readonly int GuardHitHash = Animator.StringToHash("GuardHit");
        public static readonly int TackleHash = Animator.StringToHash("Tackle");
        public static readonly int ComboHash = Animator.StringToHash("Combo");
        
        [Header("Settings")]
        [SerializeField] private float speedDampTime = 0.05f; // Speed 값이 목표치까지 부드럽게 따라가는 시간

        [Header("Stamina")] 
        [SerializeField] private float dodgeStamina = 20f;
        [SerializeField] private float attackStamina = 25f;
        [SerializeField] private float runStamina = 10f;
        public float DodgeStamina => dodgeStamina;
        public float AttackStamina => attackStamina;
        public float RunStamina => runStamina; // 달리기 스태미너, 초당

        [Header("Guard")]
        [SerializeField] private float guardAngle = 55f;            // 정면 ± 이 각도 안에서 온 공격만 막음
        [SerializeField] private float guardDamageReduce = 0.8f;    // 막으면 받는 피해 감소 (0.8 = 80% 막음)
        [SerializeField] private float guardStaminaRate = 1f;       // 막은 피해 1당 쓰는 스태미나
        [SerializeField] private float fireGuardStaminaRate = 1.5f; // 화염 공격을 막으면 스태미나에 이만큼 더 곱함
        [SerializeField] private float guardTurnSpeed = 30f;        // 가드 중 이동 입력 방향으로 도는 속도 (초당 각도)
        [SerializeField] private float guardPushDistance = 1.5f;    // 막았을 때 보스 반대쪽으로 밀리는 거리
        [SerializeField] private float guardPushTime = 1f;          // 그 거리를 밀리는 시간
        [SerializeField] private float perfectGuardTime = 0.4f;     // Ctrl을 누르고 이 시간 안에 막으면 퍼펙트 가드 (안 밀림)
        [SerializeField] private float perfectGuardHitStop = 0.2f;  // 퍼펙트 가드 히트스탑
        [SerializeField] private int perfectGuardBonusLevel = 1;    // 퍼펙트 가드 뒤 다음 한 타의 차지를 이 단계부터 셈
        [SerializeField] private float perfectGuardFatigue;         // 퍼펙트 가드 때 보스 피로를 깎는 양 (상쇄 대신, 11/02). 0이면 안 깎음
        public float GuardTurnSpeed => guardTurnSpeed;

        [Header("Attack Cancel")]
        [SerializeField] private float attackCancelTime = 0f; // _End 칸 진행률이 이 값 이상이면 구르기·가드로 후딜을 끊을 수 있음 (0 = 들어가자마자)
        public float AttackCancelTime => attackCancelTime;
        public bool IsInGuardState => currentState is GuardState; // 가드 칸에 있는지 (GuardHandIK가 읽음). 애니메이터 파라미터 IsGuarding과 다름

        private int comboBonusLevel; // 퍼펙트 가드 성공 → 다음 한 타의 차지 시작 단계 (0 = 없음). 구르거나 맞으면 사라짐

        private float guardPressTime = float.NegativeInfinity; // Ctrl을 누른 시각 (퍼펙트 가드 판정). 누를 때마다 다시 셈
        private Vector3 pushVelocity;  // 막았을 때 밀리는 속도. GuardState가 매 프레임 옮김
        private float pushEndTime;     // 이 시각까지 밀림


        public Animator Animator { get; private set; }
        public ThirdPersonController Movement { get; private set; }
        public GreatSword Sword { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerStamina Stamina { get; private set; }
        public PlayerPotion Potion { get; private set; }
        public PlayerLoadout Loadout { get; private set; } // 장비·요리 (대장간·냄비가 Apply를 부름)
        public PlayerBurn Burn { get; private set; } // 화염 상태


        private readonly Dictionary<int, PlayerStatesBase> stateDic = new Dictionary<int, PlayerStatesBase>();
        private PlayerStatesBase defaultState; // Tag가 등록되지 않은 상태일 때 쓰는 기본 상태
        private PlayerStatesBase currentState;
        private PlayerStatesBase dodgeState;

        private bool isRunLocked; // 지친 뒤 Shift를 뗄 때까지 달리기를 막습니다

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Movement = GetComponent<ThirdPersonController>();
            Sword = GetComponentInChildren<GreatSword>();
            Health = GetComponent<PlayerHealth>();
            Stamina = GetComponent<PlayerStamina>();
            Potion = GetComponent<PlayerPotion>();
            Loadout = GetComponent<PlayerLoadout>();
            Burn = GetComponent<PlayerBurn>();

            Animator.applyRootMotion = false;

            defaultState = new LocomotionState(this);
            stateDic.Add(Animator.StringToHash(TagLocomotion), defaultState);

            dodgeState = new DodgeState(this);
            stateDic.Add(Animator.StringToHash(TagDodge), dodgeState);
            
            PlayerStatesBase chargeState = new ChargeState(this);
            stateDic.Add(Animator.StringToHash(TagStart), chargeState);
            stateDic.Add(Animator.StringToHash(TagCharge), chargeState);
            stateDic.Add(Animator.StringToHash(TagAttack), new AttackState(this));
            stateDic.Add(Animator.StringToHash(TagEnd), new AttackEndState(this));
            stateDic.Add(Animator.StringToHash(TagExhausted), new ExhaustedState(this));
            stateDic.Add(Animator.StringToHash(TagDrink), new DrinkState(this));
            stateDic.Add(Animator.StringToHash(TagHit), new HitState(this));
            stateDic.Add(Animator.StringToHash(TagDown), new KnockDownState(this));
            stateDic.Add(Animator.StringToHash(TagGuard), new GuardState(this));
            // 상쇄·태클은 뺌 (11/02). 다시 쓰려면 RegisterCounterTackleStates(chargeState)
        }

        private void Update()
        {
            UpdateAnimState();
            if (currentState.BlocksStaminaRegen)
                Stamina.ResetWaitTime();
            UpdateSpeedParameter();
        }

        private void UpdateAnimState()
        {
            // 전환(Transition) 중이면 넘어가고 있는 다음 상태를 기준으로 삼습니다.
            // 그래야 공격으로 넘어가기 시작한 순간부터 공격 상태 로직(이동 멈춤 등)이 실행됩니다.
            AnimatorStateInfo stateInfo = Animator.IsInTransition(0)
                ? Animator.GetNextAnimatorStateInfo(0)
                : Animator.GetCurrentAnimatorStateInfo(0);

            if (stateDic.TryGetValue(stateInfo.tagHash, out PlayerStatesBase nextState) == false)
            {
                nextState = defaultState;
            }

            if (currentState != nextState)
            {
                currentState?.Exit();
                currentState = nextState;
                currentState.Enter();
            }

            currentState.UpdateState(stateInfo);
        }

        // 입력이 아니라 실제 속도를 Blend 값으로 바꿉니다.
        // 0 ~ 걷기 속도 => 0 ~ 1, 걷기 ~ 달리기 속도 => 1 ~ 2
        // 상태와 상관없이 매 프레임 갱신해서, 공격으로 멈췄을 때도 Speed가 같이 0으로 내려갑니다.
        private void UpdateSpeedParameter()
        {
            float speed = Movement.CurrentSpeed;
            float blend = speed <= Movement.WalkSpeed
                ? Mathf.InverseLerp(0f, Movement.WalkSpeed, speed)
                : 1f + Mathf.InverseLerp(Movement.WalkSpeed, Movement.RunSpeed, speed);

            Animator.SetFloat(SpeedHash, blend, speedDampTime, Time.deltaTime);
        }

        // 아래부터는 상태 클래스들이 사용하는 공용 함수입니다.

        /// <summary>
        /// 이동 입력을 읽어서 ThirdPersonController에 넘겨줍니다. 이동할 수 있는 상태에서 매 프레임 호출합니다.
        /// </summary>
        public void HandleMovement()
        {
            bool isShiftHeld = SimpleInput.GetKey(Key.LeftShift);
            if (isShiftHeld == false) isRunLocked = false;

            bool hasRunInput = isShiftHeld && isRunLocked == false && (SimpleInput.GetMoveAxisRaw()) != Vector2.zero;
            if (hasRunInput && Stamina.TryUseStamina(runStamina * Time.deltaTime))
            {
                Movement.SetMoveInput(SimpleInput.GetMoveAxisRaw(), true);
            }
            else
            {
                Movement.SetMoveInput(SimpleInput.GetMoveAxisRaw(), false);
            }
        }

        /// <summary>
        /// Shift를 뗐다가 다시 누를 때까지 달리기를 막습니다. 지친 상태의 Enter에서 호출합니다.
        /// </summary>
        public void LockRun()
        {
            isRunLocked = true;
        }

        /// <summary>
        /// 이동을 멈춥니다. 공격처럼 제자리에 서야 하는 상태의 Enter에서 호출합니다.
        /// </summary>
        public void StopMovement(bool immediate = true)
        {
            Movement.StopMovement(immediate);
        }

        /// <summary>
        /// 퍼펙트 가드 성공. 다음 한 타의 차지가 level부터 셉니다. 이미 있으면 큰 쪽. TryGuard에서 호출합니다 (상쇄도 썼음).
        /// </summary>
        public void SetComboBonus(int level)
        {
            comboBonusLevel = Mathf.Max(comboBonusLevel, level);
        }

        /// <summary>
        /// 구르기·피격으로 콤보 보너스를 잃습니다.
        /// </summary>
        public void ClearComboBonus()
        {
            comboBonusLevel = 0;
            nextCombo = 0; // 태클로 기억한 다음 타도 (구르거나 맞으면 1타부터). 태클은 안 씀 (11/02)
        }

        /// <summary>
        /// 콤보 보너스가 있으면 쓰고(한 번만) 시작 단계를 돌려줍니다. 없으면 0. ChargeState.Enter에서 호출합니다.
        /// </summary>
        public int UseComboBonus()
        {
            int level = comboBonusLevel;
            comboBonusLevel = 0;
            return level;
        }

        /// <summary>
        /// Ctrl을 누른 순간. 여기서부터 perfectGuardTime 안에 막으면 퍼펙트 가드. LocomotionState에서 호출합니다.
        /// </summary>
        public void MarkGuardPress()
        {
            guardPressTime = Time.time;
        }

        /// <summary>
        /// 가드 자세에서 막을 수 있으면 막습니다 (PlayerHealth.TakeDamage에서 호출).
        /// 가드 중이 아님 / 가드 불가 / 앞쪽이 아님 / 스태미나가 모자람이면 false → 일반 피격.
        /// 막으면 damage를 막고 남은 피해로 바꾸고 스태미나·GuardHit 트리거·밀림. 퍼펙트면 안 밀리고 히트스탑·불꽃·콤보 보너스.
        /// </summary>
        public bool TryGuard(AttackInfo info, ref float damage)
        {
            if ((currentState is GuardState) == false) return false;
            if (info.Tag == AttackTag.Unguardable) return false;

            // 공격 온 쪽(보스 몸)이 정면 ± guardAngle 안인지 (내적 = 두 방향 사이 각도의 cos)
            Vector3 toSource = info.Source != null ? info.Source.position - transform.position : transform.forward;
            toSource.y = 0f;
            if (toSource.sqrMagnitude < 0.0001f) toSource = transform.forward; // 보스와 겹쳐 있으면 정면으로 침
            toSource.Normalize();
            float dot = Vector3.Dot(transform.forward, toSource);
            if (dot < Mathf.Cos(guardAngle * Mathf.Deg2Rad)) return false;

            // 막은 피해만큼 스태미나 (화염은 더). 방어구·요리·철 대검 감소를 곱한 GuardStaminaRate만큼만 씀
            float blocked = damage * guardDamageReduce;
            float loadoutRate = Loadout != null ? Loadout.GuardStaminaRate : 1f;
            float stamina = blocked * guardStaminaRate * (info.Tag == AttackTag.Fire ? fireGuardStaminaRate : 1f)
                            * loadoutRate;
            if (Stamina.CurrentStamina < stamina) return false; // 모자라면 못 막음 (일반 피격)
            Stamina.TryUseStamina(stamina);

            damage -= blocked;
            Animator.SetTrigger(GuardHitHash);

            bool isPerfect = Time.time - guardPressTime <= perfectGuardTime;
            if (isPerfect)
            {
                // 퍼펙트: 안 밀림 + 히트스탑(플레이어·보스) + 불꽃·금속음 + 다음 타 차지 보너스 + 보스 피로
                pushEndTime = 0f;
                Animator bossAnimator = info.Source != null ? info.Source.GetComponent<Animator>() : null;
                HitStop.Play(Animator, bossAnimator, perfectGuardHitStop);
                if (Sword != null) Sword.PlayPerfectGuardEffect(transform.position + toSource * 0.5f + Vector3.up);
                SetComboBonus(perfectGuardBonusLevel);

                // Source = 보스 몸 (Boss와 같은 오브젝트). 불덩이 폭발처럼 보스 밑이 아니면 null → 안 깎음
                // BossRaid.Player 안이라 그냥 Boss라고 쓰면 BossRaid.Boss 네임스페이스로 읽힘
                BossRaid.Boss.Boss boss = info.Source != null ? info.Source.GetComponent<BossRaid.Boss.Boss>() : null;
                float fatigue = perfectGuardFatigue + (Loadout != null ? Loadout.PerfectGuardFatigue : 0f); // + 요리 (꼬리 고기 스튜)
                if (boss != null && fatigue > 0f) boss.ReduceFatigue(fatigue);
            }
            else
            {
                pushVelocity = -toSource * (guardPushDistance / guardPushTime);
                pushEndTime = Time.time + guardPushTime;
                if (Sword != null) Sword.PlayGuardEffect(transform.position + toSource * 0.5f + Vector3.up); // 작은 불꽃 + 막는 소리
            }

            Debug.Log($"[Player] {(isPerfect ? "퍼펙트 " : "")}가드 (막음 {blocked}, 스태미나 -{stamina})");
            return true;
        }

        /// <summary>
        /// 막았을 때 밀리는 중이면 이번 프레임만큼 옮깁니다. GuardState에서 매 프레임 호출합니다.
        /// </summary>
        public void UpdateGuardPush()
        {
            if (Time.time >= pushEndTime) return;
            Movement.UpdateAnimMoveDelta(pushVelocity * Time.deltaTime);
        }

        /// <summary>
        /// 밀림을 멈춥니다. GuardState.Exit에서 호출합니다 (구르기·공격으로 나가면 더 안 밀림).
        /// </summary>
        public void StopGuardPush()
        {
            pushEndTime = 0f;
        }

        private void OnAnimatorMove()
        {
            // 루트 모션은 구르기와 태클만. 태클은 들어오는 전환(다음 칸)부터 나가는 전환(지금 칸)이 끝날 때까지
            bool isTackleMotion = Animator.GetCurrentAnimatorStateInfo(0).tagHash == TackleTagHash
                                  || (Animator.IsInTransition(0) && Animator.GetNextAnimatorStateInfo(0).tagHash == TackleTagHash);
            if (currentState != dodgeState && isTackleMotion == false) return;
            Movement.UpdateAnimMoveDelta(Animator.deltaPosition);
        }

        #region 안 쓰는 것 (상쇄·태클, 11/02 뺌)
        // 입력만 막음: LocomotionState·ChargeState의 #region 참고. 인스펙터 값은 그대로 남음

        [Header("Counter (안 씀)")]
        [SerializeField] private float counterStamina = 30f;
        [SerializeField] private float counterInputTime = 0.1f; // 대기에서 좌·우클릭을 이 시간 안에 함께 누르면 상쇄
        [SerializeField] private int counterBonusLevel = 1;     // 상쇄 성공 뒤 다음 한 타의 차지를 이 단계부터 셈
        public float CounterInputTime => counterInputTime;
        public int CounterBonusLevel => counterBonusLevel;

        [Header("Tackle (안 씀)")]
        [SerializeField] private float tackleStamina = 20f;
        [SerializeField] private float tackleDamageReduce = 0.5f;   // 태클 중 가벼운 공격의 피해 감소 (요리 TackleReduce를 더함)
        public float TackleStamina => tackleStamina;
        [SerializeField] private float tackleArmorEnd = 1f;         // 태클 칸 진행률 0 ~ 이 값까지 슈퍼아머 (뒤쪽 후딜은 일반 피격)
        public float TackleArmorEnd => tackleArmorEnd;
        [SerializeField] private float tackleComboTime = 1f;        // 태클 뒤 Locomotion으로 돌아와서 이 시간 안에 좌클릭하면 다음 타부터
        private int nextCombo;      // 태클로 넘어갈 다음 타 (2·3, 0 = 없음)
        private float nextComboEndTime;
        public int NextCombo => Time.time <= nextComboEndTime ? nextCombo : 0;
        public bool IsTackleArmor { get; set; } // 태클 슈퍼아머 중. ChargeState가 켜고 끔

        private static readonly int TackleTagHash = Animator.StringToHash(TagTackle);

        private float idleAttackTime = float.NegativeInfinity; // 대기에서 좌클릭으로 공격을 시작한 시각 (Start 칸에서 상쇄로 바꿀 수 있는지 봄)

        // Awake의 stateDic에 상쇄·태클 칸을 등록 (지금은 안 부름)
        private void RegisterCounterTackleStates(PlayerStatesBase chargeState)
        {
            stateDic.Add(Animator.StringToHash(TagCounter), new CounterState(this));
            stateDic.Add(Animator.StringToHash(TagTackle), chargeState); // 태클은 차지 중 동작이라 같은 상태 → 오가도 Enter/Exit가 안 불려서 차지가 이어짐
        }

        /// <summary>
        /// 대기에서 좌클릭으로 공격을 시작한 시각을 기억합니다. LocomotionState에서 호출합니다.
        /// </summary>
        public void MarkIdleAttack()
        {
            idleAttackTime = Time.time;
        }

        /// <summary>
        /// 대기에서 시작한 공격이 아직 counterInputTime 안이면 true. 이때 우클릭이 오면 공격을 상쇄로 바꿉니다.
        /// </summary>
        public bool CanCancelToCounter => Time.time - idleAttackTime <= counterInputTime;

        /// <summary>
        /// 스태미나를 쓰고 상쇄를 시작합니다 (Counter 트리거). 스태미나가 없으면 false.
        /// </summary>
        public bool TryStartCounter()
        {
            if (Stamina.TryUseStamina(counterStamina) == false) return false;
            idleAttackTime = float.NegativeInfinity; // 한 번 바꿨으면 같은 공격에서 다시 안 바꿈
            Animator.SetTrigger(CounterHash);
            Debug.Log("[Player] 상쇄 시작");
            return true;
        }

        /// <summary>
        /// 좌클릭을 뗀 채 태클이 끝나 Locomotion으로 돌아갈 때, tackleComboTime 안에 좌클릭하면 이 타부터 시작. ChargeState.Exit에서 호출합니다.
        /// 0을 넣으면 지움 (차지를 시작했을 때).
        /// </summary>
        public void SetNextCombo(int combo)
        {
            nextCombo = combo;
            nextComboEndTime = Time.time + tackleComboTime;
        }

        /// <summary>
        /// 태클 중 가벼운 공격이면 피해를 줄이고 true (피격 모션 없음). PlayerHealth.TakeDamage에서 호출합니다 (태클을 안 쓰면 항상 false).
        /// </summary>
        public bool TryTackleArmor(AttackInfo info, ref float damage)
        {
            if (IsTackleArmor == false) return false;
            if (info.Tag != AttackTag.Light) return false;

            float reduce = tackleDamageReduce + (Loadout != null ? Loadout.TackleReduce : 0f);
            damage *= Mathf.Max(0f, 1f - reduce);
            Debug.Log("[Player] 태클로 버팀");
            return true;
        }

        #endregion
    }
}
