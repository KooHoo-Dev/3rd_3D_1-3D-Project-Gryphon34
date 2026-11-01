using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 대장간·냄비 목록의 한 줄: 이름 / 효과 / 비용 + 버튼 (대장간은 버튼 둘: 제작·강화 + 장착, 냄비는 요리 하나)
    // 패널이 Refresh 때마다 줄을 새로 만들어서, 버튼 이벤트가 쌓이지 않음
    public class MenuRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button mainButton;
        [SerializeField] private TMP_Text mainLabel;
        [SerializeField] private Button subButton;   // 없어도 됨 (냄비 줄)
        [SerializeField] private TMP_Text subLabel;

        public void SetText(string title, string info, string cost)
        {
            titleText.text = title;
            infoText.text = info;
            costText.text = cost;
        }

        public void SetMain(string label, bool interactable, UnityAction onClick)
        {
            SetButton(mainButton, mainLabel, label, interactable, onClick);
        }

        // label이 비어 있으면 버튼을 숨김 (아직 안 만든 장비는 장착 버튼 없음)
        public void SetSub(string label, bool interactable, UnityAction onClick)
        {
            SetButton(subButton, subLabel, label, interactable, onClick);
        }

        private static void SetButton(Button button, TMP_Text labelText, string label, bool interactable, UnityAction onClick)
        {
            if (button == null) return;

            button.gameObject.SetActive(string.IsNullOrEmpty(label) == false);
            if (labelText != null) labelText.text = label;
            button.interactable = interactable;
            if (onClick != null) button.onClick.AddListener(onClick);
        }
    }
}
