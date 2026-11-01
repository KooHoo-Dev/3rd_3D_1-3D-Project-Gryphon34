using BossRaid.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRaid.Flow
{
    [RequireComponent(typeof(Collider))]
    public class FogGate : MonoBehaviour
    {
        [SerializeField] private string bossSceneName = "01. Boss";

        private bool isLoading;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isLoading) return;
           
            if (other.GetComponentInParent<PlayerController>() == null) return;

            isLoading = true;
            Debug.Log("[안개문] 투기장으로 들어갑니다");
            SceneManager.LoadScene(bossSceneName);
        }
    }
}
