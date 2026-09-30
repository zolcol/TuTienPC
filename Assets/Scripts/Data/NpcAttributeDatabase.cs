using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class NpcAttributeData
    {
        public int attribId;
        public string name;
        public float maxLife = 100f;
        public float minBaseAttack = 20f;
        public float maxBaseAttack = 20f;
        public float attackSpeed = 15f;

        public float metalDamage;
        public float woodDamage;
        public float waterDamage;
        public float fireDamage;
        public float earthDamage;

        public float metalResist;
        public float woodResist;
        public float waterResist;
        public float fireResist;
        public float earthResist;

        public float AverageAttack => maxBaseAttack > minBaseAttack 
            ? (minBaseAttack + maxBaseAttack) * 0.5f 
            : (minBaseAttack > 0f ? minBaseAttack : maxBaseAttack);
    }

    public class NpcAttributeDatabase : ICsvTable
    {
        private static NpcAttributeDatabase instance;
        public static NpcAttributeDatabase Instance => instance ?? (instance = new NpcAttributeDatabase());

        private readonly Dictionary<int, NpcAttributeData> attributes = new Dictionary<int, NpcAttributeData>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || attributes.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            attributes.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = Path.Combine(Application.dataPath, "Settings", "N", "NpcAttribute.csv");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[NpcAttributeDatabase] ⚠️ Không tìm thấy file tại: {filePath}");
                return;
            }

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return;

                    string[] headers = CsvParserHelper.SplitCsvLine(headerLine);
                    var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < headers.Length; i++)
                    {
                        string norm = headers[i].Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
                        colMap[norm] = i;
                    }

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] tokens = CsvParserHelper.SplitCsvLine(line);
                        if (tokens.Length < 3) continue;

                        int attribId = GetColInt(tokens, colMap, "attribid", 2);
                        if (attribId <= 0) continue;

                        string name = GetColString(tokens, colMap, "name", 1);
                        float attackSpeed = GetColFloat(tokens, colMap, "attackspeed", 3, 15f);

                        float maxLife = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "maxlife", 4), 1, 100f);
                        float minAttack = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "minbaseattack", 9), 1, 20f);
                        float maxAttack = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "maxbaseattack", 10), 1, minAttack);

                        NpcAttributeData data = new NpcAttributeData
                        {
                            attribId = attribId,
                            name = name,
                            attackSpeed = attackSpeed,
                            maxLife = Mathf.Max(1f, maxLife),
                            minBaseAttack = minAttack,
                            maxBaseAttack = maxAttack,
                            woodDamage = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "wooddamage", 11), 1, 0f),
                            waterDamage = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "waterdamage", 12), 1, 0f),
                            fireDamage = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "firedamage", 13), 1, 0f),
                            earthDamage = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "earthdamage", 14), 1, 0f),
                            metalDamage = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "metaldamage", 21), 1, 0f),
                            metalResist = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "metalresist", 15), 1, 0f),
                            woodResist = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "woodresist", 16), 1, 0f),
                            waterResist = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "waterresist", 17), 1, 0f),
                            fireResist = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "fireresist", 18), 1, 0f),
                            earthResist = CsvParserHelper.ParseLevelValue(GetColRaw(tokens, colMap, "earthresist", 19), 1, 0f)
                        };

                        attributes[attribId] = data;
                    }
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcAttributeDatabase] ❌ Lỗi đọc NpcAttribute.csv: {ex.Message}");
            }
        }

        private static string GetColRaw(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < tokens.Length) return tokens[idx];
            return fallbackIndex < tokens.Length ? tokens[fallbackIndex] : "";
        }

        private static string GetColString(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            return GetColRaw(tokens, colMap, key, fallbackIndex).Trim();
        }

        private static int GetColInt(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, int def = 0)
        {
            string raw = GetColRaw(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseInt(raw, def);
        }

        private static float GetColFloat(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, float def = 0f)
        {
            string raw = GetColRaw(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseFloat(raw, def);
        }

        public static NpcAttributeData GetAttribute(int attribId)
        {
            if (attribId <= 0) return null;
            Instance.EnsureLoaded();
            Instance.attributes.TryGetValue(attribId, out NpcAttributeData data);
            return data;
        }

        public static bool HasAttribute(int attribId)
        {
            return attribId > 0 && Instance.attributes.ContainsKey(attribId);
        }
    }
}
