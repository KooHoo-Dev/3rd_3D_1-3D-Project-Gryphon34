using System.Collections.Generic;
using BossRaid.Player.States;
using BossRaid.Weapon;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Player
{
    // "무엇을 할지" 결정하는 클래스입니다.
    // - PlayerController       : 입력을 읽고, 상태를 갈아끼우고, 상태들이 쓰는 공용 동작을 제공합니다
    // - ThirdPersonController  : "어떻게 움직일지" (가속, 감속, 회전, 이동)
    // - States                 : 애니메이터 상태(Tag)별 행동
    //
    // 전이(Transition)의 주인은 애니메이터입니다. (Follower FSM)
    // 매 프레임 애니메이터의 현재 상태 Tag를 읽어서 그에 맞는 상태 클래스를 실행합니다.
    // 그래서 여기에는 "언제 어느 상태로 갈지" 결정하는 로직이 없습니다.
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

        // 애니메이터 파라미터 해시. 상태 클래스들도 이 값을 씁니다.
        public static readonly int SpeedHash = Animator.StringToHash("Speed");
        public static readonly int IsAttackHash = Animator.StringToHash("IsAttack");
        public static readonly int IsChargingHash = Animator.StringToHash("IsCharging");
        public static readonly int DodgeHash = Animator.StringToHash("Dodge");
        public static readonly int IsExhaustedHash = Animator.StringToHash("IsExhausted");
        public static readonly int DrinkHash = Animator.StringToHash("Drink");
        
        [Header("Settings")]
        [SerializeField] private float speedDampTime = 0.05f; // Speed 값이 목표치까지 부드럽게 따라가는 시간

        [Header("Stamina")] 
        [SerializeField] private float dodgeStamina = 20f;
        [SerializeField] private float attackStamina = 25f;
        [SerializeField] private float runStamina = 10f;
        public float DodgeStamina => dodgeStamina;
        public float AttackStamina => attackStamina;
        public float RunStamina => runStamina; // 달리기 스태미너, 초당
        
        

        public Animator Animator { get; private set; }
        public ThirdPersonController Movement { get; private set; }
        public GreatSword Sword { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerStamina Stamina { get; private set; }
        public PlayerPotion Potion { get; private set; }
        

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

        private void OnAnimatorMove()
        {
            if ((currentState == dodgeState) == false) return;
            Movement.UpdateAnimMoveDelta(Animator.deltaPosition);
        }
    }
}
