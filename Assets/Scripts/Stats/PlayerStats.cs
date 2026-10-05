using UnityEngine;
using TopDownGame.UI;

namespace TopDownGame.Stats
{
    /// <summary>
    /// Quản lý chỉ số nâng cao cho Người chơi (Máu & Mana kế thừa từ EntityStats + Cấp độ & Kinh nghiệm)
    /// </summary>
    public class PlayerStats : EntityStats
    {
        [Header("Level & Experience")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private long currentExp = 0;

        [Header("Skill Progression")]
        [SerializeField] private int skillPoints = 3;

        public int CurrentLevel => currentLevel;
        public long CurrentExp => currentExp;
        public int SkillPoints => skillPoints;

        public event System.Action<int, long, long> OnExpChanged; // currentLevel, currentExp, maxExp
        public event System.Action<int> OnLevelUp; // newLevel
        public event System.Action<int> OnSkillPointsChanged; // currentSkillPoints

        public long GetExpUpgradeForCurrentLevel()
        {
            var levelData = TopDownGame.Data.PlayerLevelDatabase.Instance.Get(currentLevel);
            return levelData != null && levelData.ExpUpGrade > 0 ? levelData.ExpUpGrade : 28000;
        }

        public void AddSkillPoints(int amount)
        {
            if (amount <= 0) return;
            skillPoints += amount;
            OnSkillPointsChanged?.Invoke(skillPoints);
        }

        public bool ConsumeSkillPoint(int amount = 1)
        {
            if (skillPoints < amount) return false;
            skillPoints -= amount;
            OnSkillPointsChanged?.Invoke(skillPoints);
            return true;
        }

        public void AddExp(long amount)
        {
            if (amount <= 0 || IsDead) return;

            currentExp += amount;
            long requiredExp = GetExpUpgradeForCurrentLevel();

            int maxLevel = TopDownGame.Data.PlayerLevelDatabase.Instance.MaxLevel;
            if (maxLevel <= 0) maxLevel = 400;

            bool leveledUp = false;
            while (currentLevel < maxLevel && currentExp >= requiredExp)
            {
                currentExp -= requiredExp;
                currentLevel++;
                skillPoints++;
                leveledUp = true;
                requiredExp = GetExpUpgradeForCurrentLevel();
                OnLevelUp?.Invoke(currentLevel);
                OnSkillPointsChanged?.Invoke(skillPoints);
            }

            if (currentLevel >= maxLevel)
            {
                currentExp = System.Math.Min(currentExp, requiredExp);
            }

            OnExpChanged?.Invoke(currentLevel, currentExp, requiredExp);

            if (leveledUp)
            {
                FloatingTextManager.Instance.SpawnLevelUp(transform.position + Vector3.up * 2.2f);
            }
        }

        public override void Revive(float healthPercentage = 1.0f)
        {
            base.Revive(healthPercentage);
            mana.Initialize();
        }
    }
}
