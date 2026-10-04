using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    public class FloatingTextDatabase : ICsvTable
    {
        private static FloatingTextDatabase instance;
        public static FloatingTextDatabase Instance => instance ?? (instance = new FloatingTextDatabase());

        private readonly Dictionary<int, FloatingTextResData> itemsById = new Dictionary<int, FloatingTextResData>();
        private readonly Dictionary<string, FloatingTextResData> itemsByName = new Dictionary<string, FloatingTextResData>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<FloatingTextType, FloatingTextResData> itemsByType = new Dictionary<FloatingTextType, FloatingTextResData>();

        private FloatingTextResData defaultData;

        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || itemsById.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            itemsById.Clear();
            itemsByName.Clear();
            itemsByType.Clear();
            defaultData = null;
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = GameDataPaths.FloatingTextCsv;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FloatingTextDatabase] ⚠️ Không tìm thấy file tại: {filePath}. Tạo fallback data mặc định.");
                CreateFallbackDefaults();
                IsLoaded = true;
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
                        if (tokens.Length < 2) continue;

                        int id = CsvParserHelper.ParseInt(tokens[0]);
                        if (id <= 0) continue;

                        string typeName = CsvParserHelper.GetToken(tokens, 1);
                        float duration = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 2), 0.85f);
                        float startScale = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 3), 0.7f);
                        float peakScale = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 4), 1.35f);
                        float endScale = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 5), 0.9f);
                        float popDuration = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 6), 0.10f);
                        float fadeStartTime = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 7), 0.45f);
                        string colorHex = CsvParserHelper.GetToken(tokens, 8, "#FFFFFF");
                        string outlineColorHex = CsvParserHelper.GetToken(tokens, 9, "#000000");
                        float outlineWidth = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 10), 0.22f);
                        float fontSize = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 11), 4.2f);
                        float vx = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 12), 0f);
                        float vy = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 13), 2.2f);
                        float vz = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 14), 0f);
                        float gravity = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 15), 2.0f);
                        float jitter = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 16), 0.35f);
                        float arcSpread = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 17), 0.6f);
                        string prefix = CsvParserHelper.GetToken(tokens, 18);
                        string suffix = CsvParserHelper.GetToken(tokens, 19);
                        bool isBold = CsvParserHelper.ParseBool(CsvParserHelper.GetToken(tokens, 20), true);
                        string desc = CsvParserHelper.GetToken(tokens, 21);

                        FloatingTextResData data = new FloatingTextResData
                        {
                            id = id,
                            typeName = typeName,
                            duration = duration,
                            startScale = startScale,
                            peakScale = peakScale,
                            endScale = endScale,
                            popDuration = popDuration,
                            fadeStartTime = fadeStartTime,
                            colorHex = colorHex,
                            outlineColorHex = outlineColorHex,
                            outlineWidth = outlineWidth,
                            fontSize = fontSize,
                            moveVelocity = new Vector3(vx, vy, vz),
                            gravity = gravity,
                            randomJitter = jitter,
                            arcSpread = arcSpread,
                            prefix = prefix,
                            suffix = suffix,
                            isBold = isBold,
                            desc = desc
                        };

                        itemsById[id] = data;

                        if (!string.IsNullOrEmpty(typeName))
                        {
                            itemsByName[typeName] = data;
                            if (Enum.TryParse<FloatingTextType>(typeName, true, out var parsedType))
                            {
                                itemsByType[parsedType] = data;
                            }
                        }
                    }
                }

                if (itemsById.TryGetValue(1, out var def))
                {
                    defaultData = def;
                }

                IsLoaded = true;
                Debug.Log($"[FloatingTextDatabase] ✅ Đã nạp thành công {itemsById.Count} cấu hình Floating Text từ CSV.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FloatingTextDatabase] ❌ Lỗi khi đọc file {filePath}: {ex.Message}");
                CreateFallbackDefaults();
                IsLoaded = true;
            }
        }

        private void CreateFallbackDefaults()
        {
            defaultData = new FloatingTextResData
            {
                id = 1,
                typeName = "NormalDamage",
                duration = 0.85f,
                startScale = 0.7f,
                peakScale = 1.35f,
                endScale = 0.9f,
                popDuration = 0.10f,
                fadeStartTime = 0.45f,
                colorHex = "#FFF3D0",
                outlineColorHex = "#1A1A1A",
                outlineWidth = 0.22f,
                fontSize = 4.2f,
                moveVelocity = new Vector3(0f, 2.2f, 0f),
                gravity = 2.0f,
                randomJitter = 0.35f,
                arcSpread = 0.6f,
                prefix = "-",
                suffix = "",
                isBold = true,
                desc = "Fallback Normal Damage"
            };
            itemsById[1] = defaultData;
            itemsByName["NormalDamage"] = defaultData;
            itemsByType[FloatingTextType.NormalDamage] = defaultData;
        }

        public FloatingTextResData Get(int id)
        {
            EnsureLoaded();
            if (itemsById.TryGetValue(id, out var data)) return data;
            return defaultData;
        }

        public FloatingTextResData Get(string typeName)
        {
            EnsureLoaded();
            if (!string.IsNullOrEmpty(typeName) && itemsByName.TryGetValue(typeName, out var data)) return data;
            return defaultData;
        }

        public FloatingTextResData Get(FloatingTextType type)
        {
            EnsureLoaded();
            if (itemsByType.TryGetValue(type, out var data)) return data;
            return defaultData;
        }
    }
}
