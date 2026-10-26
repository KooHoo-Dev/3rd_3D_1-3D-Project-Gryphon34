using System;
using BossRaid.Player;
using Cysharp.Threading.Tasks;
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
    
    public class HuntManager : MonoBehaviour
    {
        public static HuntResult LastResult { get; private set; } = HuntResult.None;

        [SerializeField] private string villageSceneName = "00. Village";
        [SerializeField] private float returnDelay = 3f; // 결과가 난 뒤 마을로 돌아가기까지 기다리는 시간

        public Boss.Boss boss;
        public PlayerHealth playerHealth;
        
        private bool isFinished; // 처치 직후 플레이어가 죽는 식으로 결과가 두 번 들어오는 것을 막습니다

        private void OnEnable()
        {
            boss.OnDeath += BossKilled;
            playerHealth.OnDeath += PlayerDied;
        }

        private void OnDisable()
        {
            boss.OnDeath -= BossKilled;
            playerHealth.OnDeath -= PlayerDied;
        }

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
            ReturnToVillage().Forget();
        }

        private async UniTask ReturnToVillage()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(returnDelay), cancellationToken: destroyCancellationToken);
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
