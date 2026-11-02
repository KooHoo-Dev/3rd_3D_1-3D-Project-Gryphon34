using RootMotion.FinalIK;
using UnityEngine;

namespace BossRaid.Player
{
    // 가드 중일 때만 왼손을 칼날(LeftHandGrip)에 붙입니다 (Final IK Limb IK).
    // 평소엔 가중치 0 = 원래 애니메이션 그대로. Limb IK는 LateUpdate에서 계산하므로 여기서는 가중치만 바꿉니다.
    [RequireComponent(typeof(PlayerController))]
    public class GuardHandIK : MonoBehaviour
    {
        [SerializeField] private LimbIK limbIK;  // 왼팔 Limb IK (Target = LeftHandGrip)
        [SerializeField] private float blendSpeed; // 가중치가 1초에 바뀌는 양 (0 → 1까지 1 / blendSpeed초)

        private PlayerController controller;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            float target = controller.IsInGuardState ? 1f : 0f;
            float weight = Mathf.MoveTowards(limbIK.solver.IKPositionWeight, target, blendSpeed * Time.deltaTime);

            limbIK.solver.IKPositionWeight = weight;
            limbIK.solver.IKRotationWeight = weight;
        }
    }
}
