using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    /// <summary>
    /// Database nạp cấu hình đường cong chuyển động chữ/số nhảy từ FlyChar.csv
    /// </summary>
    public class FlyCharDatabase : ICsvTable
    {
        private static FlyCharDatabase instance;
        public static FlyCharDatabase Instance => instance ?? (instance = new FlyCharDatabase());

        private readonly Dictionary<FlyCharType, FlyCharResData> itemsByType = new Dictionary<FlyCharType, FlyCharResData>();
        private readonly Dictionary<string, FlyCharResData> itemsByName = new Dictionary<string, FlyCharResData>(StringComparer.OrdinalIgnoreCase);
        private FlyCharResData fallbackData;

        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || itemsByType.Count == 0)
            {
                Load();
            }
        }

        public static void EnsureLoadedStatic() => Instance.EnsureLoaded();
        public void LoadDatabase() => Load();

        public void Clear()
        {
            itemsByType.Clear();
            itemsByName.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();
            CreateFallbackData();

            string filePath = GameDataPaths.FlyCharCsv;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FlyCharDatabase] ⚠️ Không tìm thấy file tại: {filePath}. Sử dụng fallback.");
                IsLoaded = true;
                return;
            }

            try
            {
                using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
                {
                    string headerLine = reader.ReadLine(); // Type,Scale,Alpha,Offset,Angle
                    if (string.IsNullOrEmpty(headerLine)) return;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] row = CsvParserHelper.SplitCsvLine(line);
                        if (row.Length < 5) continue;

                        string typeStr = row[0].Trim();
                        string scaleStr = row[1].Trim();
                        string alphaStr = row[2].Trim();
                        string offsetStr = row[3].Trim();
                        string angleStr = row[4].Trim();

                        FlyCharType charType = ParseType(typeStr);

                        FlyCharResData data = new FlyCharResData
                        {
                            type = charType,
                            typeName = typeStr,
                            scaleCurve = ParseAnimationCurve(scaleStr),
                            alphaCurve = ParseAnimationCurve(alphaStr),
                            offsetCurve = ParseAnimationCurve(offsetStr),
                            angleList = ParseVectorList(angleStr),
                            offsetList = ParseVectorList(offsetStr)
                        };

                        // Tính thời lượng tồn tại theo keyframe lớn nhất
                        float maxDur = 0.8f;
                        if (data.scaleCurve.length > 0)
                            maxDur = Mathf.Max(maxDur, data.scaleCurve.keys[data.scaleCurve.length - 1].time);
                        if (data.alphaCurve.length > 0)
                            maxDur = Mathf.Max(maxDur, data.alphaCurve.keys[data.alphaCurve.length - 1].time);
                        if (data.offsetCurve.length > 0)
                            maxDur = Mathf.Max(maxDur, data.offsetCurve.keys[data.offsetCurve.length - 1].time);

                        data.duration = maxDur;

                        // Áp dụng Visual Style mặc định theo từng loại
                        ApplyVisualStyle(data);

                        // Ghi đè chỉ số từ các cột mở rộng trong CSV (nếu có)
                        if (row.Length > 5 && !string.IsNullOrEmpty(row[5]) && ColorUtility.TryParseHtmlString(row[5].Trim(), out Color c))
                        {
                            data.color = c;
                        }
                        if (row.Length > 6 && !string.IsNullOrEmpty(row[6]) && ColorUtility.TryParseHtmlString(row[6].Trim(), out Color oc))
                        {
                            data.outlineColor = oc;
                        }
                        if (row.Length > 7 && float.TryParse(row[7].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ow))
                        {
                            data.outlineWidth = ow;
                        }
                        if (row.Length > 8 && float.TryParse(row[8].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fs))
                        {
                            data.fontSize = fs;
                        }
                        if (row.Length > 9 && !string.IsNullOrEmpty(row[9]))
                        {
                            data.prefix = row[9];
                        }
                        if (row.Length > 10 && !string.IsNullOrEmpty(row[10]))
                        {
                            data.suffix = row[10];
                        }

                        itemsByType[charType] = data;
                        itemsByName[typeStr] = data;
                    }
                }

                IsLoaded = true;
                Debug.Log($"[FlyCharDatabase] ✅ Đã nạp thành công {itemsByType.Count} cấu hình FlyChar từ CSV.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlyCharDatabase] ❌ Lỗi khi đọc file {filePath}: {ex.Message}");
                IsLoaded = true;
            }
        }

        private static FlyCharType ParseType(string typeStr)
        {
            if (string.Equals(typeStr, "HIT_NORMAL", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HitNormal;
            if (string.Equals(typeStr, "HIT_DEADLY", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HitDeadly;
            if (string.Equals(typeStr, "HIT_MISS", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HitMiss;
            if (string.Equals(typeStr, "HURT_NORMAL", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HurtNormal;
            if (string.Equals(typeStr, "HURT_DEADLY", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HurtDeadly;
            if (string.Equals(typeStr, "HURT_MISS", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HurtMiss;
            if (string.Equals(typeStr, "TREATMENT", StringComparison.OrdinalIgnoreCase) || string.Equals(typeStr, "CURE", StringComparison.OrdinalIgnoreCase)) return FlyCharType.Treatment;
            if (string.Equals(typeStr, "ADD_EXP", StringComparison.OrdinalIgnoreCase)) return FlyCharType.AddExp;
            if (string.Equals(typeStr, "VITALITY", StringComparison.OrdinalIgnoreCase)) return FlyCharType.Vitality;
            if (string.Equals(typeStr, "STRENGTH", StringComparison.OrdinalIgnoreCase)) return FlyCharType.Strength;
            if (string.Equals(typeStr, "DEXTERITY", StringComparison.OrdinalIgnoreCase)) return FlyCharType.Dexterity;
            if (string.Equals(typeStr, "ENERGY", StringComparison.OrdinalIgnoreCase)) return FlyCharType.Energy;
            if (string.Equals(typeStr, "HIT_MISS_IGNORE", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HitMissIgnore;
            if (string.Equals(typeStr, "HURT_MISS_IGNORE", StringComparison.OrdinalIgnoreCase)) return FlyCharType.HurtMissIgnore;

            return FlyCharType.None;
        }

        private static void ApplyVisualStyle(FlyCharResData data)
        {
            switch (data.type)
            {
                case FlyCharType.HitNormal:
                    data.color = new Color(1.0f, 1.0f, 1.0f, 1.0f); // Trắng thuần sắc nét chuẩn ARPG
                    data.outlineColor = new Color(0.02f, 0.02f, 0.04f, 1.0f); // Viền đen tuyền
                    data.outlineWidth = 0.26f;
                    data.fontSize = 3.8f;
                    data.prefix = "";
                    break;

                case FlyCharType.HitDeadly:
                    data.color = new Color(1.0f, 0.80f, 0.0f, 1.0f); // Vàng kim hổ phách bạo kích sang trọng
                    data.outlineColor = new Color(0.20f, 0.04f, 0.0f, 1.0f); // Viền nâu đỏ cháy sẫm
                    data.outlineWidth = 0.32f;
                    data.fontSize = 4.6f;
                    data.prefix = "";
                    break;

                case FlyCharType.HitMiss:
                    data.color = new Color(0.72f, 0.77f, 0.84f, 1.0f); // Bạc thép khói mờ
                    data.outlineColor = new Color(0.08f, 0.10f, 0.14f, 1.0f);
                    data.outlineWidth = 0.22f;
                    data.fontSize = 3.2f;
                    data.prefix = "";
                    break;

                case FlyCharType.HurtNormal:
                    data.color = new Color(1.0f, 0.18f, 0.18f, 1.0f); // Đỏ thẫm máu cảnh báo nguy hiểm
                    data.outlineColor = new Color(0.22f, 0.0f, 0.0f, 1.0f); // Viền đỏ đen sẫm
                    data.outlineWidth = 0.26f;
                    data.fontSize = 3.8f;
                    data.prefix = "-";
                    break;

                case FlyCharType.HurtDeadly:
                    data.color = new Color(1.0f, 0.05f, 0.28f, 1.0f); // Đỏ thẫm bạo kích
                    data.outlineColor = new Color(0.18f, 0.0f, 0.05f, 1.0f);
                    data.outlineWidth = 0.32f;
                    data.fontSize = 4.6f;
                    data.prefix = "CRIT -";
                    break;

                case FlyCharType.HurtMiss:
                    data.color = new Color(0.25f, 0.82f, 1.0f, 1.0f); // Lam thiên thanh (Né đòn huyền ảo)
                    data.outlineColor = new Color(0.0f, 0.12f, 0.24f, 1.0f);
                    data.outlineWidth = 0.26f;
                    data.fontSize = 3.4f;
                    data.prefix = "";
                    break;

                case FlyCharType.Treatment:
                    data.color = new Color(0.18f, 0.95f, 0.45f, 1.0f); // Xanh ngọc lục bảo hồi máu phát sáng
                    data.outlineColor = new Color(0.0f, 0.20f, 0.08f, 1.0f);
                    data.outlineWidth = 0.26f;
                    data.fontSize = 4.0f;
                    data.prefix = "+";
                    break;

                case FlyCharType.AddExp:
                    data.color = new Color(1.0f, 0.74f, 0.05f, 1.0f); // Vàng kim EXP
                    data.outlineColor = new Color(0.24f, 0.12f, 0.0f, 1.0f);
                    data.outlineWidth = 0.26f;
                    data.fontSize = 3.6f;
                    data.prefix = "+EXP ";
                    break;

                case FlyCharType.Vitality:
                case FlyCharType.Strength:
                case FlyCharType.Dexterity:
                case FlyCharType.Energy:
                    data.color = new Color(0.72f, 0.35f, 1.0f, 1.0f); // Tím ma pháp thuộc tính
                    data.outlineColor = new Color(0.14f, 0.0f, 0.24f, 1.0f);
                    data.outlineWidth = 0.24f;
                    data.fontSize = 3.4f;
                    break;

                case FlyCharType.HitMissIgnore:
                case FlyCharType.HurtMissIgnore:
                    data.color = new Color(0.88f, 0.91f, 0.96f, 1.0f); // Bạch kim miễn nhiễm
                    data.outlineColor = new Color(0.10f, 0.13f, 0.18f, 1.0f);
                    data.outlineWidth = 0.24f;
                    data.fontSize = 3.2f;
                    break;

                default:
                    data.color = Color.white;
                    data.outlineColor = Color.black;
                    data.outlineWidth = 0.24f;
                    data.fontSize = 3.6f;
                    break;
            }
        }

        private static AnimationCurve ParseAnimationCurve(string text)
        {
            AnimationCurve curve = new AnimationCurve();
            if (string.IsNullOrEmpty(text)) return curve;

            string[] pairs = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split(',');
                if (parts.Length >= 2 &&
                    float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float time) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                {
                    curve.AddKey(time, value);
                }
            }
            return curve;
        }

        private static List<Vector2> ParseVectorList(string text)
        {
            List<Vector2> list = new List<Vector2>();
            if (string.IsNullOrEmpty(text)) return list;

            string[] pairs = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split(',');
                if (parts.Length >= 2 &&
                    float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                {
                    list.Add(new Vector2(x, y));
                }
            }
            return list;
        }

        private void CreateFallbackData()
        {
            fallbackData = new FlyCharResData
            {
                type = FlyCharType.HitNormal,
                typeName = "HIT_NORMAL",
                scaleCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 1.5f), new Keyframe(0.2f, 1.5f), new Keyframe(0.8f, 1f), new Keyframe(1f, 1f)),
                alphaCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.4f, 1f), new Keyframe(0.8f, 0f)),
                offsetCurve = new AnimationCurve(new Keyframe(0f, 10f), new Keyframe(0.1f, 80f), new Keyframe(0.3f, 80f), new Keyframe(0.8f, 190f)),
                angleList = new List<Vector2> { new Vector2(0f, 90f), new Vector2(1f, 90f), new Vector2(2f, 90f) },
                offsetList = new List<Vector2> { new Vector2(0f, 10f), new Vector2(0.1f, 80f), new Vector2(0.3f, 80f), new Vector2(0.8f, 190f) },
                duration = 0.85f
            };
            ApplyVisualStyle(fallbackData);
        }

        public FlyCharResData Get(FlyCharType type)
        {
            EnsureLoaded();
            if (itemsByType.TryGetValue(type, out var data)) return data;
            return fallbackData;
        }

        public FlyCharResData Get(string typeName)
        {
            EnsureLoaded();
            if (!string.IsNullOrEmpty(typeName) && itemsByName.TryGetValue(typeName, out var data)) return data;
            return fallbackData;
        }
    }
}
