using System;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Stats;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Quản lý cấp độ kỹ năng và hệ thống nâng cấp của người chơi (RPG Skill Progression)
    /// </summary>
    public class PlayerSkillManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int defaultMaxSkillLevel = 5;

        private readonly Dictionary<int, int> skillLevels = new Dictionary<int, int>();
        private PlayerStats playerStats;

        public event Action<int, int> OnSkillUpgraded; // skillId, newLevel

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        public void BindStats(PlayerStats stats)
        {
            playerStats = stats;
        }

        /// <summary>
        /// Lấy cấp độ hiện tại của kỹ năng (mặc định là 1 nếu đã mở khóa)
        /// </summary>
        public int GetSkillLevel(int skillId)
        {
            if (skillId <= 0) return 0;
            if (skillLevels.TryGetValue(skillId, out int level))
            {
                return level;
            }
            // Mặc định các chiêu trong CustomSkill đều có cấp khởi điểm là 1
            return 1;
        }

        public int GetMaxSkillLevel(int skillId)
        {
            var skill = SkillDatabase.GetSkill(skillId);
            return (skill != null && skill.maxLevel > 0) ? skill.maxLevel : defaultMaxSkillLevel;
        }

        /// <summary>
        /// Kiểm tra xem có đủ điều kiện nâng cấp chiêu thức này không
        /// </summary>
        public bool CanUpgradeSkill(int skillId, out string reason)
        {
            reason = "";
            if (skillId <= 0)
            {
                reason = "Kỹ năng không hợp lệ";
                return false;
            }

            int curLevel = GetSkillLevel(skillId);
            int maxLevel = GetMaxSkillLevel(skillId);

            if (curLevel >= maxLevel)
            {
                reason = "Đã đạt cấp tối đa";
                return false;
            }

            int nextLevel = curLevel + 1;
            var skill = SkillDatabase.GetSkill(skillId);
            int reqSp = skill != null ? skill.GetSpCost(nextLevel) : 1;
            int reqPlayerLevel = skill != null ? skill.GetRequiredPlayerLevel(nextLevel) : (curLevel * 2);

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerStats != null && playerStats.SkillPoints < reqSp)
            {
                reason = reqSp > 1 ? $"Không đủ Điểm Kỹ Năng (Cần {reqSp} SP)" : "Không đủ Điểm Kỹ Năng (SP)";
                return false;
            }

            if (playerStats != null && playerStats.CurrentLevel < reqPlayerLevel)
            {
                reason = $"Cần nhân vật đạt Cấp {reqPlayerLevel}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Thực hiện nâng cấp kỹ năng
        /// </summary>
        public bool UpgradeSkill(int skillId)
        {
            if (!CanUpgradeSkill(skillId, out string _)) return false;

            int curLevel = GetSkillLevel(skillId);
            int nextLevel = curLevel + 1;
            var skill = SkillDatabase.GetSkill(skillId);
            int reqSp = skill != null ? skill.GetSpCost(nextLevel) : 1;

            if (playerStats != null && !playerStats.ConsumeSkillPoint(reqSp))
            {
                return false;
            }

            skillLevels[skillId] = nextLevel;

            OnSkillUpgraded?.Invoke(skillId, nextLevel);
            return true;
        }

        // --- CÁC HÀM TÍNH TOÁN CHỈ SỐ THEO CẤP ĐỘ ---

        public static float GetScaledDamage(SkillData skill, int level)
        {
            return skill != null ? skill.GetBaseDamage(level) : 0f;
        }

        public static float GetScaledPhysScale(SkillData skill, int level)
        {
            return skill != null ? skill.GetPhysScale(level) : 0f;
        }

        public static float GetScaledMagicScale(SkillData skill, int level)
        {
            return skill != null ? skill.GetMagicScale(level) : 0f;
        }

        public static float GetScaledHeal(SkillData skill, int level)
        {
            return skill != null ? skill.GetBaseHeal(level) : 0f;
        }

        public static float GetScaledCooldown(SkillData skill, int level)
        {
            return skill != null ? skill.GetCooldown(level) : 0f;
        }

        public static float GetScaledManaCost(SkillData skill, int level)
        {
            return skill != null ? skill.GetManaCost(level) : 0f;
        }
    }
}
