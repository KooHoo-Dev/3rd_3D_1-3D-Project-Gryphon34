using System;
using BossRaid.Data;
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
        [SerializeField] private PickupSouls SoulsPrefab; // 떨어진 소울. 투기장 시작 때 죽은 자리에 만듦

        public Boss.Boss boss;
        public PlayerController player;

        public float ReturnDelay => returnDelay;
        public event Action<HuntResult, HuntReward> OnHuntFinished; // 결과가 난 순간 (처치/사망 + 이번 판에 받은 것)
        
        private bool isFinished; // 처치 직후 플레이어가 죽는 식으로 결과가 두 번 들어오는 것을 막습니다

        private readonly RewardTable rewardTable = new();
        private readonly PartRewardTable partRewardTable = new();

        // 이번 판에 받은 것. 사냥 도중(부위파괴)과 처치 때 여기에 모았다가 Finish에서 GameData에 한 번에 더합니다
        private readonly HuntReward currentHuntReward = new();
        private int givenHpCount; // 이번 판에 이미 준 체력 구간 수 (0.25면 0~3)

        private void Start()
        {
            rewardTable.Load();
            partRewardTable.Load();
            player.Health.OnDeath += PlayerDied;
            player.Potion.OnHumanityUsed += HandleHumanityUsed;
            boss.SetPlayer(player);
            boss.SetStage(GameData.Current.currentStage); // 안개문에서 고른 단계 (HP·공격력 배율·머티리얼·추가 패턴)
            player.Health.SetMaxHealth(GameData.Current.isHuman); // 씬을 넘어 남은 사람 상태를 새 플레이어에게
            player.Potion.SetMaxPotionCount(GameData.Current.currentMaxPotion); // 태운 만큼 늘어난 회복약 최대 개수
            player.Potion.SetHumanityCount(GameData.Current.humanity);

            // 떨어진 소울이 있고 죽은 단계와 같으면 죽은 자리에 만듦. 지난 판 결과(처치/사망)와는 상관없음
            DroppedSoul drop = GameData.Current.droppedSoul;
            if (drop.amount > 0 && drop.stage == GameData.Current.currentStage)
            {
                Instantiate(SoulsPrefab, new Vector3(drop.x, drop.y, drop.z), Quaternion.identity);
                Debug.Log($"[소울] 떨어진 소울 {drop.amount} 생성 ({drop.x:0.0}, {drop.y:0.0}, {drop.z:0.0})");
            }
        }
        
        private void OnEnable()
        {
            boss.OnDeath += BossKilled;
            boss.OnPartBroken += HandlePartBroken;
            boss.OnHpReduce += HandleHpReduce;
        }

        private void OnDisable()
        {
            boss.OnDeath -= BossKilled;
            boss.OnPartBroken -= HandlePartBroken;
            boss.OnHpReduce -= HandleHpReduce;
            player.Health.OnDeath -= PlayerDied;
            player.Potion.OnHumanityUsed -= HandleHumanityUsed;
        }

        // 투기장에서 인간성을 마심: 개수 −1, 사람 상태 (최대 HP +25, HP 전부 회복). 마을에서는 Bonfire가 같은 일을 함
        private void HandleHumanityUsed()
        {
            Data.Progress progress = GameData.Current;
            progress.humanity -= 1;
            progress.isHuman = true;
            player.Health.SetMaxHealth(true);
            player.Potion.SetHumanityCount(progress.humanity);
            Debug.Log($"[인간성] 사용 → 사람 상태, 최대 HP {player.Health.MaxHealth}, 남은 인간성 {progress.humanity}");
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
                GiveKillReward();
            else
            {
                DropSouls(); // 가진 소울을 먼저 떨어뜨림 (사망 소울을 먼저 더하면 그것까지 떨어짐)
                GameData.Current.isHuman = false; // 죽으면 사람 상태만 잃음 (가진 인간성은 그대로). 마을 Bonfire.Start가 이걸 읽어 SetMaxHealth(false)
                GameData.Current.forgeUnlocked = true; // 첫 사망에 대장간이 열림 (GDD §9)
                int deathSouls = CalculateDeathSouls();
                currentHuntReward.souls += deathSouls;
                Debug.Log($"[사냥] 사망... 준 피해만큼 소울 +{deathSouls}");
            }

            // 처치든 사망이든 이번 판에 모은 것(부위 소재 포함)을 한 번에 더함 = 부위 소재는 죽어도 받음
            Data.Progress progress = GameData.Current; // UniTask에도 Progress가 있어서 어느 쪽인지 적어 줌
            currentHuntReward.AddTo(progress);
            progress.meal = ""; // 식사는 사냥 한 번 (이기든 지든 끝). 마을 PlayerLoadout.Start가 효과 없이 다시 적용
            Debug.Log($"[사냥] 이번 판 소울 +{currentHuntReward.souls}, 인간성 +{currentHuntReward.humanity}" +
                      $" → 가진 소울 {progress.souls}, 인간성 {progress.humanity}");

            OnHuntFinished?.Invoke(result, currentHuntReward); // 결과 패널 등 (HuntManager는 누가 듣는지 모름)
            ReturnToVillage().Forget();
        }

        // 죽었을 때: 가진 소울을 죽은 자리에 떨어뜨림. 지난번에 떨어진 걸 못 주웠으면 그건 덮어쓰고 절반만 돌려줌
        private void DropSouls()
        {
            Data.Progress progress = GameData.Current;
            DroppedSoul drop = progress.droppedSoul;
            int oldAmount = drop.amount; // 지난번에 떨어져 있던 양 (주웠으면 0)

            Vector3 pos = player.transform.position;
            drop.amount = progress.souls;
            drop.x = pos.x;
            drop.y = pos.y;
            drop.z = pos.z;
            drop.stage = progress.currentStage;
            progress.souls = 0;

            currentHuntReward.souls += oldAmount / 2; // 못 줍고 또 죽으면 절반은 화톳불로 (GDD §6-6)
            Debug.Log($"[사냥] 소울 {drop.amount} 떨어뜨림 ({pos}), 지난번 것 {oldAmount} → 절반 {oldAmount / 2} 돌려받음");
        }

        // 사망 소울 = 처치 소울 × 준 피해 비율 × DeathRatio (D1 ①)
        private int CalculateDeathSouls()
        {
            RewardValue reward = rewardTable.Get(GameData.Current.currentStage);
            if (reward == null) return 0;

            float damageRate = 1f - boss.CurrentHp / boss.MaxHp; // 준 피해 비율 (HP 60/100 → 0.4)
            return Mathf.FloorToInt(reward.Souls * damageRate * reward.DeathRatio);
        }

        // 부위가 깨진 순간: PartRewardTable의 소재 + 인간성 확률을 이번 판 보상에 적습니다 (GameData에는 Finish에서)
        private void HandlePartBroken(Boss.BossPart part)
        {
            if (isFinished) return;

            int stage = GameData.Current.currentStage;
            foreach (PartRewardValue row in partRewardTable.Get(stage, part.PartName))
            {
                currentHuntReward.materials[(int)row.Material] += row.Count;
                Debug.Log($"[사냥] {part.PartName} 파괴! {row.Material} +{row.Count}");
            }

            RewardValue reward = rewardTable.Get(stage);
            if (reward != null && UnityEngine.Random.value < reward.PartBrokenHumanityChance)
            {
                currentHuntReward.humanity += 1;
                Debug.Log($"[사냥] {part.PartName} 파괴! 인간성 +1");
            }
        }

        // 보스 HP가 깎일 때마다: 새로 넘은 체력 구간(75/50/25%)마다 RewardPart를 HpRateReward개씩 이번 판 보상에 적습니다
        private void HandleHpReduce()
        {
            if (isFinished) return;

            RewardValue reward = rewardTable.Get(GameData.Current.currentStage);
            if (reward == null || reward.HpRate <= 0) return;

            float lostRate = 1f - boss.CurrentHp / boss.MaxHp;          // 깎인 비율 (HP 60/100 → 0.4)
            int maxCount = Mathf.RoundToInt(1f / reward.HpRate) - 1;     // 0.25 → 75/50/25 = 3개 (0%는 처치라 뺌)
            int reachedCount = (int)((lostRate + 0.0001f) / reward.HpRate); // 넘은 구간 수, 소수점 버림 (0.4 / 0.25 = 1.6 → 1). 0.0001은 0.25가 0.2499…로 계산되는 것 막기
            if (reachedCount > maxCount) reachedCount = maxCount;

            while (givenHpCount < reachedCount) // 한 방에 두 구간을 넘으면 두 번 돎
            {
                givenHpCount++;
                currentHuntReward.materials[(int)reward.RewardPart] += reward.HpRateReward;
                Debug.Log($"[사냥] 체력 구간 {givenHpCount}/{maxCount}! {reward.RewardPart} +{reward.HpRateReward}");
            }
        }

        // 처치 보상: RewardTable의 지금 단계 줄대로 이번 판 보상에 더합니다 (부위 소재가 이미 들어 있을 수 있어서 += )
        private void GiveKillReward()
        {
            Data.Progress progress = GameData.Current;
            RewardValue reward = rewardTable.Get(progress.currentStage);
            if (reward == null) return;

            HuntReward got = currentHuntReward;
            got.souls += reward.Souls;
            got.materials[(int)MaterialType.DragonScale] += UnityEngine.Random.Range(reward.ScaleMin, reward.ScaleMax + 1); // int 버전은 끝 숫자를 빼고 뽑음
            if (UnityEngine.Random.value < reward.RageChance) // 분노 비늘은 확률로 (확실히 얻으려면 앞발 파괴)
            {
                got.materials[(int)MaterialType.RageScale] += UnityEngine.Random.Range(reward.RageMin, reward.RageMax + 1);
            }
            if (UnityEngine.Random.value < reward.HeartChance) got.materials[(int)MaterialType.DragonHeart] += 1;

            // 첫 처치: 그 단계를 처음 잡으면 인간성 1개 (1단계 첫 처치 = 2단계 해금)
            if (progress.currentStage == 1 && progress.stage1Cleared == false)
            {
                progress.stage1Cleared = true;
                got.humanity += 1;
            }
            else if (progress.currentStage == 2 && progress.stage2Cleared == false)
            {
                progress.stage2Cleared = true;
                got.humanity += 1;
            }
            else if (UnityEngine.Random.value < reward.HumanityChance) got.humanity += 1; // 첫 처치가 아니면 낮은 확률로

            Debug.Log($"[사냥] 처치! 소울 +{reward.Souls}");
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
