using BossRaid.Data;
using BossRaid.Player;
using BossRaid.UI;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Flow
{
    // 마을 씬의 화톳불입니다. 가까이에서 [E]를 누르면 화톳불 메뉴(BonfirePanel)를 열고 닫습니다.
    // 규칙(인간성 태우기)은 여기에 있고, 패널은 보여 주기만 합니다 (HuntManager ↔ ResultPanel과 같은 나눔)
    public class Bonfire : MonoBehaviour
    {
        public PlayerController player;
        [SerializeField] private BonfirePanel panel;
        [SerializeField] private float interactDistance = 3f; // 이 거리 안이면 [E]로 열 수 있음
        [SerializeField] private int maxPotionLimit = 5;      // 태워서 늘릴 수 있는 회복약 최대 개수 (GDD: 3 → 최대 5)

        public int MaxPotionLimit => maxPotionLimit;
        // 인간성이 있고, 회복약 최대가 아직 끝이 아닐 때만 태울 수 있음
        public bool CanBurnHumanity => GameData.Current.humanity > 0 && GameData.Current.currentMaxPotion < maxPotionLimit;

        private void Start()
        {
            // 씬을 넘어 남은 사람 상태를 새로 만들어진 플레이어에게 넘겨줌 (죽고 오면 HuntManager가 false로 해 둠)
            player.Health.SetMaxHealth(GameData.Current.isHuman);
            player.Potion.SetMaxPotionCount(GameData.Current.currentMaxPotion);
            player.Potion.SetHumanityCount(GameData.Current.humanity);
            player.Potion.OnHumanityUsed += HandleHumanityUsed;

            switch (HuntManager.LastResult)
            {
                case HuntResult.None:
                    Debug.Log("[화톳불] 준비하고 안개문으로 가세요");
                    break;
                case HuntResult.Killed:
                    Debug.Log("[화톳불] 사냥 성공 후 귀환");
                    Debug.Log("[대장간] 제작·강화 (미구현)");
                    break;
                case HuntResult.Died:
                    Debug.Log("[화톳불] 사망 후 귀환");
                    Debug.Log("[대장간] 제작·강화 (미구현)");
                    break;
            }
        }

        private void OnDestroy()
        {
            if (player != null) player.Potion.OnHumanityUsed -= HandleHumanityUsed;
        }

        // 마을에서 인간성을 마심: 개수 −1, 사람 상태 (최대 HP +25, HP 전부 회복). 투기장에서는 HuntManager가 같은 일을 함
        private void HandleHumanityUsed()
        {
            Progress progress = GameData.Current;
            progress.humanity -= 1;
            progress.isHuman = true;
            player.Health.SetMaxHealth(true);
            player.Potion.SetHumanityCount(progress.humanity);
            Debug.Log($"[인간성] 사용 → 사람 상태, 최대 HP {player.Health.MaxHealth}, 남은 인간성 {progress.humanity}");
        }

        private void Update()
        {
            if (panel == null) return;

            bool isNear = Vector3.Distance(player.transform.position, transform.position) <= interactDistance;
            panel.ShowPrompt(isNear);

            if (SimpleInput.GetKeyDown(Key.E) == false) return;
            if (panel.IsOpen) panel.Close();
            else if (isNear) panel.Open(this);
        }

        // 인간성 1개 → 회복약 최대 +1 (영구). 바로 늘어나게 플레이어에게도 넘겨줌
        public void BurnHumanity()
        {
            if (CanBurnHumanity == false) return;

            Progress progress = GameData.Current;
            progress.humanity -= 1;
            progress.currentMaxPotion += 1;
            player.Potion.SetMaxPotionCount(progress.currentMaxPotion);
            player.Potion.SetHumanityCount(progress.humanity);
            Debug.Log($"[화톳불] 인간성을 태움 → 회복약 최대 {progress.currentMaxPotion}, 남은 인간성 {progress.humanity}");
        }
    }
}
