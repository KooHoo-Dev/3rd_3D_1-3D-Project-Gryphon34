using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 한 줄: [아이콘] 이름 … 개수. 결과 패널에서 쓰고, 나중에 인벤토리에서도 같은 줄을 씁니다
    public class ItemRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;

        // sprite가 없으면(아이콘 그림이 아직 없으면) 프리팹의 둥근 도형에 color만 입힙니다
        public void Set(Sprite sprite, Color color, string itemName, string count)
        {
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.color = Color.white;
            }
            else icon.color = color;

            nameText.text = itemName;
            countText.text = count;
        }
    }
}
