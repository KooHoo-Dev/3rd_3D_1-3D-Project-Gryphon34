using BossRaid.Data;
using BossRaid.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRaid.Flow
{
    [RequireComponent(typeof(Collider))]
    public class FogGate : MonoBehaviour
    {
        [SerializeField] private string bossSceneName = "01. Boss";
        [SerializeField] private int stage = 1; // 이 문으로 들어가면 사냥할 단계
        [SerializeField] private GameObject[] unlockedObjects; // 열려 있을 때만 켬 (불 파티클 등, Play On Awake 켜 두기)
        [SerializeField] private GameObject lockedMessage;     // 잠긴 문에 들어오면 켬 ("지금은 들어갈 수 없다.")

        private bool isLoading;
        private bool isLocked;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        // 2단계 문은 1단계를 한 번 잡기 전까지 잠김. 깨고 마을로 돌아오면 씬이 다시 로드돼서 여기서 다시 정함
        private void Awake()
        {
            isLocked = stage >= 2 && GameData.Current.stage1Cleared == false;
            foreach (GameObject unlockedObject in unlockedObjects)
            {
                if (unlockedObject != null) unlockedObject.SetActive(isLocked == false);
            }
            if (lockedMessage != null) lockedMessage.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isLoading) return;

            if (other.GetComponentInParent<PlayerController>() == null) return;

            if (isLocked)
            {
                if (lockedMessage != null) lockedMessage.SetActive(true);
                return;
            }

            isLoading = true;
            GameData.Current.currentStage = stage;
            Debug.Log($"[안개문] {stage}단계 투기장으로 들어갑니다");
            SceneManager.LoadScene(bossSceneName);
        }

        private void OnTriggerExit(Collider other)
        {
            if (isLocked == false || lockedMessage == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            lockedMessage.SetActive(false);
        }
    }
}
