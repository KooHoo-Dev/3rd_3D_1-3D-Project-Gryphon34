using DG.Tweening;
using BossRaid.Data;
using UnityEngine;

namespace BossRaid.Flow
{
    // 씬의 배경 음악. 마을과 투기장에 같이 씁니다 (씬마다 Music 오브젝트 하나).
    // 마을: startSource(마을 음악)만. 투기장: startSource(환경음) → 보스 발견 때 battleSource(전투 음악) → 결과가 나면 둘 다 페이드 아웃.
    public class MusicPlayer : MonoBehaviour
    {
        [Header("소리 (클립·Loop·크기는 AudioSource에서)")]
        [SerializeField] private AudioSource startSource;  // 씬에 들어오면 바로 (Play On Awake 켬)
        [SerializeField] private AudioSource battleSource; // 보스 발견 때부터 (Play On Awake 끔). 마을은 비움
        [SerializeField] private float fadeTime = 1.5f;

        [Header("투기장만 (비우면 안 들음)")]
        [SerializeField] private Boss.Boss boss;
        [SerializeField] private HuntManager huntManager;

        private float battleVolume; // 인스펙터에서 정한 전투 음악 크기. 페이드 인 목표

        private void Awake()
        {
            if (battleSource != null) battleVolume = battleSource.volume;
        }

        private void OnEnable()
        {
            if (boss != null) boss.OnDiscovered += PlayBattle;
            if (huntManager != null) huntManager.OnHuntFinished += HandleHuntFinished;
        }

        private void OnDisable()
        {
            if (boss != null) boss.OnDiscovered -= PlayBattle;
            if (huntManager != null) huntManager.OnHuntFinished -= HandleHuntFinished;
        }

        // 환경음을 줄여 멈추고, 전투 음악을 0에서 원래 크기까지
        private void PlayBattle()
        {
            FadeOut(startSource);

            if (battleSource == null) return;
            battleSource.DOKill();
            battleSource.volume = 0f;
            battleSource.Play();
            battleSource.DOFade(battleVolume, fadeTime);
        }

        // 처치든 사망이든 조용히 끝냄
        private void HandleHuntFinished(HuntResult result, HuntReward reward)
        {
            FadeOut(startSource);
            FadeOut(battleSource);
        }

        private void FadeOut(AudioSource source)
        {
            if (source == null || source.isPlaying == false) return;
            source.DOKill();
            source.DOFade(0f, fadeTime).OnComplete(source.Stop);
        }

        private void OnDestroy()
        {
            // 씬이 바뀌어 지워질 때 도는 트윈이 지워진 AudioSource를 건드리지 않게
            if (startSource != null) startSource.DOKill();
            if (battleSource != null) battleSource.DOKill();
        }
    }
}
