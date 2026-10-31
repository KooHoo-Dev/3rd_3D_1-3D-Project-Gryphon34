using System.Collections.Generic;
using Jay.FileIO;
using UnityEngine;

namespace BossRaid.Data
{
    public class PartRewardTable
    {
        private const string TableName = "RewardTable/PartRewardTable.tsv";

        private List<PartRewardValue> defs = new();

        public List<PartRewardValue> Get(int stage, string part)
        {
            List<PartRewardValue> result = new();
            foreach (PartRewardValue value in defs)
            {
                if (value.Stage == stage && value.Part == part) result.Add(value);
            }

            if (result.Count == 0)
            {
                Debug.LogWarning($"[PartRewardTable] {stage}단계 '{part}' 부위 소재 줄이 없습니다");
            }
            return result;
        }

        public void Load()
        {
            defs.Clear();

            string path = Application.streamingAssetsPath + "/" + TableName;
            List<PartRewardValue> rows = TSVReader.ReadTable<PartRewardValue>(path);
            if (rows == null) return;

            defs.AddRange(rows);
        }
    }
}
