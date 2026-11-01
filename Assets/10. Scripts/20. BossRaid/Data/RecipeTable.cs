using System.Collections.Generic;
using Jay.FileIO;
using UnityEngine;

namespace BossRaid.Data
{
    public class RecipeTable
    {
        private const string TableName = "RecipeTable/RecipeTable.tsv";

        private readonly List<RecipeValue> defs = new();

        public List<RecipeValue> All => defs;

        // 없으면 null (식사 안 함 = "")
        public RecipeValue Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (RecipeValue value in defs)
            {
                if (value.Id == id) return value;
            }
            Debug.LogWarning($"[RecipeTable] '{id}' 요리 줄이 없습니다");
            return null;
        }

        public void Load()
        {
            defs.Clear();

            string path = Application.streamingAssetsPath + "/" + TableName;
            List<RecipeValue> rows = TSVReader.ReadTable<RecipeValue>(path);
            if (rows == null) return;

            defs.AddRange(rows);
        }
    }
}
