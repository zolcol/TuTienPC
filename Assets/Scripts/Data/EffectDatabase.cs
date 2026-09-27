using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class EffectResData
    {
        public int resId;
        public string resFilePath;
        public string lowResFilePath;
        public string weaponEffectSkillPath;
        public bool lockRotate; // 1 = Khóa góc xoay theo phương ngang khi gắn vào xương/model
        public string desc;

        private string cleanPath;

        public string CleanPath
        {
            get
            {
                if (cleanPath != null) return cleanPath;
                if (string.IsNullOrEmpty(resFilePath)) return string.Empty;

                string p = resFilePath.Trim().Replace("\\", "/");
                if (p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    p = p.Substring(0, p.Length - 7);
                }
                cleanPath = p;
                return cleanPath;
            }
        }
    }

    public class EffectDatabase : MonoBehaviour
    {
        private static EffectDatabase instance;
        public static EffectDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EffectDatabase>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[EffectDatabase]");
                        instance = go.AddComponent<EffectDatabase>();
                        DontDestroyOnLoad(go);
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        private readonly Dictionary<int, EffectResData> effectsById = new Dictionary<int, EffectResData>();
        private readonly Dictionary<string, EffectResData> effectsByPath = new Dictionary<string, EffectResData>(StringComparer.OrdinalIgnoreCase);
        private bool isLoaded = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureLoaded();
        }

        public void EnsureLoaded()
        {
            if (!isLoaded || effectsById.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại Effect Database")]
        public void LoadDatabase()
        {
            effectsById.Clear();
            effectsByPath.Clear();

            string filePath = Path.Combine(Application.dataPath, "Settings", "N", "EffectRes.csv");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[EffectDatabase] ⚠️ Không tìm thấy file tại: {filePath}");
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

                        int resId = CsvParserHelper.ParseInt(tokens[0]);
                        if (resId <= 0) continue;

                        string resPath = CsvParserHelper.GetToken(tokens, 1);
                        string lowPath = CsvParserHelper.GetToken(tokens, 2);
                        string wpnPath = CsvParserHelper.GetToken(tokens, 3);
                        string rawLock = CsvParserHelper.GetToken(tokens, 4).Trim();
                        bool lockRotate = rawLock.Equals("1");
                        string desc = CsvParserHelper.GetToken(tokens, 5);

                        EffectResData data = new EffectResData
                        {
                            resId = resId,
                            resFilePath = resPath,
                            lowResFilePath = lowPath,
                            weaponEffectSkillPath = wpnPath,
                            lockRotate = lockRotate,
                            desc = desc
                        };

                        effectsById[resId] = data;

                        string clean = data.CleanPath;
                        if (!string.IsNullOrEmpty(clean))
                        {
                            effectsByPath[clean] = data;
                        }
                    }
                }

                isLoaded = true;
                // Debug.Log($"✅ <color=cyan>[EffectDatabase]</color> Đã nạp thành công <b>{effectsById.Count}</b> hiệu ứng kỹ năng từ Settings/N/EffectRes.csv!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EffectDatabase] ❌ Lỗi đọc EffectRes.csv: {ex.Message}");
            }
        }

        public static string GetEffectPath(int resId)
        {
            if (resId <= 0) return string.Empty;
            Instance.EnsureLoaded();
            return Instance.effectsById.TryGetValue(resId, out EffectResData data) ? data.CleanPath : string.Empty;
        }

        public static bool HasEffect(int resId)
        {
            if (resId <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.effectsById.ContainsKey(resId);
        }

        public static EffectResData GetEffectData(int resId)
        {
            if (resId <= 0) return null;
            Instance.EnsureLoaded();
            Instance.effectsById.TryGetValue(resId, out EffectResData data);
            return data;
        }

        public static EffectResData GetEffectDataByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Instance.EnsureLoaded();

            string clean = path.Trim().Replace("\\", "/");
            if (clean.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 7);
            }

            Instance.effectsByPath.TryGetValue(clean, out EffectResData data);
            return data;
        }

        public static bool IsLockRotate(int resId)
        {
            var data = GetEffectData(resId);
            return data != null && data.lockRotate;
        }

        public static bool IsLockRotate(string path)
        {
            var data = GetEffectDataByPath(path);
            return data != null && data.lockRotate;
        }
    }
}
