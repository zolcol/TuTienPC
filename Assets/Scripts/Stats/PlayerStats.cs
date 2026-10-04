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

        public int CurrentLevel => currentLevel;
        public long CurrentExp => currentExp;

        public event System.Action<int, long, long> OnExpChanged; // currentLevel, currentExp, maxExp
        public event System.Action<int> OnLevelUp; // newLevel

        public long GetExpUpgradeForCurrentLevel()
        {
            var levelData = TopDownGame.Data.PlayerLevelDatabase.Instance.Get(currentLevel);
            return levelData != null && levelData.ExpUpGrade > 0 ? levelData.ExpUpGrade : 28000;
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
                leveledUp = true;
                requiredExp = GetExpUpgradeForCurrentLevel();
                OnLevelUp?.Invoke(currentLevel);
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
