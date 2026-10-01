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

        private void OnGUI()
        {
            if (!showHealthBar || IsDead) return;

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * healthBarOffsetY);
            if (screenPos.z > 0)
            {
                float barWidth = 80f;
                float barHeight = 8f;
                float hpPercent = health.Percentage;

                Rect bgRect = new Rect(screenPos.x - barWidth * 0.5f, Screen.height - screenPos.y, barWidth, barHeight);
                Rect fillRect = new Rect(bgRect.x, bgRect.y, barWidth * hpPercent, barHeight);

                GUI.color = new Color(0f, 0f, 0f, 0.8f);
                GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

                GUI.color = Color.Lerp(Color.red, new Color(0.2f, 0.9f, 0.2f), hpPercent);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

                GUI.color = Color.white;
            }
        }
    }
}
