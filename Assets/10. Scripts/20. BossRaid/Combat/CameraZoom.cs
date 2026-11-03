using System;

namespace BossRaid.Combat
{
    // 카메라 거리 요청: 누구나 CameraZoom.Play(거리 차이, 시간)를 부르면 카메라(ThirdPersonCamera)가 듣고 그 거리로 옮겨갑니다
    // 거리 차이는 기본 거리에 더하는 값 (+ = 멀어짐, 0 = 원래 거리). CameraShake처럼 부르는 쪽이 카메라를 몰라도 됨
    public static class CameraZoom
    {
        public static event Action<float, float> OnZoomRequested; // (거리 차이, 시간)

        public static void Play(float offset, float duration)
        {
            OnZoomRequested?.Invoke(offset, duration);
        }
    }
}
