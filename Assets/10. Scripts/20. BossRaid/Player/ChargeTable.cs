using System.Collections.Generic;
using Jay.FileIO;
using UnityEngine;

namespace BossRaid.Player
{
    public class ChargeTable
    {
        private const string TableName = "ChargeTable/ChargeTable.tsv";

        private List<ChargeValue> defs = new();

        public int Count => defs.Count;

        public ChargeValue Get(float time)
        {
            ChargeValue result = defs[0];
            foreach (ChargeValue value in defs)
            {
                if (time >= value.Time) result = value;
                else break;
            }
            return result;
        }

        public void Load()
        {
            defs.Clear();

            string path = Application.streamingAssetsPath + "/" + TableName;
            List<ChargeValue> rows = TSVReader.ReadTable<ChargeValue>(path);
            if (rows == null) return;

            defs.AddRange(rows);
        }
    }
}
