using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    public class NpcResDatabase : ICsvTable
    {
        private static NpcResDatabase instance;
        public static NpcResDatabase Instance => instance ?? (instance = new NpcResDatabase());

        private readonly Dictionary<int, NpcResData> resDict = new Dictionary<int, NpcResData>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || resDict.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            resDict.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = GameDataPaths.NpcResCsv;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[NpcResDatabase] ⚠️ Không tìm thấy file tại: {filePath}");
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

                        int resId = GetColInt(tokens, colMap, "npcresid", 0);
                        if (resId <= 0) continue;

                        string resFile = GetColString(tokens, colMap, "npcresfile", 1);
                        string desc = GetColString(tokens, colMap, "desc", 3);
                        int runSound = GetColInt(tokens, colMap, "runsoundid", 4);
                        int deathSound = GetColInt(tokens, colMap, "deathsoundid", 5);
                        int hitSound = GetColInt(tokens, colMap, "hitsoundid", 6);
                        float rawHeight = GetColFloat(tokens, colMap, "height", 7, 1.8f);
                        float rawWidth = GetColFloat(tokens, colMap, "width", 8, 0.5f);
                        
                        float height = rawHeight > 0f ? (rawHeight > 20f ? rawHeight / 100f : rawHeight) : 1.8f;
                        float width = rawWidth > 0f ? (rawWidth > 10f ? rawWidth / 100f : rawWidth) : 0.5f;

                        NpcResData data = new NpcResData
                        {
                            resId = resId,
                            resFile = resFile,
                            desc = desc,
                            runSoundId = runSound,
                            deathSoundId = deathSound,
                            hitSoundId = hitSound,
                            height = height > 0f ? height : 1.8f,
                            width = width > 0f ? width : 0.5f
                        };

                        foreach (var kvp in colMap)
                        {
                            string colName = kvp.Key;
                            if (colName.EndsWith("frame", StringComparison.OrdinalIgnoreCase))
                            {
                                string action = colName.Substring(0, colName.Length - 5);
                                int frameVal = GetColInt(tokens, colMap, colName, -1, 0);
                                if (frameVal > 0) data.ActionFrames[action] = frameVal;
                            }
                            else if (colName.EndsWith("cross", StringComparison.OrdinalIgnoreCase))
                            {
                                string action = colName.Substring(0, colName.Length - 5);
                                float crossVal = GetColFloat(tokens, colMap, colName, -1, 0f);
                                if (crossVal > 0f) data.ActionCrossFades[action] = crossVal;
                            }
                        }

                        resDict[resId] = data;
                    }
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcResDatabase] ❌ Lỗi đọc NpcRes.csv: {ex.Message}");
            }
        }

        private static string GetColString(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < tokens.Length) return tokens[idx].Trim();
            return fallbackIndex < tokens.Length ? tokens[fallbackIndex].Trim() : "";
        }

        private static int GetColInt(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, int def = 0)
        {
            string raw = GetColString(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseInt(raw, def);
        }

        private static float GetColFloat(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, float def = 0f)
        {
            string raw = GetColString(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseFloat(raw, def);
        }

        public static NpcResData GetRes(int resId)
        {
            if (resId <= 0) return null;
            Instance.EnsureLoaded();
            Instance.resDict.TryGetValue(resId, out NpcResData data);
            return data;
        }

        public static NpcResData GetResByName(string goName)
        {
            if (string.IsNullOrEmpty(goName)) return null;
            Instance.EnsureLoaded();
            string cleanName = goName.Replace("(Clone)", "").Trim();
            foreach (var kvp in Instance.resDict)
            {
                if (!string.IsNullOrEmpty(kvp.Value.resFile) && kvp.Value.resFile.IndexOf(cleanName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        public static bool HasRes(int resId)
        {
            return resId > 0 && Instance.resDict.ContainsKey(resId);
        }

        public static GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            string cleanPath = path.Replace("\\", "/").Trim();
            if (cleanPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(0, cleanPath.Length - 7);
            }

            GameObject prefab = Resources.Load<GameObject>(cleanPath);
            if (prefab != null) return prefab;

            if (cleanPath.StartsWith("Player/", StringComparison.OrdinalIgnoreCase))
            {
                string altPath = "Players/" + cleanPath.Substring(7);
                prefab = Resources.Load<GameObject>(altPath);
                if (prefab != null) return prefab;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            prefab = Resources.Load<GameObject>($"Players/Npcs/Prefabs/{fileName}");
            if (prefab != null) return prefab;

            prefab = Resources.Load<GameObject>($"Player/Npcs/Prefabs/{fileName}");
            return prefab;
        }
    }
}
