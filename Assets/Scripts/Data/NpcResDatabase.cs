using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class NpcResData
    {
        public int resId;
        public string resFile;
        public string desc;
        public int runSoundId;
        public int deathSoundId;
        public int hitSoundId;
        public float height = 1.8f;
        public float width = 0.5f;

        public Dictionary<string, int> ActionFrames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, float> ActionCrossFades = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private GameObject cachedPrefab;
        private bool attemptedLoad = false;

        public GameObject LoadPrefab()
        {
            if (cachedPrefab != null) return cachedPrefab;
            if (attemptedLoad) return null;

            attemptedLoad = true;
            cachedPrefab = NpcResDatabase.LoadPrefab(resFile);
            return cachedPrefab;
        }
    }

    public class NpcResDatabase : MonoBehaviour
    {
        private static NpcResDatabase instance;
        public static NpcResDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NpcResDatabase>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[NpcResDatabase]");
                        instance = go.AddComponent<NpcResDatabase>();
                        DontDestroyOnLoad(go);
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        private readonly Dictionary<int, NpcResData> resDict = new Dictionary<int, NpcResData>();
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
            if (!isLoaded || resDict.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại NpcRes Database")]
        public void LoadDatabase()
        {
            resDict.Clear();

            string filePath = Path.Combine(Application.dataPath, "Settings", "N", "NpcRes.csv");
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
                        float height = GetColFloat(tokens, colMap, "height", 7, 1.8f);
                        float width = GetColFloat(tokens, colMap, "width", 8, 0.5f);

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

                isLoaded = true;
                Debug.Log($"✅ <color=cyan>[NpcResDatabase]</color> Đã nạp thành công <b>{resDict.Count}</b> tài nguyên Model 3D từ Settings/N/NpcRes.csv!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcResDatabase] ❌ Lỗi đọc NpcRes.csv: {ex.Message}");
            }
        }

        private string GetColString(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < tokens.Length) return tokens[idx].Trim();
            return fallbackIndex < tokens.Length ? tokens[fallbackIndex].Trim() : "";
        }

        private int GetColInt(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, int def = 0)
        {
            string raw = GetColString(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseInt(raw, def);
        }

        private float GetColFloat(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, float def = 0f)
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

        /// <summary>
        /// Bộ nạp Prefab Model 3D thông minh đa đường dẫn (Resources & Assets Fallback)
        /// </summary>
        public static GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            string cleanPath = path.Replace("\\", "/").Trim();
            if (cleanPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(0, cleanPath.Length - 7);
            }

            // 1. Thử Resources.Load trực tiếp
            GameObject prefab = Resources.Load<GameObject>(cleanPath);
            if (prefab != null) return prefab;

            // 2. Thử biến thể đường dẫn "Players/" thay vì "Player/" (do cấu trúc thư mục)
            if (cleanPath.StartsWith("Player/", StringComparison.OrdinalIgnoreCase))
            {
                string altPath = "Players/" + cleanPath.Substring(7);
                prefab = Resources.Load<GameObject>(altPath);
                if (prefab != null) return prefab;
            }

            // 3. Thử tìm theo tên file trong Resources
            string fileName = Path.GetFileNameWithoutExtension(path);
            prefab = Resources.Load<GameObject>($"Players/Npcs/Prefabs/{fileName}");
            if (prefab != null) return prefab;

            prefab = Resources.Load<GameObject>($"Player/Npcs/Prefabs/{fileName}");
            if (prefab != null) return prefab;

#if UNITY_EDITOR
            // 4. Fallback trong Unity Editor dò tìm file chính xác bất kỳ thư mục nào
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{fileName} t:Prefab");
            foreach (var guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(assetPath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (loaded != null) return loaded;
                }
            }
#endif

            return null;
        }
    }
}
