using System.Collections.Generic;
using Jay.FileIO;
using UnityEngine;

namespace BossRaid.Data
{
    // StreamingAssets/RewardTable/RewardTable.tsv를 읽어 단계별 보상을 돌려줍니다 (ChargeTable과 같은 방식)
    public class RewardTable
    {
        private const string TableName = "RewardTable/RewardTable.tsv";

        private List<RewardValue> defs = new();

        public RewardValue Get(int stage)
        {
            foreach (RewardValue value in defs)
            {
                if (value.Stage == stage) return value;
            }
            Debug.LogError($"[RewardTable] {stage}단계 보상 줄이 없습니다");
            return null;
        }

        public void Load()
        {
            defs.Clear();

            string path = Application.streamingAssetsPath + "/" + TableName;
            List<RewardValue> rows = TSVReader.ReadTable<RewardValue>(path);
            if (rows == null) return;

            defs.AddRange(rows);
        }
    }
}
