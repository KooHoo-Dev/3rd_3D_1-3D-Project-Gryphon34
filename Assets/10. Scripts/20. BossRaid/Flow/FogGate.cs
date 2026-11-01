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

        private bool isLoading;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        // 2단계 문은 1단계를 한 번 잡기 전까지 숨김 (대장간·냄비와 같음)
        private void Awake()
        {
            if (stage >= 2 && GameData.Current.stage1Cleared == false) gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isLoading) return;
           
            if (other.GetComponentInParent<PlayerController>() == null) return;

            isLoading = true;
            GameData.Current.currentStage = stage;
            Debug.Log($"[안개문] {stage}단계 투기장으로 들어갑니다");
            SceneManager.LoadScene(bossSceneName);
        }
    }
}
