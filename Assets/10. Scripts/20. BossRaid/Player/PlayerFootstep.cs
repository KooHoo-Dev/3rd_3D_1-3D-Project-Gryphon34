using UnityEngine;

namespace BossRaid.Player
{
    // Locomotion 칸의 Footstep 이벤트(AnimStateEventSender, 매 바퀴 반복)를 받아 발소리를 냅니다.
    // 걷기·달리기는 그 순간의 애니메이터 Speed(0 = 대기, 1 = 걷기, 2 = 달리기)로 고릅니다.
    public class PlayerFootstep : MonoBehaviour
    {
        // Locomotion 칸의 AnimStateEventSender에 적힌 신호 이름과 같아야 합니다
        private const string FootstepEvent = "Footstep";

        [Header("소리 (크기·피치·2D/3D는 AudioSource에서)")]
        [SerializeField] private AudioSource audioSource; // 플레이어 루트처럼 항상 켜진 오브젝트 (꺼지면 소리가 끊김)
        [SerializeField] private AudioClip[] walkSounds;
        [SerializeField] private AudioClip[] runSounds;

        [Header("Speed 기준")]
        [SerializeField] private float minBlend = 0.5f; // 이보다 낮으면 대기가 더 많이 섞임 → 소리 안 냄
        [SerializeField] private float runBlend = 1.5f; // 이 이상이면 달리기 클립이 더 많이 섞임 → runSounds

        private Animator animator;
        private AnimStateEventListener animEventListener;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            animEventListener = GetComponentInChildren<AnimStateEventListener>();
        }

        private void OnEnable()
        {
            animEventListener.OnOccursAnimStateEvent += HandleAnimStateEvent;
        }

        private void OnDisable()
        {
            animEventListener.OnOccursAnimStateEvent -= HandleAnimStateEvent;
        }

        private void HandleAnimStateEvent(string eventName, string parameter)
        {
            if (eventName != FootstepEvent) return;

            float speed = animator.GetFloat(PlayerController.SpeedHash);
            if (speed < minBlend) return;

            PlayRandomSound(speed >= runBlend ? runSounds : walkSounds);
        }

        private void PlayRandomSound(AudioClip[] clips)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
