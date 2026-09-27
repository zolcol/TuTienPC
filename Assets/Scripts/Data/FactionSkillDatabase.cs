using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class FactionSkillData
    {
        public int faction;
        public int skillId;
        public string desc;
        public int gainLevel;
        public bool isBaseSkill;
        public string btnName;
        public string iconAtlas;
        public string btnIcon;
        public bool isAnger;
    }

    public class FactionSkillDatabase : MonoBehaviour
    {
        private static FactionSkillDatabase instance;
        public static FactionSkillDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<FactionSkillDatabase>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[FactionSkillDatabase]");
                        instance = go.AddComponent<FactionSkillDatabase>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                        else
                        {
                            go.hideFlags = HideFlags.HideAndDontSave;
                        }
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        private readonly Dictionary<int, List<FactionSkillData>> factionSkills = new Dictionary<int, List<FactionSkillData>>();
        private readonly Dictionary<int, FactionSkillData> skillToFactionMap = new Dictionary<int, FactionSkillData>();
        private bool isLoaded = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
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
            if (!isLoaded || factionSkills.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại FactionSkill Database")]
        public void LoadDatabase()
        {
            factionSkills.Clear();
            skillToFactionMap.Clear();

            string filePath = Path.Combine(Application.dataPath, "Settings", "N", "FactionSkill.csv");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FactionSkillDatabase] ⚠️ Không tìm thấy file tại: {filePath}");
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
                        if (tokens.Length < 3) continue;

                        int faction = CsvParserHelper.ParseInt(tokens[0]);
                        int skillId = CsvParserHelper.ParseInt(tokens[1]);
                        if (faction <= 0 || skillId <= 0) continue;

                        string desc = CsvParserHelper.GetToken(tokens, 2);
                        int gainLevel = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 3), 1);
                        bool isBaseSkill = CsvParserHelper.ParseBool(CsvParserHelper.GetToken(tokens, 4));
                        string btnName = CsvParserHelper.GetToken(tokens, 7);
                        string iconAtlas = CsvParserHelper.GetToken(tokens, 8);
                        string btnIcon = CsvParserHelper.GetToken(tokens, 9);
                        bool isAnger = CsvParserHelper.ParseBool(CsvParserHelper.GetToken(tokens, 11));

                        FactionSkillData data = new FactionSkillData
                        {
                            faction = faction,
                            skillId = skillId,
                            desc = desc,
                            gainLevel = gainLevel,
                            isBaseSkill = isBaseSkill,
                            btnName = btnName,
                            iconAtlas = iconAtlas,
                            btnIcon = btnIcon,
                            isAnger = isAnger
                        };

                        if (!factionSkills.ContainsKey(faction))
                        {
                            factionSkills[faction] = new List<FactionSkillData>();
                        }
                        factionSkills[faction].Add(data);
                        skillToFactionMap[skillId] = data;
                    }
                }

                isLoaded = true;
                // Debug.Log($"✅ <color=cyan>[FactionSkillDatabase]</color> Đã nạp thành công kỹ năng cho <b>{factionSkills.Count}</b> môn phái từ Settings/N/FactionSkill.csv!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FactionSkillDatabase] ❌ Lỗi đọc FactionSkill.csv: {ex.Message}");
            }
        }

        public static List<FactionSkillData> GetSkillsByFaction(int faction)
        {
            Instance.EnsureLoaded();
            return Instance.factionSkills.TryGetValue(faction, out var list) ? list : new List<FactionSkillData>();
        }

        public static FactionSkillData GetFactionSkill(int skillId)
        {
            Instance.EnsureLoaded();
            return Instance.skillToFactionMap.TryGetValue(skillId, out var data) ? data : null;
        }
    }
}
