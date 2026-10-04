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

    public class StateEffectDatabase : ICsvTable
    {
        private static StateEffectDatabase instance;
        public static StateEffectDatabase Instance => instance ?? (instance = new StateEffectDatabase());

        private readonly Dictionary<int, StateEffectData> dataDict = new Dictionary<int, StateEffectData>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || dataDict.Count == 0)
            {
                Load();
            }
        }

        public static void EnsureLoadedStatic() => Instance.EnsureLoaded();

        public void LoadDatabase() => Load();

        public void Clear()
        {
            dataDict.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();
            EffectDatabase.Instance.EnsureLoaded();

            string filePath = GameDataPaths.StateEffectCsv;
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

                    IsLoaded = true;
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[StateEffectDatabase] ❌ Lỗi đọc StateEffect.csv: {ex.Message}");
                }
            }

            TextAsset csvFile = Resources.Load<TextAsset>("CSV/N/StateEffect");
            if (csvFile != null)
            {
                string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
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
                IsLoaded = true;
            }
        }

        public static void Reload()
        {
            Instance.Load();
        }

        public static StateEffectData GetStateEffect(int id)
        {
            if (id <= 0) return null;
            Instance.EnsureLoaded();
            return Instance.dataDict.TryGetValue(id, out var data) ? data : null;
        }

        public static bool HasStateEffect(int id)
        {
            if (id <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.dataDict.ContainsKey(id);
        }
    }
}
