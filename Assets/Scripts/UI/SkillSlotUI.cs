using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownGame.Skills;

namespace TopDownGame.UI
{
    public class SkillSlotUI : MonoBehaviour
    {
        [Header("UI Components")]
        [Tooltip("Ảnh hiển thị icon của kỹ năng")]
        [SerializeField] private Image iconImage;

        [Tooltip("Lớp phủ hồi chiêu (Image dạng Filled, Fill Method: Radial 360)")]
        [SerializeField] private Image cooldownOverlay;

        [Tooltip("Text hiển thị số giây đếm ngược hồi chiêu")]
        [SerializeField] private TextMeshProUGUI cooldownText;

        [Tooltip("Text hiển thị phím bấm (ví dụ: E, Q, R)")]
        [SerializeField] private TextMeshProUGUI keyBadgeText;

        [Tooltip("Text hiển thị lượng Mana tiêu hao (tùy chọn)")]
        [SerializeField] private TextMeshProUGUI manaCostText;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color notEnoughManaColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        public void SetupSlot(SkillData skillData, string keyName, int skillLevel = 1)
        {
            if (keyBadgeText != null)
            {
                keyBadgeText.text = keyName;
            }

            if (skillData == null)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
                if (manaCostText != null) manaCostText.gameObject.SetActive(false);
                return;
            }

            if (iconImage != null)
            {
                Sprite icon = skillData.GetIconSprite();
                if (icon != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = icon;
                }
                else
                {
                    // Nếu chưa có file Icon Sprite, hiển thị icon mờ hoặc giữ nguyên
                    iconImage.enabled = false;
                }
            }

            if (manaCostText != null)
            {
                float mana = skillData.GetManaCost(skillLevel);
                if (mana > 0f)
                {
                    manaCostText.gameObject.SetActive(true);
                    manaCostText.text = $"{mana:F0}";
                }
                else
                {
                    manaCostText.gameObject.SetActive(false);
                }
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }

        public void UpdateCooldown(float remainingCooldown, float totalCooldown)
        {
            if (totalCooldown <= 0f || remainingCooldown <= 0f)
            {
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
            }
            else
            {
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = remainingCooldown / totalCooldown;
                }

                if (cooldownText != null)
                {
                    cooldownText.gameObject.SetActive(true);
                    cooldownText.text = remainingCooldown >= 1f 
                        ? remainingCooldown.ToString("F0") 
                        : remainingCooldown.ToString("F1");
                }
            }
        }

        public void SetManaAffordable(bool hasEnoughMana)
        {
            if (iconImage != null)
            {
                iconImage.color = hasEnoughMana ? normalColor : notEnoughManaColor;
            }
        }
    }
}
