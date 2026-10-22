using UnityEngine;

namespace BossRaid
{
    [RequireComponent(typeof(ThirdPersonController))]
    public class PlayerAnimation : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int Combo1Hash = Animator.StringToHash("Combo1_Start");
        private static readonly int Combo2Hash = Animator.StringToHash("Combo2_Start");
        private static readonly int Combo3Hash = Animator.StringToHash("Combo3_Start");

        [Header("Settings")]
        [SerializeField] private float dampTime = 0.05f;

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
        }
    }
}
