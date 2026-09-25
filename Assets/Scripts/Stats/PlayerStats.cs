using UnityEngine;

namespace TopDownGame.Stats
{
    /// <summary>
    /// Quản lý chỉ số nâng cao cho Người chơi (Máu kế thừa từ EntityStats + Mana và cơ chế tiêu hao năng lượng)
    /// </summary>
    public class PlayerStats : EntityStats
    {
        [Header("Mana Resource")]
        [SerializeField] private ResourceStat mana = new ResourceStat();

        public ResourceStat Mana => mana;

        protected override void Awake()
        {
            base.Awake();
            mana.Initialize();
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;
            mana.Tick(Time.deltaTime);
        }

        public bool HasEnoughMana(float amount)
        {
            return mana.CurrentValue >= amount;
        }

        public bool ConsumeMana(float amount)
        {
            return mana.Consume(amount);
        }

        public void RestoreMana(float amount)
        {
            mana.Modify(amount);
        }

        public override void Revive(float healthPercentage = 1.0f)
        {
            base.Revive(healthPercentage);
            mana.Initialize();
        }
    }
}
