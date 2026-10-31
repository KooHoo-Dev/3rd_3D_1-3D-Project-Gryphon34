using System;

namespace BossRaid.Data
{
    // 죽은 자리에 떨어진 소울 한 덩어리.
    [Serializable]
    public class DroppedSoul
    {
        public int amount;     // 0이면 떨어진 소울 없음
        public float x, y, z;  // 죽은 위치
    }
}
