using BossRaid.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRaid.Flow
{
    // 마을 씬의 안개문입니다. 플레이어가 들어오면 투기장 씬을 엽니다.
    // 콜라이더는 Is Trigger로 둡니다. (컴포넌트를 처음 붙일 때 자동으로 켜집니다)
    [RequireComponent(typeof(Collider))]
    public class FogGate : MonoBehaviour
    {
        [SerializeField] private string bossSceneName = "01. Boss";

        private bool isLoading; // 씬을 여는 중에 또 들어와서 두 번 여는 것을 막습니다

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isLoading) return;
            // 플레이어의 몸 콜라이더든 칼 판정이든, 위로 올라가서 PlayerController가 있으면 플레이어입니다
            if (other.GetComponentInParent<PlayerController>() == null) return;

            isLoading = true;
            Debug.Log("[안개문] 투기장으로 들어갑니다");
            SceneManager.LoadScene(bossSceneName);
        }
    }
}
