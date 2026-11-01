using System;

namespace BossRaid.Data
{
    // 가지고 있는 장비 하나 (어떤 장비를 몇 단계까지). Progress.weapons / armors에 들어감
    [Serializable]
    public class GearLevel
    {
        public string id;
        public int level;

        public GearLevel() { }

        public GearLevel(string id, int level)
        {
            this.id = id;
            this.level = level;
        }
    }
}
