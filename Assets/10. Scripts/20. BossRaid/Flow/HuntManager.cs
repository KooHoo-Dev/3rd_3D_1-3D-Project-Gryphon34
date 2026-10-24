using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRaid.Flow
{
    public enum HuntResult
    {
        None,   // 아직 사냥을 안 함 (게임 시작 직후)
        Killed, // 보스 처치
        Died    // 플레이어 사망
    }

    // 투기장 씬에 하나 둡니다.
    // 사냥이 끝나면(처치 / 사망) 결과를 남기고, 잠시 뒤 마을로 돌려보냅니다.
    // - 보스와 플레이어를 직접 알지 않습니다. 사망하는 쪽에서 BossKilled / PlayerDied를 불러주면 됩니다.
    // - LastResult는 static이라 씬이 바뀌어도 남습니다. 마을의 Bonfire가 읽습니다.
    public class HuntManager : MonoBehaviour
    {
        public static HuntResult LastResult { get; private set; } = HuntResult.None;

        [SerializeField] private string villageSceneName = "00. Village";
        [SerializeField] private float returnDelay = 3f; // 결과가 난 뒤 마을로 돌아가기까지 기다리는 시간

        private bool isFinished; // 처치 직후 플레이어가 죽는 식으로 결과가 두 번 들어오는 것을 막습니다

        public void BossKilled()
        {
            Finish(HuntResult.Killed);
        }

        public void PlayerDied()
        {
            Finish(HuntResult.Died);
        }

        private void Finish(HuntResult result)
        {
            if (isFinished) return;
            isFinished = true;

            LastResult = result;

            if (result == HuntResult.Killed)
                Debug.Log("[사냥] 처치! 보상: 소울, 소재 (미구현)");
            else
                Debug.Log("[사냥] 사망... 준 피해만큼 소울 (미구현)");

            StartCoroutine(ReturnToVillage());
        }

        private IEnumerator ReturnToVillage()
        {
            yield return new WaitForSeconds(returnDelay);
            Debug.Log("[사냥] 마을로 돌아갑니다");
            SceneManager.LoadScene(villageSceneName);
        }

        [ContextMenu("Test Boss Killed")]
        private void TestBossKilled()
        {
            BossKilled();
        }

        [ContextMenu("Test Player Died")]
        private void TestPlayerDied()
        {
            PlayerDied();
        }
    }
}
