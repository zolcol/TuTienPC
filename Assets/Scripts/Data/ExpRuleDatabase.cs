using System;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    public class ExpRuleDatabase : ICsvTable
    {
        private static ExpRuleDatabase instance;
        public static ExpRuleDatabase Instance => instance ?? (instance = new ExpRuleDatabase());

        // Ma trận tỉ lệ: matrix[playerLevel, monsterLevel] -> int % (ví dụ: 100)
        private int[,] expMatrix;
        private int maxPlayerLevel = 0;
        private int maxMonsterLevel = 0;

        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || expMatrix == null)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            expMatrix = null;
            maxPlayerLevel = 0;
            maxMonsterLevel = 0;
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = GameDataPaths.ExpRuleCsv;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[ExpRuleDatabase] ⚠️ Không tìm thấy file: {filePath}");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length < 2) return;

                // Header chứa các monster level (cột 0 là header của player level, các cột sau 1..N là monster level 0..400)
                string[] headerTokens = CsvParserHelper.SplitCsvLine(lines[0]);
                int columnCount = headerTokens.Length;

                // Dự trù kích thước ma trận an toàn (500x500)
                const int MAX_DIM = 500;
                expMatrix = new int[MAX_DIM, MAX_DIM];

                int loadedRows = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] tokens = CsvParserHelper.SplitCsvLine(line);
                    if (tokens.Length < 2) continue;

                    int playerLvl = CsvParserHelper.ParseInt(tokens[0]);
                    if (playerLvl < 0 || playerLvl >= MAX_DIM) continue;

                    if (playerLvl > maxPlayerLevel) maxPlayerLevel = playerLvl;

                    for (int col = 1; col < tokens.Length && col < columnCount; col++)
                    {
                        int monsterLvl = CsvParserHelper.ParseInt(headerTokens[col]);
                        if (monsterLvl < 0 || monsterLvl >= MAX_DIM) continue;

                        int percent = CsvParserHelper.ParseInt(tokens[col], 100);
                        expMatrix[playerLvl, monsterLvl] = percent;

                        if (monsterLvl > maxMonsterLevel) maxMonsterLevel = monsterLvl;
                    }

                    loadedRows++;
                }

                IsLoaded = true;
                Debug.Log($"[ExpRuleDatabase] ✅ Đã nạp ma trận ExpRule ({loadedRows} hàng Player Lv, Max Monster Lv: {maxMonsterLevel}).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExpRuleDatabase] ❌ Lỗi nạp {filePath}: {ex.Message}");
            }
        }

        public int GetExpPercent(int playerLevel, int monsterLevel)
        {
            EnsureLoaded();
            if (expMatrix == null) return 100;

            int clampedPlayer = Mathf.Clamp(playerLevel, 1, maxPlayerLevel > 0 ? maxPlayerLevel : 400);
            int clampedMonster = Mathf.Clamp(monsterLevel, 0, maxMonsterLevel > 0 ? maxMonsterLevel : 400);

            if (clampedPlayer < expMatrix.GetLength(0) && clampedMonster < expMatrix.GetLength(1))
            {
                int val = expMatrix[clampedPlayer, clampedMonster];
                return val > 0 ? val : 100;
            }

            return 100;
        }

        /// <summary>
        /// Tính toán EXP thực nhận khi người chơi hạ gục quái theo chuẩn game:
        /// EXP = Player.BaseAwardExp * (ExpRule[PlayerLevel, MonsterLevel] / 100)
        /// BaseAwardExp được lấy theo cấp độ của người chơi (PlayerLevel) từ PlayerLevel.csv
        /// </summary>
        public long CalculateExpReward(int playerLevel, int monsterLevel)
        {
            // Lấy BaseAwardExp theo cấp độ của Người chơi (PlayerLevel)
            var playerLevelData = PlayerLevelDatabase.Instance.Get(playerLevel);
            int baseAward = playerLevelData != null && playerLevelData.BaseAwardExp > 0 ? playerLevelData.BaseAwardExp : 1000;
            
            // Lấy tỉ lệ hưởng % dựa trên chênh lệch cấp độ giữa Player và Quái
            int percent = GetExpPercent(playerLevel, monsterLevel);

            return (long)Mathf.RoundToInt(baseAward * (percent / 100.0f));
        }
    }
}
