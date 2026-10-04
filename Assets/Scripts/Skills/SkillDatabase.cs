using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.Skills
{
    public class SkillDatabase : ICsvTable
    {
        private static SkillDatabase instance;
        public static SkillDatabase Instance => instance ?? (instance = new SkillDatabase());

        private readonly Dictionary<int, SkillData> skills = new Dictionary<int, SkillData>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || skills.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            skills.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            EffectDatabase.Instance.EnsureLoaded();
            FactionSkillDatabase.Instance.EnsureLoaded();
            StateEffectDatabase.Instance.EnsureLoaded();
            MissileDatabase.Instance.EnsureLoaded();

            string nSkillPath = GameDataPaths.SkillCsv;
            string nActionEventPath = GameDataPaths.ActionEventCsv;

            if (File.Exists(nSkillPath) && File.Exists(nActionEventPath))
            {
                SkillCsvParser.LoadStandard(nSkillPath, nActionEventPath, skills);
                if (skills.Count > 0)
                {
                    IsLoaded = true;
                    return;
                }
            }

            string legacyPath = Path.Combine(Application.dataPath, "Settings", "Skills.csv");
            LegacySkillCsvParser.Load(legacyPath, skills);
            IsLoaded = true;
        }

        public static SkillData GetSkill(int id)
        {
            if (id <= 0) return null;
            Instance.EnsureLoaded();
            Instance.skills.TryGetValue(id, out SkillData data);
            return data;
        }

        public static bool HasSkill(int id)
        {
            if (id <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.skills.ContainsKey(id);
        }

        public static Dictionary<int, SkillData> GetAllSkills()
        {
            Instance.EnsureLoaded();
            return Instance.skills;
        }

        public static void Reload()
        {
            Instance.Load();
        }
    }
}
