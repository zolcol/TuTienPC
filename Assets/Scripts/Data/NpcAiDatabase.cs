using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Combat;

namespace TopDownGame.Data
{
    public class NpcAiDatabase : ICsvTable
    {
        private static NpcAiDatabase instance;
        public static NpcAiDatabase Instance => instance ?? (instance = new NpcAiDatabase());

        private readonly Dictionary<string, NpcAiData> aiProfiles = new Dictionary<string, NpcAiData>(StringComparer.OrdinalIgnoreCase);

        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || aiProfiles.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            aiProfiles.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            // Đăng ký profile fallback mặc định
            aiProfiles["commonactive"] = NpcAiData.CreateDefaultActive();
            aiProfiles["commonpassive"] = NpcAiData.CreateDefaultPassive();

            string dirPath = Path.Combine(Application.dataPath, "Settings", "N", "AI");
            if (!Directory.Exists(dirPath))
            {
                Debug.LogWarning($"[NpcAiDatabase] ⚠️ Không tìm thấy thư mục: {dirPath}");
                IsLoaded = true;
                return;
            }

            try
            {
                string[] files = Directory.GetFiles(dirPath, "*.ini", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    ParseIniFile(file);
                }

                IsLoaded = true;
                Debug.Log($"[NpcAiDatabase] ✅ Đã nạp {aiProfiles.Count} cấu hình AI từ: {dirPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcAiDatabase] ❌ Lỗi khi đọc file AI INI: {ex.Message}");
            }
        }

        private void ParseIniFile(string filePath)
        {
            try
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string fileName = Path.GetFileName(filePath);

                var data = new NpcAiData
                {
                    fileName = fileNameWithoutExt
                };

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                bool inBaseSection = false;

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        string section = line.Substring(1, line.Length - 2).Trim();
                        inBaseSection = string.Equals(section, "Base", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (!inBaseSection) continue;

                    int eqIdx = line.IndexOf('=');
                    if (eqIdx <= 0) continue;

                    string key = line.Substring(0, eqIdx).Trim().ToLowerInvariant();
                    string val = line.Substring(eqIdx + 1).Trim();

                    switch (key)
                    {
                        case "attack":
                            data.isAggressive = CsvParserHelper.ParseInt(val, 1) == 1;
                            break;
                        case "strikeback":
                            data.canStrikeBack = CsvParserHelper.ParseInt(val, 1) == 1;
                            break;
                        case "randmonmove":
                        case "randommove":
                            data.wanderChance = CsvParserHelper.ParseInt(val, 0);
                            break;
                        case "fleehpprecent":
                        case "fleehpprecentage":
                        case "fleehppercent":
                            data.fleeHpPercent = CsvParserHelper.ParseFloat(val, 0f);
                            break;
                        case "fleenearrate":
                            data.fleeNearRate = CsvParserHelper.ParseFloat(val, 0f);
                            break;
                        case "aibreathtime":
                            int breathFrames = CsvParserHelper.ParseInt(val, 15);
                            data.breathTimeSec = Mathf.Max(0.1f, breathFrames / 15.0f);
                            break;
                        case "changetargettime":
                            int lockFrames = CsvParserHelper.ParseInt(val, 60);
                            data.lockDuration = Mathf.Max(0.5f, lockFrames / 15.0f);
                            break;
                        case "selecttarget":
                            data.selectTarget = ParseSelectTarget(val);
                            break;
                    }
                }

                aiProfiles[fileNameWithoutExt] = data;
                aiProfiles[fileName] = data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcAiDatabase] ❌ Lỗi đọc file INI {filePath}: {ex.Message}");
            }
        }

        private static AiTargetSelectType ParseSelectTarget(string val)
        {
            if (string.IsNullOrEmpty(val)) return AiTargetSelectType.StrikeBack;
            val = val.Trim().ToLowerInvariant();

            switch (val)
            {
                case "strikeback":
                    return AiTargetSelectType.StrikeBack;
                case "nearest":
                    return AiTargetSelectType.Nearest;
                case "poorest":
                    return AiTargetSelectType.Poorest;
                case "richest":
                    return AiTargetSelectType.Richest;
                case "random":
                    return AiTargetSelectType.Random;
                case "player":
                    return AiTargetSelectType.Player;
                default:
                    return AiTargetSelectType.StrikeBack;
            }
        }

        public static NpcAiData GetAi(string aiKey)
        {
            Instance.EnsureLoaded();

            if (string.IsNullOrWhiteSpace(aiKey))
            {
                return Instance.aiProfiles.TryGetValue("commonactive", out var def) ? def : NpcAiData.CreateDefaultActive();
            }

            // Chuẩn hóa đường dẫn: Setting/Npc/Ai/CommonActive.ini -> CommonActive
            string cleanKey = Path.GetFileNameWithoutExtension(aiKey).Trim();
            if (Instance.aiProfiles.TryGetValue(cleanKey, out var data))
            {
                return data;
            }

            string cleanKeyWithExt = Path.GetFileName(aiKey).Trim();
            if (Instance.aiProfiles.TryGetValue(cleanKeyWithExt, out data))
            {
                return data;
            }

            return Instance.aiProfiles.TryGetValue("commonactive", out var fallback) ? fallback : NpcAiData.CreateDefaultActive();
        }
    }
}
