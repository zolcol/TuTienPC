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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            instance = null;
        }

        private readonly Dictionary<int, SkillData> baseSkills = new Dictionary<int, SkillData>();
        private readonly Dictionary<int, SkillData> customSkills = new Dictionary<int, SkillData>();
        private readonly Dictionary<int, int> baseToCustomMap = new Dictionary<int, int>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || baseSkills.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            baseSkills.Clear();
            customSkills.Clear();
            baseToCustomMap.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            EffectDatabase.Instance.EnsureLoaded();
            StateEffectDatabase.Instance.EnsureLoaded();
            MissileDatabase.Instance.EnsureLoaded();

            string nSkillPath = GameDataPaths.SkillCsv;
            string nActionEventPath = GameDataPaths.ActionEventCsv;

            if (File.Exists(nSkillPath) && File.Exists(nActionEventPath))
            {
                SkillCsvParser.LoadStandard(nSkillPath, nActionEventPath, baseSkills);
            }
            else
            {
                string legacyPath = Path.Combine(Application.dataPath, "Settings", "Skills.csv");
                LegacySkillCsvParser.Load(legacyPath, baseSkills);
            }

            string customSkillPath = GameDataPaths.CustomSkillCsv;
            if (File.Exists(customSkillPath))
            {
                SkillCsvParser.LoadCustomSkills(customSkillPath, baseSkills, customSkills, baseToCustomMap);
            }

            IsLoaded = true;
        }

        /// <summary>
        /// Lấy kỹ năng của game (CHỈ tra cứu từ CustomSkill.csv).
        /// Hỗ trợ cả Custom SkillId lẫn BaseSkillId (tự động ánh xạ BaseSkillId sang Custom Skill tương ứng).
        /// Nếu chiêu chưa được khai báo trong CustomSkill.csv sẽ trả về null và cảnh báo.
        /// </summary>
        public static SkillData GetSkill(int id)
        {
            if (id <= 0) return null;
            Instance.EnsureLoaded();
            if (Instance.customSkills.TryGetValue(id, out SkillData data))
            {
                return data;
            }

            // Tự động map từ BaseSkillId sang Custom Skill
            if (Instance.baseToCustomMap.TryGetValue(id, out int customId) && Instance.customSkills.TryGetValue(customId, out SkillData mappedData))
            {
                return mappedData;
            }

            Debug.LogWarning($"[SkillDatabase] ⚠️ Không tìm thấy Skill ID {id} (hoặc BaseSkillId {id}) trong CustomSkill.csv! Hãy khai báo chiêu này trong CustomSkill.csv trước khi dùng.");
            return null;
        }

        /// <summary>
        /// Lấy chiêu gốc trong Skill.csv (dành cho bộ nạp hoặc công cụ tra cứu animation/vfx gốc).
        /// </summary>
        public static SkillData GetBaseSkill(int baseId)
        {
            if (baseId <= 0) return null;
            Instance.EnsureLoaded();
            Instance.baseSkills.TryGetValue(baseId, out SkillData data);
            return data;
        }

        /// <summary>
        /// Lấy chiêu thức (ưu tiên custom skill theo ID hoặc BaseID, nếu không có mới tìm trong base skill).
        /// Dùng cho các sub-skill/child effect nội bộ.
        /// </summary>
        public static SkillData GetSkillOrBase(int id)
        {
            if (id <= 0) return null;
            Instance.EnsureLoaded();
            if (Instance.customSkills.TryGetValue(id, out SkillData data)) return data;
            if (Instance.baseToCustomMap.TryGetValue(id, out int customId) && Instance.customSkills.TryGetValue(customId, out SkillData mappedData)) return mappedData;
            if (Instance.baseSkills.TryGetValue(id, out SkillData baseData)) return baseData;
            return null;
        }

        /// <summary>
        /// Lấy chiêu phụ / chiêu con: Ưu tiên lấy từ CustomSkill.csv nếu có; 
        /// nếu không có thì lấy từ BaseSkill và tự động kế thừa các chỉ số chiến đấu từ chiêu cha (parentSkill).
        /// </summary>
        public static SkillData GetSubSkill(int subSkillId, SkillData parentSkill = null)
        {
            if (subSkillId <= 0) return null;
            Instance.EnsureLoaded();

            if (TryGetCustomSkill(subSkillId, out SkillData customSub))
            {
                return customSub;
            }

            if (Instance.baseSkills.TryGetValue(subSkillId, out SkillData baseSub))
            {
                if (parentSkill != null)
                {
                    SkillData inherited = baseSub.Clone();
                    inherited.baseDamage = parentSkill.baseDamage;
                    inherited.physScale = parentSkill.physScale;
                    inherited.magicScale = parentSkill.magicScale;
                    inherited.baseHeal = parentSkill.baseHeal;
                    inherited.healScale = parentSkill.healScale;
                    return inherited;
                }
                return baseSub;
            }

            return null;
        }

        public static bool HasSkill(int id)
        {
            if (id <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.customSkills.ContainsKey(id) || Instance.baseToCustomMap.ContainsKey(id);
        }

        public static bool TryGetCustomSkill(int id, out SkillData customSkill)
        {
            customSkill = null;
            if (id <= 0) return false;
            Instance.EnsureLoaded();
            if (Instance.customSkills.TryGetValue(id, out customSkill)) return true;
            if (Instance.baseToCustomMap.TryGetValue(id, out int customId))
            {
                return Instance.customSkills.TryGetValue(customId, out customSkill);
            }
            return false;
        }

        public static bool TryGetCustomSkillByBaseId(int baseSkillId, out SkillData customSkill)
        {
            return TryGetCustomSkill(baseSkillId, out customSkill);
        }

        public static Dictionary<int, SkillData> GetAllSkills()
        {
            Instance.EnsureLoaded();
            return Instance.customSkills;
        }

        public static Dictionary<int, SkillData> GetAllBaseSkills()
        {
            Instance.EnsureLoaded();
            return Instance.baseSkills;
        }

        public static void Reload()
        {
            Instance.Load();
        }
    }
}
