using System.Collections.Generic;
using BossRaid.Data;
using BossRaid.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 냄비 메뉴 (마을 씬). CookingPot이 [E]로 열고 닫고, 버튼은 CookingPot.Cook을 부릅니다
    // 열린 레시피만 줄로 보여 줌. 줄마다 [요리] (지금 먹은 요리는 "먹는 중")
    public class CookingPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;   // "[E] 냄비" (가까이 있을 때만)
        [SerializeField] private CanvasGroup window;
        [SerializeField] private TMP_Text mealText;     // 지금 식사
        [SerializeField] private TMP_Text walletText;   // 소울, 꼬리 고기, 화룡 심장
        [SerializeField] private Transform rowParent;
        [SerializeField] private MenuRow rowTemplate;   // 꺼 둔 줄 하나. 레시피마다 복제합니다 (sub 버튼은 안 씀)
        [SerializeField] private Button closeButton;

        [Header("소리 (2D, 비어 있으면 안 냄)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] eatSounds; // 요리를 먹었을 때

        public bool IsOpen { get; private set; }

        private CookingPot pot;
        private readonly List<MenuRow> rows = new();

        private void Start()
        {
            SetWindowVisible(false);
            promptText.gameObject.SetActive(false);
            rowTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(Close);
        }

        public void ShowPrompt(bool show)
        {
            promptText.gameObject.SetActive(show && IsOpen == false);
        }

        public void Open(CookingPot owner)
        {
            pot = owner;
            IsOpen = true;
            promptText.gameObject.SetActive(false);
            SetWindowVisible(true);
            MenuLock.Set(pot.player, true);
            Refresh();
        }

        public void Close()
        {
            if (IsOpen == false) return;
            IsOpen = false;
            SetWindowVisible(false);
            MenuLock.Set(pot.player, false);
        }

        private void Refresh()
        {
            Progress progress = GameData.Current;
            RecipeValue meal = pot.Table.Get(progress.meal);
            mealText.text = meal != null ? $"지금 식사: {meal.Name}" : "지금 식사: 없음";
            walletText.text = $"{ItemDisplay.SoulName} {progress.souls}" +
                              $"   {ItemDisplay.GetName(MaterialType.TailMeat)} {progress.materials[(int)MaterialType.TailMeat]}" +
                              $" · {ItemDisplay.GetName(MaterialType.DragonHeart)} {progress.materials[(int)MaterialType.DragonHeart]}";

            foreach (MenuRow row in rows) Destroy(row.gameObject);
            rows.Clear();

            foreach (RecipeValue recipe in pot.Table.All)
            {
                if (pot.IsUnlocked(recipe) == false) continue;
                AddRow(recipe);
            }
        }

        private void AddRow(RecipeValue recipe)
        {
            MenuRow row = Instantiate(rowTemplate, rowParent);
            row.gameObject.SetActive(true);
            row.SetText(recipe.Name, GetEffectText(recipe), recipe.ToCostText());

            string label = pot.IsEating(recipe) ? "먹는 중" : "요리";
            row.SetMain(label, pot.CanCook(recipe), () =>
            {
                bool canCook = pot.CanCook(recipe);
                pot.Cook(recipe);
                if (canCook) PlayRandomSound(eatSounds);
                Refresh();
            });
            row.SetSub("", false, null);

            rows.Add(row);
        }

        private static string GetEffectText(RecipeValue recipe)
        {
            List<string> parts = new();
            if (recipe.MaxStamina > 0f) parts.Add($"최대 스태미나 +{recipe.MaxStamina}");
            if (recipe.StaminaRegen > 0f) parts.Add($"스태미나 회복 +{recipe.StaminaRegen * 100:0}%");
            if (recipe.ChargeTimeReduce > 0f) parts.Add($"차지 시간 −{recipe.ChargeTimeReduce * 100:0}%");
            if (recipe.QuickStandUp) parts.Add("넘어지면 바로 기상");
            if (recipe.PerfectGuardFatigue > 0f) parts.Add($"퍼펙트 가드 때 보스 피로 +{recipe.PerfectGuardFatigue}");
            if (recipe.FireReduce > 0f) parts.Add($"불 피해 −{recipe.FireReduce * 100:0}%");
            if (recipe.GuardStaminaReduce > 0f) parts.Add($"가드 스태미나 −{recipe.GuardStaminaReduce * 100:0}%");
            return parts.Count > 0 ? string.Join(", ", parts) : "효과 없음";
        }

        private void PlayRandomSound(AudioClip[] clips)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        private void SetWindowVisible(bool visible)
        {
            window.alpha = visible ? 1f : 0f;
            window.blocksRaycasts = visible;
            window.interactable = visible;
        }
    }
}
