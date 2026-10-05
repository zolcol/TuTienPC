using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownGame.Skills;
using TopDownGame.Player;

namespace TopDownGame.UI
{
    public class SkillBookItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI slotBadgeText;
        [SerializeField] private GameObject slotBadgeObj;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private Button selectButton;

        private SkillData skillData;
        private Action<SkillData> onSelectedCallback;

        public SkillData SkillData => skillData;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnClick);
            }
        }

        public void Setup(SkillData data, int level, PlayerCombat.SkillHotbarSlot? equippedSlot, Action<SkillData> onSelected)
        {
            this.skillData = data;
            this.onSelectedCallback = onSelected;

            if (data == null) return;

            if (nameText != null)
            {
                nameText.text = data.name;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
            }

            if (iconImage != null)
            {
                Sprite icon = data.GetIconSprite();
                if (icon != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = icon;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            UpdateEquippedSlot(equippedSlot);
            SetSelected(false);
        }

        public void UpdateEquippedSlot(PlayerCombat.SkillHotbarSlot? slot)
        {
            if (slot.HasValue)
            {
                if (slotBadgeObj != null) slotBadgeObj.SetActive(true);
                if (slotBadgeText != null) slotBadgeText.text = $"[ {slot.Value} ]";
            }
            else
            {
                if (slotBadgeObj != null) slotBadgeObj.SetActive(false);
                if (slotBadgeText != null) slotBadgeText.text = "";
            }
        }

        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(selected);
            }
        }

        private void OnClick()
        {
            onSelectedCallback?.Invoke(skillData);
        }
    }
}
