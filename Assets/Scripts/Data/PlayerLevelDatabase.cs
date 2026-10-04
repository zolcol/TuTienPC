using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    public class PlayerLevelDatabase : ICsvTable
    {
        private static PlayerLevelDatabase instance;
        public static PlayerLevelDatabase Instance => instance ?? (instance = new PlayerLevelDatabase());

        private readonly Dictionary<int, PlayerLevelData> levels = new Dictionary<int, PlayerLevelData>();
        private int maxLevel = 1;

        public bool IsLoaded { get; private set; }
        public int MaxLevel => maxLevel;

        public void EnsureLoaded()
        {
            if (!IsLoaded || levels.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            levels.Clear();
            maxLevel = 1;
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = GameDataPaths.PlayerLevelCsv;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[PlayerLevelDatabase] ⚠️ Không tìm thấy file: {filePath}");
                return;
            }

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] tokens = CsvParserHelper.SplitCsvLine(line);
                        if (tokens.Length < 3) continue;

                        int lvl = CsvParserHelper.ParseInt(tokens[0]);
                        if (lvl <= 0) continue;

                        long expUpgrade = CsvParserHelper.ParseLong(CsvParserHelper.GetToken(tokens, 1));
                        int baseAwardExp = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 2));
                        int runSpeed = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 3));
                        int attackSpeed = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 4));
                        int fightPower = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 5));
                        int resist = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 6));

                        var data = new PlayerLevelData
                        {
                            Level = lvl,
                            ExpUpGrade = expUpgrade,
                            BaseAwardExp = baseAwardExp,
                            RunSpeed = runSpeed,
                            AttackSpeed = attackSpeed,
                            FightPower = fightPower,
                            AttackSeriesResist = resist
                        };

                        levels[lvl] = data;
                        if (lvl > maxLevel) maxLevel = lvl;
                    }
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerLevelDatabase] ❌ Lỗi nạp {filePath}: {ex.Message}");
            }
        }

        public PlayerLevelData Get(int level)
        {
            EnsureLoaded();
            if (levels.TryGetValue(level, out var data)) return data;
            return null;
        }
    }
}
