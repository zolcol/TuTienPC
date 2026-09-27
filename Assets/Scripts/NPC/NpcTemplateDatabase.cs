using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.NPC
{
    public class NpcTemplateDatabase : MonoBehaviour
    {
        private static NpcTemplateDatabase instance;
        public static NpcTemplateDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NpcTemplateDatabase>();
                    if (instance == null)
                    {
                        GameObject dbObj = new GameObject("[NpcTemplateDatabase]");
                        instance = dbObj.AddComponent<NpcTemplateDatabase>();
                        DontDestroyOnLoad(dbObj);
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        [Tooltip("File CSV dự phòng (nếu không đọc trực tiếp từ Settings/N)")]
        [SerializeField] private TextAsset fallbackCsvFile;

        private readonly Dictionary<int, NpcTemplateData> templates = new Dictionary<int, NpcTemplateData>();
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
            if (!isLoaded || templates.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại Database từ CSV")]
        public void LoadDatabase()
        {
            templates.Clear();

            // Đảm bảo 2 Database nền tảng đã sẵn sàng
            NpcResDatabase.Instance.EnsureLoaded();
            NpcAttributeDatabase.Instance.EnsureLoaded();

            string nTemplatePath = Path.Combine(Application.dataPath, "Settings", "N", "NpcTemplate.csv");
            string nCharacterPath = Path.Combine(Application.dataPath, "Settings", "N", "Character.csv");

            // 1. ƯU TIÊN NẠP DỮ LIỆU CHUẨN TỪ Settings/N
            if (File.Exists(nTemplatePath))
            {
                LoadFromStandardNTemplate(nTemplatePath);
                if (File.Exists(nCharacterPath))
                {
                    LoadFromStandardNTemplate(nCharacterPath); // Nạp thêm các class nhân vật Player
                }

                if (templates.Count > 0)
                {
                    isLoaded = true;
                    // Debug.Log($"✅ <color=green>[NpcTemplateDatabase]</color> Đã nạp thành công <b>{templates.Count}</b> mẫu Quái vật & Nhân vật từ Settings/N (NpcTemplate.csv + Character.csv)!");
                    return;
                }
            }

            // 2. FALLBACK VỀ FILE CŨ
            LoadFromLegacyCsv();
        }

        private void LoadFromStandardNTemplate(string filePath)
        {
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

                        int id = GetColInt(tokens, colMap, "templateid", 1);
                        if (id <= 0) continue;

                        string name = GetColString(tokens, colMap, "name", 2);
                        NpcKind kind = ParseKind(GetColString(tokens, colMap, "kind", 3));
                        int camp = GetColInt(tokens, colMap, "camp", 4, 1);
                        int resId = GetColInt(tokens, colMap, "npcresid", 7);
                        int attribId = GetColInt(tokens, colMap, "npcattribid", 12);

                        int skill1 = GetColInt(tokens, colMap, "normalskill1", 13);
                        int skill2 = GetColInt(tokens, colMap, "normalskill2", 15);
                        int skill3 = GetColInt(tokens, colMap, "normalskill3", 17);

                        // Tầm nhìn / Tốc độ (cm -> m) theo chuẩn DATA_CONVENTIONS.md
                        float rawVision = GetColFloat(tokens, colMap, "visionradius", 19, 1000f);
                        float rawActive = GetColFloat(tokens, colMap, "activeradius", 20, 1500f);
                        float visionMeters = rawVision > 0f ? (rawVision / 100f) : 10f;
                        float activeMeters = rawActive > 0f ? (rawActive / 100f) : 15f;

                        float rawSpeed = GetColFloat(tokens, colMap, "runspeed", 25, 0f);
                        float runSpeed = (rawSpeed >= 100f) ? (rawSpeed / 100f) : 5.0f;

                        // Đồng bộ file prefab từ NpcResDatabase
                        string prefab = "";
                        var resData = NpcResDatabase.GetRes(resId);
                        if (resData != null)
                        {
                            prefab = resData.resFile;
                        }

                        NpcTemplateData data = new NpcTemplateData
                        {
                            id = id,
                            name = name,
                            kind = kind,
                            camp = camp,
                            npcResId = resId,
                            npcAttribId = attribId,
                            skill = skill1,
                            skill1 = skill1,
                            skill2 = skill2,
                            skill3 = skill3,
                            visionRadius = visionMeters,
                            activeRadius = activeMeters,
                            runSpeed = runSpeed,
                            prefab = prefab
                        };

                        templates[id] = data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcTemplateDatabase] ❌ Lỗi đọc {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        private void LoadFromLegacyCsv()
        {
            string filePath = Path.Combine(Application.dataPath, "Settings", "NpcTemplate.csv");
            if (!File.Exists(filePath) && fallbackCsvFile == null)
            {
                Debug.LogWarning("[NpcTemplateDatabase] ⚠️ Không tìm thấy file NpcTemplate.csv cũ.");
                return;
            }

            string csvContent = fallbackCsvFile != null ? fallbackCsvFile.text : File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] tokens = CsvParserHelper.SplitCsvLine(line);
                if (tokens.Length < 3) continue;

                int id = CsvParserHelper.ParseInt(tokens[0]);
                if (id <= 0) continue;

                string name = CsvParserHelper.GetToken(tokens, 1);
                NpcKind kind = ParseKind(CsvParserHelper.GetToken(tokens, 2));
                int skill = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 3));
                int skill1 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 4));
                int skill2 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 5));
                int skill3 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 6));
                string prefab = CsvParserHelper.GetToken(tokens, 7);

                NpcTemplateData data = new NpcTemplateData
                {
                    id = id,
                    name = name,
                    kind = kind,
                    skill = skill,
                    skill1 = skill1,
                    skill2 = skill2,
                    skill3 = skill3,
                    prefab = prefab
                };

                templates[id] = data;
            }

            isLoaded = true;
            // Debug.Log($"✅ [NpcTemplateDatabase] Đã nạp {templates.Count} NPC từ file cũ (Fallback)!");
        }

        private string GetColRaw(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < tokens.Length) return tokens[idx];
            return fallbackIndex < tokens.Length ? tokens[fallbackIndex] : "";
        }

        private string GetColString(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            return GetColRaw(tokens, colMap, key, fallbackIndex).Trim();
        }

        private int GetColInt(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, int def = 0)
        {
            string raw = GetColRaw(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseInt(raw, def);
        }

        private float GetColFloat(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, float def = 0f)
        {
            string raw = GetColRaw(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseFloat(raw, def);
        }

        private NpcKind ParseKind(string token)
        {
            if (string.IsNullOrEmpty(token)) return NpcKind.Normal;
            token = token.Trim().ToLowerInvariant();

            if (int.TryParse(token, out int intVal))
            {
                if (Enum.IsDefined(typeof(NpcKind), intVal))
                {
                    return (NpcKind)intVal;
                }
            }

            switch (token)
            {
                case "normal":
                case "monster":
                case "enemy":
                    return NpcKind.Normal;
                case "player":
                    return NpcKind.Player;
                case "dialoger":
                case "dialog":
                case "npc":
                    return NpcKind.Dialoger;
                case "partner":
                case "pet":
                    return NpcKind.Partner;
                case "silencer":
                case "portal":
                    return NpcKind.Silencer;
                case "gather":
                case "chest":
                    return NpcKind.Gather;
                case "trap":
                    return NpcKind.Trap;
            }

            return NpcKind.Normal;
        }

        public static NpcTemplateData GetTemplate(int id)
        {
            if (id <= 0) return null;
            Instance.EnsureLoaded();
            Instance.templates.TryGetValue(id, out NpcTemplateData data);
            return data;
        }

        public static bool HasTemplate(int id)
        {
            if (id <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.templates.ContainsKey(id);
        }

        public static Dictionary<int, NpcTemplateData> GetAllTemplates()
        {
            Instance.EnsureLoaded();
            return Instance.templates;
        }

        public static GameObject LoadPrefab(string path)
        {
            return NpcResDatabase.LoadPrefab(path);
        }

        public static void Reload()
        {
            Instance.LoadDatabase();
        }
    }
}
