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
            return defaultMaxSkillLevel;
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

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerStats != null && playerStats.SkillPoints < 1)
            {
                reason = "Không đủ Điểm Kỹ Năng (SP)";
                return false;
            }

            // Yêu cầu cấp nhân vật tối thiểu: Cấp 1 -> cần lv 1, Cấp 2 -> cần lv 3, Cấp 3 -> cần lv 5, v.v.
            int reqPlayerLevel = curLevel * 2;
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

            if (playerStats != null && !playerStats.ConsumeSkillPoint(1))
            {
                return false;
            }

            int newLevel = GetSkillLevel(skillId) + 1;
            skillLevels[skillId] = newLevel;

            OnSkillUpgraded?.Invoke(skillId, newLevel);
            return true;
        }

        // --- CÁC HÀM TÍNH TOÁN CHỈ SỐ THEO CẤP ĐỘ ---

        public static float GetScaledDamage(SkillData skill, int level)
        {
            if (skill == null) return 0f;
            float multiplier = 1f + 0.15f * Mathf.Max(0, level - 1);
            return skill.baseDamage * multiplier;
        }

        public static float GetScaledPhysScale(SkillData skill, int level)
        {
            if (skill == null) return 0f;
            float multiplier = 1f + 0.10f * Mathf.Max(0, level - 1);
            return skill.physScale * multiplier;
        }

        public static float GetScaledMagicScale(SkillData skill, int level)
        {
            if (skill == null) return 0f;
            float multiplier = 1f + 0.10f * Mathf.Max(0, level - 1);
            return skill.magicScale * multiplier;
        }

        public static float GetScaledHeal(SkillData skill, int level)
        {
            if (skill == null) return 0f;
            float multiplier = 1f + 0.15f * Mathf.Max(0, level - 1);
            return skill.baseHeal * multiplier;
        }

        public static float GetScaledCooldown(SkillData skill, int level)
        {
            if (skill == null) return 0f;
            // Mỗi cấp giảm 0.2s hồi chiêu (tối thiểu 0.5s hoặc giữ nguyên nếu chiêu 0s)
            if (skill.cooldown <= 0f) return 0f;
            float cd = skill.cooldown - 0.2f * Mathf.Max(0, level - 1);
            return Mathf.Max(0.5f, cd);
        }

        public static float GetScaledManaCost(SkillData skill, int level)
        {
            if (skill == null) return 0f;
            if (skill.manaCost <= 0f) return 0f;
            float multiplier = 1f + 0.05f * Mathf.Max(0, level - 1);
            return Mathf.Round(skill.manaCost * multiplier);
        }
    }
}
