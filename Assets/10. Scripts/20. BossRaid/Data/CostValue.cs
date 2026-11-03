using System.Text;

namespace BossRaid.Data
{
    // 표 한 줄의 "비용" 칸들 (소울 + 소재 6종). 무기·방어구·요리 줄이 이걸 물려받아서 같은 칸 이름을 씁니다
    // TSV 헤더 이름 = 필드 이름 (Souls, DragonScale, Horn …). 안 쓰는 칸은 0
    [System.Serializable]
    public class CostValue
    {
        public int Souls;
        public int DragonScale;
        public int Horn;
        public int TailSpike;
        public int TailMeat;
        public int RageScale;
        public int DragonHeart;

        public int GetCount(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.DragonScale: return DragonScale;
                case MaterialType.Horn: return Horn;
                case MaterialType.TailSpike: return TailSpike;
                case MaterialType.TailMeat: return TailMeat;
                case MaterialType.RageScale: return RageScale;
                case MaterialType.DragonHeart: return DragonHeart;
                default: return 0;
            }
        }

        // 가진 소울·소재로 낼 수 있는지
        public bool CanPay(Progress progress)
        {
            if (progress.souls < Souls) return false;
            for (int i = 0; i < progress.materials.Length; i++)
            {
                if (progress.materials[i] < GetCount((MaterialType)i)) return false;
            }
            return true;
        }

        // 냄 (CanPay를 먼저 확인하고 부를 것)
        public void Pay(Progress progress)
        {
            progress.souls -= Souls;
            for (int i = 0; i < progress.materials.Length; i++)
            {
                progress.materials[i] -= GetCount((MaterialType)i);
            }
        }

        // "소울 800 · 뿔 1 · 화룡 비늘 4" (비용이 없으면 "무료")
        public string ToCostText()
        {
            StringBuilder text = new StringBuilder();
            if (Souls > 0) text.Append($"소울 {Souls}");
            for (int i = 0; i < System.Enum.GetValues(typeof(MaterialType)).Length; i++)
            {
                var type = (MaterialType)i;
                int count = GetCount(type);
                if (count <= 0) continue;
                if (text.Length > 0) text.Append(" · ");
                text.Append($"{UI.ItemDisplay.GetName(type)} {count}");
            }
            return text.Length > 0 ? text.ToString() : "무료";
        }

        // "소울 150/200 · 화룡 비늘 3/2" 가진 양/필요한 양. 모자란 것만 빨갛게 (숫자로도 읽히게, 색만으로 알리지 않음)
        public string ToCostText(Progress progress)
        {
            StringBuilder text = new StringBuilder();
            if (Souls > 0) AppendCost(text, "소울", progress.souls, Souls);
            for (int i = 0; i < System.Enum.GetValues(typeof(MaterialType)).Length; i++)
            {
                var type = (MaterialType)i;
                int count = GetCount(type);
                if (count <= 0) continue;
                AppendCost(text, UI.ItemDisplay.GetName(type), progress.materials[i], count);
            }
            return text.Length > 0 ? text.ToString() : "무료";
        }

        private const string ShortColor = "#FF7A7A"; // 모자란 재료 글자색

        private static void AppendCost(StringBuilder text, string name, int have, int need)
        {
            if (text.Length > 0) text.Append(" · ");
            string cost = $"{name} {have}/{need}";
            text.Append(have < need ? $"<color={ShortColor}>{cost}</color>" : cost);
        }
    }
}
