using UnityEngine;

namespace BossRaid.Flow
{
    // 마을 대장장이 NPC의 망치질 (Crafter@Item-Hammer-Table 클립). 클립에 애니메이션 이벤트 Strike가 한 바퀴에 3번 있어서
    // 받는 함수가 없으면 "AnimationEvent 'Strike' ... has no receiver!" 오류가 계속 남 → 이 컴포넌트가 받습니다
    // Animator가 붙은 오브젝트(Chr_FantasyHero_Preset_6)에 붙일 것. 소리 칸이 비어 있으면 아무것도 안 함
    public class BlacksmithHammer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] strikeSounds;

        // 애니메이션 이벤트가 이름으로 부름 (이름을 바꾸면 안 됨)
        public void Strike()
        {
            if (audioSource == null || strikeSounds == null || strikeSounds.Length == 0) return;
            AudioClip clip = strikeSounds[Random.Range(0, strikeSounds.Length)];
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
