using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class NpcStatData
    {
        public int id;
        public string name;
        public float maxHp = 100f;
        public float maxMp = 0f;
        public float physicalDamage = 20f;
        public float magicDamage = 0f;
        public float armor = 0f;
        public float magicResist = 0f;
        public float attackSpeed = 15f;
        public float critChance = 5f;
        public float critMultiplier = 150f;
        public float hpRegen = 0f;
        public float mpRegen = 0f;

        public string rawMaxHp;
        public string rawMaxMp;
        public string rawPhysicalDamage;
        public string rawMagicDamage;
        public string rawArmor;
        public string rawMagicResist;
        public string rawAttackSpeed;
        public string rawCritChance;
        public string rawCritMultiplier;
        public string rawHpRegen;
        public string rawMpRegen;

        public float GetMaxHp(int level) => CsvParserHelper.ParseLevelValue(rawMaxHp, level, maxHp);
        public float GetMaxMp(int level) => CsvParserHelper.ParseLevelValue(rawMaxMp, level, maxMp);
        public float GetPhysicalDamage(int level) => CsvParserHelper.ParseLevelValue(rawPhysicalDamage, level, physicalDamage);
        public float GetMagicDamage(int level) => CsvParserHelper.ParseLevelValue(rawMagicDamage, level, magicDamage);
        public float GetArmor(int level) => CsvParserHelper.ParseLevelValue(rawArmor, level, armor);
        public float GetMagicResist(int level) => CsvParserHelper.ParseLevelValue(rawMagicResist, level, magicResist);
        public float GetAttackSpeed(int level) => CsvParserHelper.ParseLevelValue(rawAttackSpeed, level, attackSpeed);
        public float GetCritChance(int level) => CsvParserHelper.ParseLevelValue(rawCritChance, level, critChance);
        public float GetCritMultiplier(int level) => CsvParserHelper.ParseLevelValue(rawCritMultiplier, level, critMultiplier);
        public float GetHpRegen(int level) => CsvParserHelper.ParseLevelValue(rawHpRegen, level, hpRegen);
        public float GetMpRegen(int level) => CsvParserHelper.ParseLevelValue(rawMpRegen, level, mpRegen);
    }

    public class NpcStatDatabase : ICsvTable
    {
        private static NpcStatDatabase instance;
        public static NpcStatDatabase Instance => instance ?? (instance = new NpcStatDatabase());

        private readonly Dictionary<int, NpcStatData> stats = new Dictionary<int, NpcStatData>();

        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || stats.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            stats.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = GameDataPaths.NpcStatsCsv;
            if (!File.Exists(filePath))
            {
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
                        if (tokens.Length < 1) continue;

                        int id = GetColInt(tokens, colMap, "id", 0, -1);
                        if (id < 0) continue;

                        string name = GetColString(tokens, colMap, "name", 1);
                        string rawMaxHp = GetColRaw(tokens, colMap, "maxhp", 2);
                        string rawMaxMp = GetColRaw(tokens, colMap, "maxmp", 3);
                        string rawPhysDmg = GetColRaw(tokens, colMap, "physicaldamage", 4);
                        string rawMagDmg = GetColRaw(tokens, colMap, "magicdamage", 5);
                        string rawArmor = GetColRaw(tokens, colMap, "armor", 6);
                        string rawMagRes = GetColRaw(tokens, colMap, "magicresist", 7);
                        string rawAtkSpd = GetColRaw(tokens, colMap, "attackspeed", 8);
                        string rawCritRate = GetColRaw(tokens, colMap, "critchance", 9);
                        string rawCritMult = GetColRaw(tokens, colMap, "critmultiplier", 10);
                        string rawHpRegen = GetColRaw(tokens, colMap, "hpregen", 11);
                        string rawMpRegen = GetColRaw(tokens, colMap, "mpregen", 12);

                        // Cảnh báo nếu các thuộc tính cốt lõi bị bỏ trống
                        var missingCols = new List<string>();
                        if (string.IsNullOrWhiteSpace(rawMaxHp)) missingCols.Add("MaxHp");
                        if (string.IsNullOrWhiteSpace(rawPhysDmg)) missingCols.Add("PhysicalDamage");
                        if (string.IsNullOrWhiteSpace(rawAtkSpd)) missingCols.Add("AttackSpeed");

                        if (missingCols.Count > 0)
                        {
                            Debug.LogWarning($"[NpcStatDatabase] ⚠️ [Id={id} ({name})] Cột quan trọng bị bỏ trống ({string.Join(", ", missingCols)}). Hệ thống sẽ dùng chỉ số mặc định.");
                        }

                        NpcStatData data = new NpcStatData
                        {
                            id = id,
                            name = name,
                            maxHp = CsvParserHelper.ParseLevelValue(rawMaxHp, 1, 100f),
                            maxMp = CsvParserHelper.ParseLevelValue(rawMaxMp, 1, 0f),
                            physicalDamage = CsvParserHelper.ParseLevelValue(rawPhysDmg, 1, 20f),
                            magicDamage = CsvParserHelper.ParseLevelValue(rawMagDmg, 1, 0f),
                            armor = CsvParserHelper.ParseLevelValue(rawArmor, 1, 0f),
                            magicResist = CsvParserHelper.ParseLevelValue(rawMagRes, 1, 0f),
                            attackSpeed = CsvParserHelper.ParseLevelValue(rawAtkSpd, 1, 15f),
                            critChance = CsvParserHelper.ParseLevelValue(rawCritRate, 1, 5f),
                            critMultiplier = CsvParserHelper.ParseLevelValue(rawCritMult, 1, 150f),
                            hpRegen = CsvParserHelper.ParseLevelValue(rawHpRegen, 1, 0f),
                            mpRegen = CsvParserHelper.ParseLevelValue(rawMpRegen, 1, 0f),

                            rawMaxHp = rawMaxHp,
                            rawMaxMp = rawMaxMp,
                            rawPhysicalDamage = rawPhysDmg,
                            rawMagicDamage = rawMagDmg,
                            rawArmor = rawArmor,
                            rawMagicResist = rawMagRes,
                            rawAttackSpeed = rawAtkSpd,
                            rawCritChance = rawCritRate,
                            rawCritMultiplier = rawCritMult,
                            rawHpRegen = rawHpRegen,
                            rawMpRegen = rawMpRegen
                        };

                        stats[id] = data;
                    }
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcStatDatabase] ❌ Lỗi đọc NpcStats.csv: {ex.Message}");
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

        public static NpcStatData GetStats(int id)
        {
            if (id < 0) return null;
            Instance.EnsureLoaded();
            if (Instance.stats.TryGetValue(id, out NpcStatData data))
            {
                return data;
            }

            Debug.LogWarning($"[NpcStatDatabase] ⚠️ Không tìm thấy chỉ số cho Id = {id} trong NpcStats.csv!");
            return null;
        }

        public static bool HasStats(int id)
        {
            return id >= 0 && Instance.stats.ContainsKey(id);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            instance = null;
        }
    }
}
