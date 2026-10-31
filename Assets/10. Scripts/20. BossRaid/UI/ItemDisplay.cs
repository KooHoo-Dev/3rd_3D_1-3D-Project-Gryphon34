using BossRaid.Data;
using UnityEngine;

namespace BossRaid.UI
{
    // 아이템을 화면에 보여줄 때의 한글 이름과 임시 색 (결과 패널, 나중에 인벤토리)
    // 아이콘 그림이 생기기 전까지는 이 색의 도형을 아이콘 자리에 씁니다
    public static class ItemDisplay
    {
        public const string SoulName = "소울";
        public const string HumanityName = "인간성";
        public static readonly Color SoulColor = new Color(0.55f, 0.75f, 0.95f);
        public static readonly Color HumanityColor = new Color(0.85f, 0.7f, 0.3f); // HUD 인간성 칸과 같은 색

        public static string GetName(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.DragonScale: return "화룡 비늘";
                case MaterialType.Horn: return "뿔";
                case MaterialType.TailSpike: return "꼬리 가시";
                case MaterialType.TailMeat: return "꼬리 고기";
                case MaterialType.RageScale: return "분노 비늘";
                case MaterialType.DragonHeart: return "화룡 심장";
                default: return type.ToString();
            }
        }

        public static Color GetColor(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.DragonScale: return new Color(0.85f, 0.35f, 0.2f);
                case MaterialType.Horn: return new Color(0.8f, 0.78f, 0.7f);
                case MaterialType.TailSpike: return new Color(0.6f, 0.55f, 0.5f);
                case MaterialType.TailMeat: return new Color(0.75f, 0.4f, 0.45f);
                case MaterialType.RageScale: return new Color(0.95f, 0.55f, 0.15f);
                case MaterialType.DragonHeart: return new Color(0.75f, 0.1f, 0.2f);
                default: return Color.white;
            }
        }
    }
}
