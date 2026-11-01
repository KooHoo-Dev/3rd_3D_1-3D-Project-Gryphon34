using System;
using BossRaid.Data;
using BossRaid.Player;
using BossRaid.UI;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRaid.Flow
{
    // 화톳불 옆 냄비 (마을 씬). [E]로 CookingPanel을 열고 닫고, 요리 규칙을 가지고 있습니다 (Bonfire와 같은 구조)
    // 기본 재료는 따로 사지 않고, 요리할 때 레시피의 Souls(기본 재료값)를 바로 냄
    public class CookingPot : MonoBehaviour
    {
        public PlayerController player;
        [SerializeField] private CookingPanel panel;
        [SerializeField] private float interactDistance = 3f;

        public RecipeTable Table => player.Loadout.RecipeTable;

        private void Awake()
        {
            // 2단계 해금(1단계 첫 처치) 전에는 냄비가 안 보임 (GDD §9)
            if (GameData.Current.stage1Cleared == false) gameObject.SetActive(false);
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

        // Unlock 칸이 비었으면 처음부터, 아니면 그 소재를 한 번이라도 얻었으면 열림
        public bool IsUnlocked(RecipeValue recipe)
        {
            if (string.IsNullOrEmpty(recipe.Unlock)) return true;
            if (Enum.TryParse(recipe.Unlock, out MaterialType type)) return GameData.Current.materialFound[(int)type];

            Debug.LogWarning($"[냄비] {recipe.Id}의 Unlock '{recipe.Unlock}'이 MaterialType에 없습니다");
            return false;
        }

        public bool IsEating(RecipeValue recipe)
        {
            return GameData.Current.meal == recipe.Id;
        }

        public bool CanCook(RecipeValue recipe)
        {
            return IsUnlocked(recipe) && IsEating(recipe) == false && recipe.CanPay(GameData.Current);
        }

        // 먹으면 이전 식사는 사라지고 이걸로 덮어씀. 효과는 바로 적용 (마을에서도)
        public void Cook(RecipeValue recipe)
        {
            if (CanCook(recipe) == false) return;

            Progress progress = GameData.Current;
            recipe.Pay(progress);
            progress.meal = recipe.Id;
            player.Loadout.Apply();
            Debug.Log($"[냄비] {recipe.Name} 먹음 → 남은 소울 {progress.souls}");
        }
    }
}
