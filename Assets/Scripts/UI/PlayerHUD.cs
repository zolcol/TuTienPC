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

        [Header("Level & Exp Bar (Tùy chọn)")]
        [SerializeField] private Image expFill;
        [SerializeField] private TextMeshProUGUI expText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private float expLerpSpeed = 10f;

        [Header("Skill Slots (Hiển thị theo thứ tự Q - E - R)")]
        [SerializeField] private SkillSlotUI skillSlot_Q;
        [SerializeField] private SkillSlotUI skillSlot_E;
        [SerializeField] private SkillSlotUI skillSlot_R;

        private float targetHealthFill = 1f;
        private float targetManaFill = 1f;
        private float targetExpFill = 0f;
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
                stats.OnExpChanged += HandleExpChanged;
                stats.OnLevelUp += HandleLevelUp;

                // Cập nhật giá trị khởi điểm ngay lập tức
                HandleHealthChanged(stats.Health.CurrentValue, stats.Health.MaxValue);
                HandleManaChanged(stats.Mana.CurrentValue, stats.Mana.MaxValue);
                HandleExpChanged(stats.CurrentLevel, stats.CurrentExp, stats.GetExpUpgradeForCurrentLevel());

                if (healthFill != null) healthFill.fillAmount = targetHealthFill;
                if (healthGhostFill != null) healthGhostFill.fillAmount = targetHealthFill;
                if (manaFill != null) manaFill.fillAmount = targetManaFill;
                if (expFill != null) expFill.fillAmount = targetExpFill;
            }

            // 2. Thiết lập 3 ô kỹ năng theo thứ tự Q - E - R từ Database
            RefreshSkillSlots();

            if (player.Combat != null)
            {
                player.Combat.OnSkillSlotsChanged += RefreshSkillSlots;
            }
            if (player.SkillManager != null)
            {
                player.SkillManager.OnSkillUpgraded += HandleSkillUpgraded;
            }
        }

        private void HandleSkillUpgraded(int skillId, int newLevel)
        {
            RefreshSkillSlots();
        }

        public void RefreshSkillSlots()
        {
            if (player == null) return;
            int qLv = (player.SkillManager != null && player.SkillSlotQ != null) ? player.SkillManager.GetSkillLevel(player.SkillSlotQ.id) : 1;
            int eLv = (player.SkillManager != null && player.SkillSlotE != null) ? player.SkillManager.GetSkillLevel(player.SkillSlotE.id) : 1;
            int rLv = (player.SkillManager != null && player.SkillSlotR != null) ? player.SkillManager.GetSkillLevel(player.SkillSlotR.id) : 1;

            if (skillSlot_Q != null) skillSlot_Q.SetupSlot(player.SkillSlotQ, "Q", qLv);
            if (skillSlot_E != null) skillSlot_E.SetupSlot(player.SkillSlotE, "E", eLv);
            if (skillSlot_R != null) skillSlot_R.SetupSlot(player.SkillSlotR, "R", rLv);
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                if (player.Stats != null)
                {
                    player.Stats.Health.OnValueChanged -= HandleHealthChanged;
                    player.Stats.Mana.OnValueChanged -= HandleManaChanged;
                    player.Stats.OnExpChanged -= HandleExpChanged;
                    player.Stats.OnLevelUp -= HandleLevelUp;
                }
                if (player.Combat != null)
                {
                    player.Combat.OnSkillSlotsChanged -= RefreshSkillSlots;
                }
                if (player.SkillManager != null)
                {
                    player.SkillManager.OnSkillUpgraded -= HandleSkillUpgraded;
                }
            }
        }

        private void HandleExpChanged(int level, long exp, long maxExp)
        {
            if (levelText != null) levelText.text = $"Lv.{level}";
            targetExpFill = maxExp > 0 ? Mathf.Clamp01((float)exp / maxExp) : 1f;
            if (expText != null) expText.text = $"{FormatExpValue(exp)} / {FormatExpValue(maxExp)}";
        }

        private void HandleLevelUp(int newLevel)
        {
            if (levelText != null) levelText.text = $"Lv.{newLevel}";
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

            // 4. Thanh EXP vàng cam
            if (expFill != null)
            {
                expFill.fillAmount = Mathf.Lerp(expFill.fillAmount, targetExpFill, Time.deltaTime * expLerpSpeed);
                if (Mathf.Abs(expFill.fillAmount - targetExpFill) < 0.001f)
                {
                    expFill.fillAmount = targetExpFill;
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
                healthText.text = $"{FormatStatValue(current)} / {FormatStatValue(max)}";
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            targetManaFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (manaText != null)
            {
                manaText.text = $"{FormatStatValue(current)} / {FormatStatValue(max)}";
            }
        }

        public static string FormatStatValue(float value)
        {
            int intVal = Mathf.CeilToInt(value);
            if (intVal >= 1_000_000)
            {
                return (intVal / 1_000_000f).ToString("0.#") + "M";
            }
            if (intVal >= 100_000)
            {
                return (intVal / 1_000f).ToString("0.#") + "K";
            }
            return intVal.ToString("N0");
        }

        public static string FormatExpValue(long value)
        {
            if (value >= 1_000_000)
            {
                return (value / 1_000_000f).ToString("0.##") + "M";
            }
            if (value >= 100_000)
            {
                return (value / 1_000f).ToString("0.#") + "K";
            }
            return value.ToString("N0");
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
