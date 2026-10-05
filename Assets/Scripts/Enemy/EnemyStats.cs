using UnityEngine;
using TopDownGame.Stats;
using TopDownGame.UI;

namespace TopDownGame.Enemy
{
    /// <summary>
    /// Quản lý chỉ số và phản ứng khi bị thương / chết của quái vật
    /// Kế thừa EntityStats nên đã có sẵn hệ thống Máu, IDamageable, OnDamaged, OnDeath
    /// </summary>
    public class EnemyStats : EntityStats
    {
        [Header("Enemy Rewards & Settings")]
        [Tooltip("Cấp độ quái vật (mặc định lv 1 theo quy ước hiện tại)")]
        [SerializeField] private int monsterLevel = 1;

        [Tooltip("Thời gian xác quái biến mất sau khi chết (giây)")]
        [SerializeField] private float despawnDelay = 3.0f;

        [Header("Floating Health Bar (Tùy chọn)")]
        [Tooltip("Bật/tắt thanh máu nhỏ trên đầu quái")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private float healthBarOffsetY = 2.0f;

        private bool hasAwardedExp = false;

        public int MonsterLevel { get => monsterLevel; set => monsterLevel = Mathf.Max(1, value); }
        public float DespawnDelay => despawnDelay;

        protected override void Die()
        {
            base.Die();
            AwardExpToPlayer();
        }

        private void AwardExpToPlayer()
        {
            if (hasAwardedExp) return;
            hasAwardedExp = true;

            var player = FindObjectOfType<TopDownGame.Player.PlayerController>();
            if (player != null && player.Stats != null)
            {
                int pLevel = player.Stats.CurrentLevel;
                long exp = TopDownGame.Data.ExpRuleDatabase.Instance.CalculateExpReward(pLevel, monsterLevel);

                if (exp > 0)
                {
                    player.Stats.AddExp(exp);
                    Vector3 textSpawnPos = transform.position + Vector3.up * 1.8f;
                    FloatingTextManager.Instance.SpawnExp((int)exp, textSpawnPos);
                }
            }
        }

        private EnemyHealthBar healthBar;

        protected virtual void Start()
        {
            if (showHealthBar)
            {
                healthBar = GetComponent<EnemyHealthBar>();
                if (healthBar == null)
                {
                    healthBar = gameObject.AddComponent<EnemyHealthBar>();
                }
                healthBar.Initialize(this, healthBarOffsetY);
            }
        }

        public void SetHealthBarOffset(float offset)
        {
            healthBarOffsetY = offset;
            if (healthBar != null)
            {
                healthBar.SetOffsetY(offset);
            }
        }
    }
}
