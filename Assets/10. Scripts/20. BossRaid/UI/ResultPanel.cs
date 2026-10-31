using System.Collections.Generic;
using BossRaid.Data;
using BossRaid.Flow;
using TMPro;
using UnityEngine;

namespace BossRaid.UI
{
    // 사냥 결과 패널 (투기장 씬). HuntManager.OnHuntFinished를 듣고, 마을로 돌아가기 전까지 받은 것을 보여 줍니다
    // HuntManager는 이 패널을 모릅니다 (PlayerHUD가 플레이어를 보고, 플레이어는 HUD를 모르는 것과 같은 구조)
    // 화면 구성은 BossRaid/UI/Create Result Panel 메뉴(ResultPanelBuilder)가 만듭니다
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform rowParent;
        [SerializeField] private ItemRow rowTemplate; // 꺼 둔 줄 하나. 받은 것마다 복제합니다
        [SerializeField] private TMP_Text footerText;

        [Header("제목")]
        [SerializeField] private string killedTitle = "토벌 완료";
        [SerializeField] private string diedTitle = "사냥 실패";
        [SerializeField] private Color killedColor = new Color(0.94f, 0.62f, 0.15f);
        [SerializeField] private Color diedColor = new Color(0.94f, 0.58f, 0.58f);

        [Header("아이콘 (비어 있으면 ItemDisplay의 색 도형)")]
        [SerializeField] private Sprite soulIcon;
        [SerializeField] private Sprite humanityIcon;
        [SerializeField] private Sprite[] materialIcons; // MaterialType 순서

        private HuntManager hunt;
        private readonly List<ItemRow> rows = new();

        private void Start()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            rowTemplate.gameObject.SetActive(false);

            hunt = FindAnyObjectByType<HuntManager>();
            if (hunt == null)
            {
                Debug.LogWarning("[ResultPanel] 씬에 HuntManager가 없어서 결과 패널을 끕니다");
                enabled = false;
                return;
            }
            hunt.OnHuntFinished += HandleHuntFinished;
        }

        private void OnDestroy()
        {
            if (hunt != null) hunt.OnHuntFinished -= HandleHuntFinished;
        }

        private void HandleHuntFinished(HuntResult result, HuntReward reward)
        {
            bool killed = result == HuntResult.Killed;
            titleText.text = killed ? killedTitle : diedTitle;
            titleText.color = killed ? killedColor : diedColor;
            footerText.text = $"{Mathf.CeilToInt(hunt.ReturnDelay)}초 뒤 마을로 돌아갑니다";

            ClearRows();
            if (reward.souls > 0) AddRow(soulIcon, ItemDisplay.SoulColor, ItemDisplay.SoulName, reward.souls);
            for (int i = 0; i < reward.materials.Length; i++)
            {
                if (reward.materials[i] <= 0) continue;
                var type = (MaterialType)i;
                Sprite icon = materialIcons != null && i < materialIcons.Length ? materialIcons[i] : null;
                AddRow(icon, ItemDisplay.GetColor(type), ItemDisplay.GetName(type), reward.materials[i]);
            }
            if (reward.humanity > 0) AddRow(humanityIcon, ItemDisplay.HumanityColor, ItemDisplay.HumanityName, reward.humanity);

            group.alpha = 1f;
        }

        private void AddRow(Sprite icon, Color color, string itemName, int count)
        {
            ItemRow row = Instantiate(rowTemplate, rowParent);
            row.gameObject.SetActive(true);
            row.Set(icon, color, itemName, $"+{count}");
            rows.Add(row);
        }

        private void ClearRows()
        {
            foreach (ItemRow row in rows) Destroy(row.gameObject);
            rows.Clear();
        }
    }
}
