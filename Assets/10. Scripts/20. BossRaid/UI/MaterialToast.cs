using System.Collections.Generic;
using BossRaid.Data;
using BossRaid.Flow;
using UnityEngine;

namespace BossRaid.UI
{
    // 사냥 도중 소재를 얻으면 오른쪽 아래(소울 위)에 "뿔 +1" 줄을 잠깐 띄웁니다 (몬스터헌터 보수 획득처럼 설명 문장 없이)
    // HuntManager.OnMaterialGained를 듣습니다. 마을처럼 HuntManager가 없는 씬에서는 스스로 꺼짐
    // 화면 구성은 BossRaid/UI/Add Gain Popups To Player HUD 메뉴(PlayerHUDBuilder)가 만듭니다
    public class MaterialToast : MonoBehaviour
    {
        [SerializeField] private ItemRow rowTemplate;    // 꺼 둔 줄 하나 (CanvasGroup 포함). 얻을 때마다 복제합니다
        [SerializeField] private float showTime = 2.5f;  // 다 보이는 시간
        [SerializeField] private float fadeTime = 0.5f;  // 사라지는 시간
        [SerializeField] private Sprite[] materialIcons; // MaterialType 순서. 비어 있으면 ItemDisplay의 색 도형

        private class Toast
        {
            public CanvasGroup group;
            public float timer;
        }

        private HuntManager hunt;
        private readonly List<Toast> toasts = new();

        private void Start()
        {
            rowTemplate.gameObject.SetActive(false);

            hunt = FindAnyObjectByType<HuntManager>();
            if (hunt == null) // 마을: 사냥이 없음
            {
                enabled = false;
                return;
            }
            hunt.OnMaterialGained += HandleMaterialGained;
        }

        private void OnDestroy()
        {
            if (hunt != null) hunt.OnMaterialGained -= HandleMaterialGained;
        }

        // 히트스톱·튜토리얼 슬로모션 중에도 같은 속도로 사라지게 unscaled 시간
        private void Update()
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                Toast toast = toasts[i];
                toast.timer += Time.unscaledDeltaTime;
                toast.group.alpha = 1f - Mathf.Clamp01((toast.timer - showTime) / fadeTime);
                if (toast.timer < showTime + fadeTime) continue;

                Destroy(toast.group.gameObject);
                toasts.RemoveAt(i);
            }
        }

        // 새 줄은 맨 아래에 붙고, 먼저 뜬 줄은 위로 밀려남 (부모의 Vertical Layout Group)
        private void HandleMaterialGained(MaterialType type, int count)
        {
            int index = (int)type;
            Sprite icon = materialIcons != null && index < materialIcons.Length ? materialIcons[index] : null;

            ItemRow row = Instantiate(rowTemplate, rowTemplate.transform.parent);
            row.gameObject.SetActive(true);
            row.Set(icon, ItemDisplay.GetColor(type), ItemDisplay.GetName(type), $"+{count}");
            toasts.Add(new Toast { group = row.GetComponent<CanvasGroup>() });
        }
    }
}
