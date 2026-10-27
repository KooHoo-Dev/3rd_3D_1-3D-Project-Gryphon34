using UnityEngine;

namespace BossRaid
{
    // "어떻게 움직일지"만 담당하는 클래스입니다.
    // 무엇을 할지(입력을 읽고 공격할지 이동할지)는 바깥(PlayerController, 상태 클래스)이 정하고
    // 이 클래스는 SetMoveInput / StopMovement 로 받은 값대로 가속, 감속, 회전, 이동만 합니다.
    public class ThirdPersonController : MonoBehaviour
    {
        private const float StopThreshold = 0.1f;

        [Header("Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float runSpeed = 7f;

        [Header("Momentum")]
        [SerializeField] private float acceleration = 10f;       // 출발할 때 가속도
        [SerializeField] private float deceleration = 12f;       // 입력을 뗐을 때 감속도
        [SerializeField] private float brakeDeceleration = 12f;  // 반대로 꺾었을 때 미끄러지며 서는 감속도. 작을수록 많이 미끄러짐
        [SerializeField] private float brakeAngle = 135f;        // 진행 방향과 입력 방향이 이 각도보다 크면 브레이크
        [SerializeField] private float rotationSpeed = 720f;     // 초당 회전 각도
        [SerializeField, Min(1f)] private float slideSpeedDivider = 2f; // 미끄러지며 설 때(StopMovement(false)) 처음 속도를 이 값으로 나눔. 클수록 덜 미끄러짐

        [Header("Ref")]
        [SerializeField] private Transform cameraTransform; // 이동 방향의 기준. 카메라가 보는 좌우 방향으로 움직입니다

        // 이번 프레임의 이동 상태입니다. 애니메이션 쪽에서 읽어갑니다.
        public float CurrentSpeed { get; private set; }
        public bool IsBraking { get; private set; }
        public bool IsMoving => CurrentSpeed > StopThreshold;
        public float WalkSpeed => moveSpeed;
        public float RunSpeed => runSpeed;

        // 바깥에서 넣어준 이동 입력입니다. 실제 이동은 Update에서 이 값으로 처리합니다.
        private Vector2 moveInput;
        private bool runInput;

        private void Awake()
        {
            if (TryGetComponent(out Rigidbody body)) body.freezeRotation = true;
        }

        private void Update()
        {
            UpdatePosition();
        }

        /// <summary>
        /// 이동 입력을 넣어줍니다. 값만 저장하고, 실제 이동은 Update에서 합니다.
        /// </summary>
        public void SetMoveInput(Vector2 axis, bool run)
        {
            moveInput = axis;
            runInput = run;
        }

        /// <summary>
        /// 이동을 멈춥니다. 공격처럼 제자리에 서야 하는 상태에 들어갈 때 호출합니다.
        /// </summary>
        /// <param name="immediate">true면 감속 없이 바로 섭니다. false면 속도를 slideSpeedDivider로 나눈 뒤 감속도대로 미끄러지며 섭니다.</param>
        public void StopMovement(bool immediate = true)
        {
            moveInput = Vector2.zero;
            runInput = false;

            if (immediate)
            {
                CurrentSpeed = 0f;
                IsBraking = false;
            }
            else
            {
                CurrentSpeed /= slideSpeedDivider;
            }
        }

        public void UpdateAnimMoveDelta(Vector3 moveDelta)
        {
            transform.position += moveDelta;
        }

        private void UpdatePosition()
        {
            bool hasInput = moveInput.sqrMagnitude >= 0.01f;
            float targetSpeed = 0f;
            IsBraking = false;

            if (hasInput)
            {
                // moveInput을 3차원 입력 벡터로 바꿔줍니다.
                Vector3 inputVector = new Vector3(moveInput.x, 0f, moveInput.y);

                // 카메라의 좌우 각도만 뽑아서 회전량을 생성해 줍니다.
                Quaternion yaw = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

                // inputVector를 yaw만큼 회전시킨 이동 벡터를 생성합니다.
                Vector3 moveVector = (yaw * inputVector).normalized;

                // 움직이는 중에 거의 반대 방향을 누르면 바로 돌지 않고
                // 바라보는 방향 그대로 미끄러지며 멈춥니다.
                float turnAngle = Vector3.Angle(transform.forward, moveVector);
                IsBraking = IsMoving && turnAngle > brakeAngle;

                if (IsBraking == false)
                {
                    // 몸을 입력 방향으로 서서히 돌립니다
                    Quaternion targetRotation = Quaternion.LookRotation(moveVector);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                    // 입력 방향을 덜 바라볼수록 목표 속도를 낮춥니다.
                    // 멈춘 상태에서 뒤를 누르면 제자리에서 돌아선 뒤 출발하게 됩니다.
                    float facing = Mathf.Clamp01(Vector3.Dot(transform.forward, moveVector));
                    float maxSpeed = runInput ? runSpeed : moveSpeed;
                    targetSpeed = maxSpeed * facing;
                }
            }

            float rate;
            if (IsBraking) rate = brakeDeceleration;
            else rate = targetSpeed > CurrentSpeed ? acceleration : deceleration;
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, rate * Time.deltaTime);

            // 이동은 항상 몸이 바라보는 방향으로 합니다. (관성)
            transform.position += transform.forward * (CurrentSpeed * Time.deltaTime);
        }
        
        public void UpdateDodgeRotation(Vector2 axis)
        {
            bool hasInput = axis.sqrMagnitude >= 0.01f;

            if (hasInput)
            {
                Vector3 inputVector = new Vector3(axis.x, 0f, axis.y);
                Quaternion yaw = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
                // inputVector를 yaw만큼 회전시킨 이동 벡터를 생성합니다.
                Vector3 moveVector = (yaw * inputVector).normalized;
                
                Quaternion targetRotation = Quaternion.LookRotation(moveVector);   // 볼 방향 → 회전값
                transform.rotation = targetRotation;                               // 바로 그 회전으로
            }
        }
    }
}
