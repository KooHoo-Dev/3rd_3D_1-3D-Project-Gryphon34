using BossRaid.Combat;
using DG.Tweening;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid
{
    // 몬스터헌터식 3인칭 카메라입니다.
    // 평소에는 마우스로 자유롭게 돌리는 카메라이고,
    // 타겟 카메라를 켜면 좌우 각도가 보스 쪽으로 자동으로 따라갑니다. (상하는 계속 마우스로 조작합니다)
    // 이 스크립트는 카메라 피벗에 붙이고, 실제 Camera는 피벗의 자식으로 둡니다.
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform target;                // 따라갈 플레이어
        [SerializeField] private float pivotHeight = 1.8f;        // 플레이어 발밑에서 피벗까지의 높이
        [SerializeField] private float followSmoothTime = 0.08f;  // 클수록 카메라가 플레이어를 늦게 따라옵니다

        [Header("Orbit")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float distance = 6f; // 큰 보스가 한 화면에 들어오도록 멀리 둡니다
        [SerializeField] private float horizontalSensitivity = 0.5f;
        [SerializeField] private float verticalSensitivity = 0.5f;
        [SerializeField] private float pitchAngleMin = -30f; // 음수일수록 더 올려다볼 수 있습니다
        [SerializeField] private float pitchAngleMax = 60f;

        [Header("Target Camera")]
        [SerializeField] private Transform lockTarget;            // 보스의 몸통 중심. 루트는 발밑이라 척추 뼈 같은 곳을 넣습니다
        [SerializeField] private Key toggleKey = Key.Q;
        [SerializeField] private float targetTurnSpeed = 5f;      // 보스 쪽으로 돌아가는 빠르기
        [SerializeField] private float maxTargetDistance = 50f;   // 보스와 이보다 멀어지면 타겟 카메라가 꺼집니다
        [SerializeField] private float minTrackDistance = 2f;     // 보스 바로 밑에서는 방향이 마구 바뀌므로 따라가지 않습니다

        [Header("Camera Collision")]
        [SerializeField] private LayerMask cameraCollisionMask = ~0; // 보스 레이어는 빼두세요. 안 그러면 보스가 다가올 때마다 카메라가 당겨집니다
        [SerializeField] private float cameraRadius = 0.25f;        // 카메라를 감싸는 구의 반지름. 벽과의 여유 공간
        [SerializeField] private float minDistance = 1f;            // 벽에 막혀도 이 거리보다 가까워지지 않습니다
        [SerializeField] private float distanceReturnSpeed = 5f;    // 벽에서 벗어났을 때 원래 거리로 돌아가는 속도

        // 입력 회전량들을 모아둘겁니다
        private float angleX = 15f;
        private float angleY = 0f;

        // 지금 실제로 적용 중인 카메라 거리입니다
        private float currentDistance;
        private readonly RaycastHit[] cameraHits = new RaycastHit[16];

        private Vector3 followVelocity;

        // 흔들림: cameraTransform 위치는 매 프레임 거리로 덮어쓰므로, 흔들림 값을 따로 두고 그 위치에 더합니다
        private Vector3 shakeOffset;
        private Tweener shakeTween;

        // 거리 요청: 기본 거리 distance에 더하는 값 (차지 중 멀어짐 등). 벽 충돌은 이 값을 더한 거리로 계산
        private float zoomOffset;
        private Tweener zoomTween;

        public bool IsTargeting { get; private set; }

        private void OnEnable()
        {
            CameraShake.OnShakeRequested += HandleShakeRequested;
            CameraZoom.OnZoomRequested += HandleZoomRequested;
        }

        private void OnDisable()
        {
            CameraShake.OnShakeRequested -= HandleShakeRequested;
            shakeTween?.Kill();
            shakeOffset = Vector3.zero;
            CameraZoom.OnZoomRequested -= HandleZoomRequested;
            zoomTween?.Kill();
            zoomOffset = 0f;
        }

        // 연달아 맞으면 앞의 흔들림을 끊고 새로 (안 끊으면 둘이 같은 값을 번갈아 씀)
        private void HandleShakeRequested(float strength, float duration)
        {
            shakeTween?.Kill();
            shakeOffset = Vector3.zero;
            shakeTween = DOTween.Shake(() => shakeOffset, x => shakeOffset = x, duration, strength)
                .OnComplete(() => shakeOffset = Vector3.zero);
        }

        // 새 요청이 오면 앞의 것을 끊고 지금 값에서 새 목표로 (차지를 빨리 떼면 멀어지던 중에 바로 돌아옴)
        private void HandleZoomRequested(float offset, float duration)
        {
            zoomTween?.Kill();
            zoomTween = DOTween.To(() => zoomOffset, x => zoomOffset = x, offset, duration);
        }

        private void Start()
        {
            // 플레이어의 자식으로 두면 몸이 돌 때 피벗도 같이 끌려가고, 따라오는 지연도 줄 수 없습니다.
            // 그래서 시작할 때 떼어내서 독립적으로 따라가게 합니다.
            transform.SetParent(null, true);
            transform.position = GetPivotPosition();

            angleY = target.eulerAngles.y; // 캐릭터 뒤에서 출발합니다
            currentDistance = distance;
            LockCursor();
        }

        private void LateUpdate()
        {
            UpdateTargeting();
            UpdateAngle();
            UpdateFollow();
            UpdateCameraRotation();
        }

        // 다른 보스가 나오면 바꿔 끼울 수 있게 열어둡니다.
        public void SetLockTarget(Transform newTarget)
        {
            lockTarget = newTarget;
            if (!CanTarget()) IsTargeting = false;
        }

        private void UpdateTargeting()
        {
            if (SimpleInput.GetKeyDown(toggleKey)) IsTargeting = !IsTargeting && CanTarget();

            // 보스가 죽어서 꺼졌거나 너무 멀어지면 자유 카메라로 돌아갑니다
            if (IsTargeting && !CanTarget()) IsTargeting = false;
        }

        private bool CanTarget()
        {
            if (lockTarget == null || !lockTarget.gameObject.activeInHierarchy) return false;
            return (lockTarget.position - target.position).sqrMagnitude <= maxTargetDistance * maxTargetDistance;
        }

        private void UpdateAngle()
        {
            Vector2 delta = SimpleInput.GetMouseDelta();
            angleX -= delta.y * verticalSensitivity;
            angleX = Mathf.Clamp(angleX, pitchAngleMin, pitchAngleMax);

            // 타겟 카메라 중에는 좌우를 카메라가 맡습니다. 마우스 좌우 입력은 무시합니다.
            if (IsTargeting) TrackLockTarget();
            else angleY += delta.x * horizontalSensitivity;
        }

        private void TrackLockTarget()
        {
            Vector3 toTarget = lockTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < minTrackDistance * minTrackDistance) return;

            float targetYaw = Quaternion.LookRotation(toTarget).eulerAngles.y;
            angleY = Mathf.LerpAngle(angleY, targetYaw, targetTurnSpeed * Time.deltaTime);
        }

        private void UpdateFollow()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, GetPivotPosition(), ref followVelocity, followSmoothTime);
        }

        private Vector3 GetPivotPosition()
        {
            return target.position + Vector3.up * pivotHeight;
        }

        private void UpdateCameraRotation()
        {
            transform.rotation = Quaternion.Euler(angleX, angleY, 0f);

            float targetDistance = GetBlockedCameraDistance();
            if (targetDistance < currentDistance)
            {
                // 벽에 막히면 바로 당깁니다. 천천히 당기면 그 사이에 벽을 뚫고 보입니다.
                currentDistance = targetDistance;
            }
            else
            {
                // 벽에서 벗어나면 원래 거리로 부드럽게 돌아갑니다.
                currentDistance = Mathf.Lerp(currentDistance, targetDistance, distanceReturnSpeed * Time.deltaTime);
            }

            cameraTransform.localPosition = new Vector3(0f, 0f, -currentDistance) + shakeOffset;
        }

        // 피벗에서 카메라 방향으로 구를 쏴서 벽에 막히는 거리를 구합니다.
        // 막히는 게 없으면 distance, 막히면 그 거리 (최소 minDistance)를 반환합니다.
        private float GetBlockedCameraDistance()
        {
            Vector3 origin = transform.position;
            Vector3 direction = -transform.forward;
            float wantedDistance = distance + zoomOffset; // 거리 요청까지 더한 원하는 거리
            int count = Physics.SphereCastNonAlloc(origin, cameraRadius, direction, cameraHits,
                wantedDistance, cameraCollisionMask, QueryTriggerInteraction.Ignore);

            float closest = wantedDistance;
            for (int i = 0; i < count; i++)
            {
                // 플레이어 자신의 콜라이더는 무시합니다
                if (cameraHits[i].collider.transform.IsChildOf(target)) continue;
                closest = Mathf.Min(closest, cameraHits[i].distance);
            }

            return Mathf.Max(closest, minDistance);
        }

        private void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
