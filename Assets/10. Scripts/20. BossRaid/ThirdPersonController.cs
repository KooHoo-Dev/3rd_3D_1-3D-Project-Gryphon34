using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid
{
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

        [Header("Ref")]
        [SerializeField] private Transform cameraTransform; // 이동 방향의 기준. 카메라가 보는 좌우 방향으로 움직입니다

        // 이번 프레임의 이동 상태입니다. 애니메이션 쪽에서 읽어갑니다.
        public float CurrentSpeed { get; private set; }
        public bool IsBraking { get; private set; }
        public bool IsMoving => CurrentSpeed > StopThreshold;
        public float WalkSpeed => moveSpeed;
        public float RunSpeed => runSpeed;

        private void Awake()
        {
            if (TryGetComponent(out Rigidbody body)) body.freezeRotation = true;
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            bool hasInput = inputAxis.sqrMagnitude >= 0.01f;
            float targetSpeed = 0f;
            IsBraking = false;

            if (hasInput)
            {
                // inputAxis를 3차원 입력 벡터로 바꿔줍니다.
                Vector3 inputVector = new Vector3(inputAxis.x, 0f, inputAxis.y);

                // 카메라의 좌우 각도만 뽑아서 회전량을 생성해 줍니다.
                Quaternion yaw = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

                // inputVector를 yaw만큼 회전시킨 이동 벡터를 생성합니다.
                Vector3 moveVector = (yaw * inputVector).normalized;

                // 움직이는 중에 거의 반대 방향을 누르면 바로 돌지 않고
                // 바라보는 방향 그대로 미끄러지며 멈춥니다.
                float turnAngle = Vector3.Angle(transform.forward, moveVector);
                IsBraking = IsMoving && turnAngle > brakeAngle;

                if (!IsBraking)
                {
                    // 몸을 입력 방향으로 서서히 돌립니다
                    Quaternion targetRotation = Quaternion.LookRotation(moveVector);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                    // 입력 방향을 덜 바라볼수록 목표 속도를 낮춥니다.
                    // 멈춘 상태에서 뒤를 누르면 제자리에서 돌아선 뒤 출발하게 됩니다.
                    float facing = Mathf.Clamp01(Vector3.Dot(transform.forward, moveVector));
                    float maxSpeed = SimpleInput.GetKey(Key.LeftShift) ? runSpeed : moveSpeed;
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
    }
}
