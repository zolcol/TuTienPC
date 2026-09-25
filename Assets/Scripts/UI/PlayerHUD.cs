using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.Stats;

namespace TopDownGame.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private PlayerController player;

        [Header("Health Bar (Máu)")]
        [SerializeField] private Image healthFill;
        [Tooltip("Vạch vàng tụt theo sau khi nhận sát thương (Ghost Health Bar)")]
        [SerializeField] private Image healthGhostFill;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private float healthLerpSpeed = 15f;
        [Tooltip("Thời gian chờ trước khi vạch vàng bắt đầu tụt (giây)")]
        [SerializeField] private float ghostDelay = 0.45f;
        [Tooltip("Tốc độ tụt của vạch vàng")]
        [SerializeField] private float ghostShrinkSpeed = 2f;

        [Header("Mana Bar (Năng lượng)")]
        [SerializeField] private Image manaFill;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private float manaLerpSpeed = 10f;

        [Header("Skill Slots (Hiển thị theo thứ tự Q - E - R)")]
        [SerializeField] private SkillSlotUI skillSlot_Q;
        [SerializeField] private SkillSlotUI skillSlot_E;
        [SerializeField] private SkillSlotUI skillSlot_R;

        private float targetHealthFill = 1f;
        private float targetManaFill = 1f;
        private float ghostTimer;

        private void Start()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
            }

            if (player != null)
            {
                BindPlayer(player);
            }
        }

        public void BindPlayer(PlayerController targetPlayer)
        {
            this.player = targetPlayer;
            if (player == null) return;

            // 1. Khởi tạo và lắng nghe sự kiện Stats
            PlayerStats stats = player.Stats != null ? player.Stats : player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.Health.OnValueChanged += HandleHealthChanged;
                stats.Mana.OnValueChanged += HandleManaChanged;

                // Cập nhật giá trị khởi điểm ngay lập tức
                HandleHealthChanged(stats.Health.CurrentValue, stats.Health.MaxValue);
                HandleManaChanged(stats.Mana.CurrentValue, stats.Mana.MaxValue);

                if (healthFill != null) healthFill.fillAmount = targetHealthFill;
                if (healthGhostFill != null) healthGhostFill.fillAmount = targetHealthFill;
                if (manaFill != null) manaFill.fillAmount = targetManaFill;
            }

            // 2. Thiết lập 3 ô kỹ năng theo thứ tự Q - E - R từ Database
            if (skillSlot_Q != null) skillSlot_Q.SetupSlot(player.SkillSlotQ, "Q");
            if (skillSlot_E != null) skillSlot_E.SetupSlot(player.SkillSlotE, "E");
            if (skillSlot_R != null) skillSlot_R.SetupSlot(player.SkillSlotR, "R");
        }

        private void OnDestroy()
        {
            if (player != null && player.Stats != null)
            {
                player.Stats.Health.OnValueChanged -= HandleHealthChanged;
                player.Stats.Mana.OnValueChanged -= HandleManaChanged;
            }
        }

        private void Update()
        {
            UpdateBarsAnimation();

            if (player == null) return;

            UpdateSkillCooldownsAndMana();
        }

        private void UpdateBarsAnimation()
        {
            // 1. Thanh máu đỏ
            if (healthFill != null)
            {
                healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetHealthFill, Time.deltaTime * healthLerpSpeed);
                if (Mathf.Abs(healthFill.fillAmount - targetHealthFill) < 0.001f)
                {
                    healthFill.fillAmount = targetHealthFill;
                }
            }

            // 2. Vạch vàng tụt theo sau một khoảng trễ (Ghost Health Bar)
            if (healthGhostFill != null)
            {
                if (ghostTimer > 0f)
                {
                    ghostTimer -= Time.deltaTime;
                }
                else
                {
                    healthGhostFill.fillAmount = Mathf.Lerp(healthGhostFill.fillAmount, targetHealthFill, Time.deltaTime * ghostShrinkSpeed);
                    if (Mathf.Abs(healthGhostFill.fillAmount - targetHealthFill) < 0.001f)
                    {
                        healthGhostFill.fillAmount = targetHealthFill;
                    }
                }

                if (healthFill != null && healthGhostFill.fillAmount < healthFill.fillAmount)
                {
                    healthGhostFill.fillAmount = healthFill.fillAmount;
                }
            }

            // 3. Thanh mana xanh
            if (manaFill != null)
            {
                manaFill.fillAmount = Mathf.Lerp(manaFill.fillAmount, targetManaFill, Time.deltaTime * manaLerpSpeed);
                if (Mathf.Abs(manaFill.fillAmount - targetManaFill) < 0.001f)
                {
                    manaFill.fillAmount = targetManaFill;
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float newFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (newFill < targetHealthFill)
            {
                ghostTimer = ghostDelay;
            }
            else
            {
                ghostTimer = 0f;
            }

            targetHealthFill = newFill;

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            targetManaFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (manaText != null)
            {
                manaText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void UpdateSkillCooldownsAndMana()
        {
            UpdateSlot(skillSlot_Q, player.SkillSlotQ);
            UpdateSlot(skillSlot_E, player.SkillSlotE);
            UpdateSlot(skillSlot_R, player.SkillSlotR);
        }

        private void UpdateSlot(SkillSlotUI slotUI, SkillData skill)
        {
            if (slotUI == null || skill == null) return;

            float remaining = player.GetRemainingCooldown(skill.id);
            slotUI.UpdateCooldown(remaining, skill.cooldown);

            if (player.Stats != null && skill.manaCost > 0f)
            {
                slotUI.SetManaAffordable(player.Stats.HasEnoughMana(skill.manaCost));
            }
            else
            {
                slotUI.SetManaAffordable(true);
            }
        }
    }
}
