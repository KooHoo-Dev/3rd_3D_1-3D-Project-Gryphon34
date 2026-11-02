using System;

namespace BossRaid.Data
{
    // 죽은 자리에 떨어진 소울 한 덩어리.
    [Serializable]
    public class DroppedSoul
    {
        public int amount;     // 0이면 떨어진 소울 없음
        public float x, y, z;  // 죽은 위치
        public int stage;      // 죽은 단계. 같은 단계 투기장에서만 나타남 (1·2단계가 같은 씬이라)
    }
}
