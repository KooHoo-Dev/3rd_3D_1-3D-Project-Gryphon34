using System;

namespace BossRaid.Data
{
    // 이번 판에 받은 것. HuntManager가 계산해서 Progress에 더하고, OnHuntFinished로 결과 패널에 넘깁니다
    [Serializable]
    public class HuntReward
    {
        public int souls;
        public int[] materials = new int[Enum.GetValues(typeof(MaterialType)).Length];
        public int humanity;

        public void AddTo(Progress progress)
        {
            progress.souls += souls;
            for (int i = 0; i < materials.Length; i++) progress.materials[i] += materials[i];
            progress.humanity += humanity;
        }
    }
}
