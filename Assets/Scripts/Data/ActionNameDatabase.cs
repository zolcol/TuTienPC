using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    /// <summary>
    /// Database quản lý bảng tra cứu tên hoạt ảnh và biến thể cưỡi thú từ ActionName.csv
    /// </summary>
    public class ActionNameDatabase : ICsvTable
    {
        private static ActionNameDatabase instance;
        public static ActionNameDatabase Instance => instance ?? (instance = new ActionNameDatabase());

        private readonly Dictionary<int, ActionNameData> actions = new Dictionary<int, ActionNameData>();
        private readonly Dictionary<string, int> clipToIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || actions.Count == 0)
            {
                Load();
            }
        }

        public void Load()
        {
            LoadData();
        }

        public void Clear()
        {
            ClearCache();
        }

        public void ClearCache()
        {
            actions.Clear();
            clipToIdMap.Clear();
            IsLoaded = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            instance = null;
        }

        private void LoadData()
        {
            ClearCache();
            string filePath = GameDataPaths.ActionNameCsv;

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[ActionNameDatabase] ⚠️ Không tìm thấy file tại: {filePath}");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length <= 1)
                {
                    IsLoaded = true;
                    return;
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                    string[] tokens = CsvParserHelper.SplitCsvLine(line);
                    if (tokens.Length < 2) continue;

                    int actId = CsvParserHelper.ParseInt(tokens[0]);
                    if (actId <= 0) continue;

                    ActionNameData data = new ActionNameData
                    {
                        actId = actId,
                        actName = CsvParserHelper.GetToken(tokens, 1),
                        rideActNameDefault = CsvParserHelper.GetToken(tokens, 5),
                        hidePart = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 64)),
                        rideHidePart = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 65)),
                        description = CsvParserHelper.GetToken(tokens, 66)
                    };

                    // Parse ActName1..3
                    for (int v = 0; v < 3; v++)
                    {
                        data.actVariants[v] = CsvParserHelper.GetToken(tokens, 2 + v);
                    }

                    // Parse RideActName1..58 (cột 6 đến 63)
                    for (int r = 0; r < 58; r++)
                    {
                        data.rideActNames[r] = CsvParserHelper.GetToken(tokens, 6 + r);
                    }

                    actions[actId] = data;

                    // Lưu vào bảng tra ngược clipName -> actId
                    if (!string.IsNullOrEmpty(data.actName) && !clipToIdMap.ContainsKey(data.actName))
                    {
                        clipToIdMap[data.actName] = actId;
                    }
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActionNameDatabase] ❌ Lỗi đọc ActionName.csv: {ex.Message}");
            }
        }

        public static ActionNameData GetAction(int actId)
        {
            if (actId <= 0) return null;
            Instance.EnsureLoaded();
            Instance.actions.TryGetValue(actId, out var data);
            return data;
        }

        public static string GetClipName(int actId, int mountId = 0, int variantIndex = 0)
        {
            if (actId <= 0) return string.Empty;
            Instance.EnsureLoaded();
            if (Instance.actions.TryGetValue(actId, out var data))
            {
                return data.GetClipName(mountId, variantIndex);
            }
            return string.Empty;
        }

        public static int GetActId(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return 0;
            Instance.EnsureLoaded();
            if (Instance.clipToIdMap.TryGetValue(clipName, out int actId))
            {
                return actId;
            }
            return 0;
        }

        public static int GetHidePart(int actId, bool isRiding)
        {
            var data = GetAction(actId);
            if (data == null) return 0;
            return isRiding ? data.rideHidePart : data.hidePart;
        }
    }
}
