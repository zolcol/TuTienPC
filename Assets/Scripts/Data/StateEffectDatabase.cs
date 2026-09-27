using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    public class StateEffectData
    {
        public int id;
        public string name;
        public int effectResId1;
        public int slotId1;
        public int effectResId2;
        public int slotId2;
        public int headResId;
        public string headResPath => EffectDatabase.GetEffectPath(headResId);
        public string effectPath1 => EffectDatabase.GetEffectPath(effectResId1);
        public string effectPath2 => EffectDatabase.GetEffectPath(effectResId2);
    }

    public class StateEffectDatabase
    {
        private static readonly Dictionary<int, StateEffectData> dataDict = new Dictionary<int, StateEffectData>();
        private static bool isLoaded = false;

        public static void EnsureLoaded()
        {
            if (isLoaded && dataDict.Count > 0) return;
            LoadFromCsv();
            isLoaded = true;
        }

        public static void Reload()
        {
            isLoaded = false;
            dataDict.Clear();
            EnsureLoaded();
        }

        private static void LoadFromCsv()
        {
            dataDict.Clear();
            EffectDatabase.Instance.EnsureLoaded();

            string filePath = Path.Combine(Application.dataPath, "Settings", "N", "StateEffect.csv");
            if (File.Exists(filePath))
            {
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
                            if (tokens.Length < 2) continue;

                            int id = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 0), -1);
                            if (id <= 0) continue;

                            StateEffectData data = new StateEffectData
                            {
                                id = id,
                                name = CsvParserHelper.GetToken(tokens, 1),
                                effectResId1 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 2), 0),
                                slotId1 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 3), 0),
                                effectResId2 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 4), 0),
                                slotId2 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 5), 0),
                                headResId = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 6), 0)
                            };

                            dataDict[id] = data;
                        }
                    }

                    // Debug.Log($"✅ <color=cyan>[StateEffectDatabase]</color> Đã nạp thành công <b>{dataDict.Count}</b> cấu hình Buff/Debuff từ Settings/N/StateEffect.csv!");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[StateEffectDatabase] ❌ Lỗi đọc StateEffect.csv: {ex.Message}");
                }
            }

            // Fallback: nếu không tìm thấy file trực tiếp trong Settings/N
            TextAsset csvFile = Resources.Load<TextAsset>("CSV/N/StateEffect");
            if (csvFile != null)
            {
                string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] tokens = CsvParserHelper.SplitCsvLine(lines[i]);
                    if (tokens.Length < 2) continue;

                    int id = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 0), -1);
                    if (id <= 0) continue;

                    StateEffectData data = new StateEffectData
                    {
                        id = id,
                        name = CsvParserHelper.GetToken(tokens, 1),
                        effectResId1 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 2), 0),
                        slotId1 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 3), 0),
                        effectResId2 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 4), 0),
                        slotId2 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 5), 0),
                        headResId = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 6), 0)
                    };

                    dataDict[id] = data;
                }
                // Debug.Log($"✅ [StateEffectDatabase] Đã nạp {dataDict.Count} cấu hình từ Resources fallback.");
            }
            else
            {
                Debug.LogWarning($"[StateEffectDatabase] ⚠️ Không tìm thấy StateEffect.csv tại: {filePath}");
            }
        }

        public static StateEffectData GetStateEffect(int id)
        {
            if (id <= 0) return null;
            EnsureLoaded();
            return dataDict.TryGetValue(id, out var data) ? data : null;
        }

        public static bool HasStateEffect(int id)
        {
            if (id <= 0) return false;
            EnsureLoaded();
            return dataDict.ContainsKey(id);
        }
    }
}
