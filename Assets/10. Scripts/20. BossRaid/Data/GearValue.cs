namespace BossRaid.Data
{
    // 무기·방어구 표의 공통 칸. 한 줄 = 장비 하나의 한 단계
    // Level 0 줄의 비용 = 제작 비용, Level n 줄의 비용 = n단계로 강화하는 비용. 단계 수는 줄을 더하면 늘어남
    [System.Serializable]
    public class GearValue : CostValue
    {
        public string Id;    // 코드에서 부르는 이름 (Iron, Horn …). Progress에 이 이름으로 저장
        public int Level;
        public string Name;  // 화면에 보이는 이름 (철 대검)
    }
}
