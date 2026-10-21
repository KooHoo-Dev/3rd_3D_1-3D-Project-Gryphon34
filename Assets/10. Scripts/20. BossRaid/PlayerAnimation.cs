using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid
{
    // ThirdPersonController의 이동 상태를 읽어서 Animator 파라미터로 넘겨주는 스크립트입니다.
    // Animator Controller에는 "Speed"(Float) 파라미터와
    // Idle(0) / Walk(1) / Run(2) 로 구성된 1D Blend Tree가 있어야 합니다.
    [RequireComponent(typeof(ThirdPersonController))]
    public class PlayerAnimation : MonoBehaviour
    {
        // 문자열 대신 해시값을 미리 만들어 두면 매 프레임 문자열 비교를 하지 않아도 됩니다.
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("Settings")]
        [SerializeField] private float dampTime = 0.05f; // 값이 목표치까지 부드럽게 따라가는 시간

        [Header("Ref")]
        [SerializeField] private Animator animator;

        private ThirdPersonController controller;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            
            animator.applyRootMotion = false;
        }

        private void Update()
        {
            // 입력이 아니라 실제 속도를 Blend 값으로 바꿉니다.
            // 0 ~ 걷기 속도 => 0 ~ 1, 걷기 ~ 달리기 속도 => 1 ~ 2
            // 그래서 가속/감속, 브레이크로 미끄러지는 동안에도 애니메이션이 속도를 따라갑니다.
            float speed = controller.CurrentSpeed;
            float blend = speed <= controller.WalkSpeed
                ? Mathf.InverseLerp(0f, controller.WalkSpeed, speed)
                : 1f + Mathf.InverseLerp(controller.WalkSpeed, controller.RunSpeed, speed);

            animator.SetFloat(SpeedHash, blend, dampTime, Time.deltaTime);
            
            // 테스트용. 확인 끝나면 지우세요.
            if (SimpleInput.GetKeyDown(Key.Digit1)) animator.CrossFadeInFixedTime("Combo1_Start", 0.1f);
            if (SimpleInput.GetKeyUp(Key.Digit1))   animator.CrossFadeInFixedTime("Combo1_End", 0.05f);

            if (SimpleInput.GetKeyDown(Key.Digit2)) animator.CrossFadeInFixedTime("Combo2_Start", 0.1f);
            if (SimpleInput.GetKeyUp(Key.Digit2))   animator.CrossFadeInFixedTime("Combo2_End", 0.05f);

            if (SimpleInput.GetKeyDown(Key.Digit3)) animator.CrossFadeInFixedTime("Combo3_Start", 0.1f);
            if (SimpleInput.GetKeyUp(Key.Digit3))   animator.CrossFadeInFixedTime("Combo3_End", 0.05f);
            
        }
    }
}
