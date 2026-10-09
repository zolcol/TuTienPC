using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TopDownGame.Data
{
    /// <summary>
    /// Bộ tiện ích xử lý định dạng CSV và bóc tách dữ liệu đa tầng (Level-based attributes)
    /// </summary>
    public static class CsvParserHelper
    {
        private static readonly Regex LevelPairRegex = new Regex(@"\{(\d+)\s*,\s*([\d\.-]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Tách các dòng CSV có xử lý dấu ngoặc kép (Double quotes) an toàn
        /// </summary>
        public static string[] SplitCsvLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();

            List<string> result = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    string token = line.Substring(startIndex, i - startIndex).Trim().Trim('"');
                    result.Add(token);
                    startIndex = i + 1;
                }
            }

            if (startIndex <= line.Length)
            {
                string token = line.Substring(startIndex).Trim().Trim('"');
                result.Add(token);
            }

            return result.ToArray();
        }

        public static string GetToken(string[] tokens, int index, string defaultVal = "")
        {
            return (tokens != null && index >= 0 && index < tokens.Length) ? tokens[index].Trim() : defaultVal;
        }

        public static int ParseInt(string s, int defaultVal = 0)
        {
            if (string.IsNullOrEmpty(s)) return defaultVal;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val) ? val : defaultVal;
        }

        public static long ParseLong(string s, long defaultVal = 0L)
        {
            if (string.IsNullOrEmpty(s)) return defaultVal;
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long val) ? val : defaultVal;
        }

        public static float ParseFloat(string s, float defaultVal = 0f)
        {
            if (string.IsNullOrEmpty(s)) return defaultVal;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : defaultVal;
        }

        public static bool ParseBool(string s, bool defaultVal = false)
        {
            if (string.IsNullOrEmpty(s)) return defaultVal;
            s = s.Trim().ToUpperInvariant();
            if (s == "TRUE" || s == "1" || s == "YES") return true;
            if (s == "FALSE" || s == "0" || s == "NO") return false;
            return defaultVal;
        }

        /// <summary>
        /// Bóc tách giá trị chỉ số theo Level từ chuỗi có format: "{1,188},{10,255},{20,439}" hoặc số đơn giản "30".
        /// Hỗ trợ nội suy tuyến tính (Linear Interpolation) giữa các mốc cấp độ.
        /// </summary>
        public static float ParseLevelValue(string raw, int targetLevel = 1, float defaultVal = 0f)
        {
            if (string.IsNullOrEmpty(raw)) return defaultVal;

            raw = raw.Trim().Trim('"');
            if (string.IsNullOrEmpty(raw)) return defaultVal;

            // 1. Nếu là số đơn giản (ví dụ "30", "15.5")
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float simpleVal))
            {
                return simpleVal;
            }

            // 2. Nếu là chuỗi danh sách các cặp {Level, Value}
            MatchCollection matches = LevelPairRegex.Matches(raw);
            if (matches.Count > 0)
            {
                List<KeyValuePair<int, float>> points = new List<KeyValuePair<int, float>>(matches.Count);

                foreach (Match m in matches)
                {
                    if (m.Groups.Count >= 3)
                    {
                        if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lv) &&
                            float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                        {
                            points.Add(new KeyValuePair<int, float>(lv, val));
                        }
                    }
                }

                if (points.Count == 0) return defaultVal;
                if (points.Count == 1) return points[0].Value;

                points.Sort((a, b) => a.Key.CompareTo(b.Key));

                if (targetLevel <= points[0].Key)
                {
                    return points[0].Value;
                }

                if (targetLevel >= points[points.Count - 1].Key)
                {
                    return points[points.Count - 1].Value;
                }

                for (int i = 0; i < points.Count - 1; i++)
                {
                    var p1 = points[i];
                    var p2 = points[i + 1];

                    if (targetLevel == p1.Key) return p1.Value;
                    if (targetLevel == p2.Key) return p2.Value;

                    if (targetLevel > p1.Key && targetLevel < p2.Key)
                    {
                        float t = (float)(targetLevel - p1.Key) / (p2.Key - p1.Key);
                        return p1.Value + (p2.Value - p1.Value) * t;
                    }
                }

                return points[points.Count - 1].Value;
            }

            return defaultVal;
        }

        /// <summary>
        /// Bóc tách giá trị nguyên rời rạc theo dạng bậc thang (Step / Tier).
        /// Ví dụ format: "{1,1},{4,2}" -> targetLevel 1..3 trả về 1, targetLevel 4+ trả về 2.
        /// </summary>
        public static int ParseLevelStepInt(string raw, int targetLevel = 1, int defaultVal = 1)
        {
            if (string.IsNullOrEmpty(raw)) return defaultVal;

            raw = raw.Trim().Trim('"');
            if (string.IsNullOrEmpty(raw)) return defaultVal;

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int simpleVal))
            {
                return simpleVal;
            }

            MatchCollection matches = LevelPairRegex.Matches(raw);
            if (matches.Count > 0)
            {
                List<KeyValuePair<int, int>> points = new List<KeyValuePair<int, int>>(matches.Count);

                foreach (Match m in matches)
                {
                    if (m.Groups.Count >= 3)
                    {
                        if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lv) &&
                            int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
                        {
                            points.Add(new KeyValuePair<int, int>(lv, val));
                        }
                    }
                }

                if (points.Count == 0) return defaultVal;

                points.Sort((a, b) => a.Key.CompareTo(b.Key));

                int result = points[0].Value;
                for (int i = 0; i < points.Count; i++)
                {
                    if (targetLevel >= points[i].Key)
                    {
                        result = points[i].Value;
                    }
                    else
                    {
                        break;
                    }
                }
                return result;
            }

            return defaultVal;
        }

        /// <summary>
        /// Bóc tách giá trị nguyên có hỗ trợ nội suy tuyến tính (Linear Interpolation) làm tròn.
        /// Ví dụ format: "{1,1},{3,5},{7,10}"
        /// </summary>
        public static int ParseLevelInt(string raw, int targetLevel = 1, int defaultVal = 1)
        {
            if (string.IsNullOrEmpty(raw)) return defaultVal;
            float val = ParseLevelValue(raw, targetLevel, defaultVal);
            return Mathf.RoundToInt(val);
        }
    }
}
