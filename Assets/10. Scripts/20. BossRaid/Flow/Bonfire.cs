using BossRaid.Data;
using BossRaid.Player;
using BossRaid.UI;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Flow
{
    public class Bonfire : MonoBehaviour
    {
        public PlayerController player;
        [SerializeField] private BonfirePanel panel;
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private int maxPotionLimit = 5;

        public int MaxPotionLimit => maxPotionLimit;
        
        public bool CanBurnHumanity => GameData.Current.humanity > 0 && GameData.Current.currentMaxPotion < maxPotionLimit;

        private void Start()
        {
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
