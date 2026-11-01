using System;

namespace BossRaid.Combat
{
    // 카메라 흔들기 요청: 누구나 CameraShake.Play(세기, 시간)를 부르면 카메라(ThirdPersonCamera)가 듣고 흔듭니다
    // 부르는 쪽(대검·보스 등)이 카메라를 몰라도 되게 이벤트로 잇습니다 (HitStop처럼 어디서든 부름)
    public static class CameraShake
    {
        public static event Action<float, float> OnShakeRequested; // (세기, 시간)

        public static void Play(float strength, float duration)
        {
            OnShakeRequested?.Invoke(strength, duration);
        }
    }
}
