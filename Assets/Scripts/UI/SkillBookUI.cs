using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.Stats;
using TopDownGame.Input;
using TopDownGame.Combat;

namespace TopDownGame.UI
{
    /// <summary>
    /// Bảng quản lý Võ Học & Kỹ Năng (RPG Skill Book Window)
    /// Hỗ trợ xem thông tin chi tiết, gán phím Q-E-R và nâng cấp kỹ năng.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SkillBookUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;

        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI skillPointsText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button toggleButton;

        [Header("Skill List (Left Panel)")]
        [SerializeField] private Transform skillListContent;
        [SerializeField] private SkillBookItemUI itemTemplate;

        [Header("Detail Header (Right Panel)")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailTypeText;
        [SerializeField] private TextMeshProUGUI detailLevelText;

        [Header("Detail Description & Stats")]
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailCooldownText;
        [SerializeField] private TextMeshProUGUI detailManaCostText;
        [SerializeField] private TextMeshProUGUI detailDamageText;
        [SerializeField] private TextMeshProUGUI detailScalingText;

        [Header("Assign Hotbar Buttons")]
        [SerializeField] private Button assignQButton;
        [SerializeField] private TextMeshProUGUI assignQText;
        [SerializeField] private Button assignEButton;
        [SerializeField] private TextMeshProUGUI assignEText;
        [SerializeField] private Button assignRButton;
        [SerializeField] private TextMeshProUGUI assignRText;

        [Header("Upgrade Section")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonText;
        [SerializeField] private TextMeshProUGUI upgradeRequirementText;
        [SerializeField] private TextMeshProUGUI nextLevelPreviewText;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.15f;

        private bool isOpen = false;
        private SkillData selectedSkill;
        private readonly List<SkillBookItemUI> spawnedItems = new List<SkillBookItemUI>();
        private Coroutine fadeCoroutine;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (panelRect == null) panelRect = GetComponent<RectTransform>();

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);

            if (assignQButton != null) assignQButton.onClick.AddListener(() => OnAssignSlotClicked(PlayerCombat.SkillHotbarSlot.Q));
            if (assignEButton != null) assignEButton.onClick.AddListener(() => OnAssignSlotClicked(PlayerCombat.SkillHotbarSlot.E));
            if (assignRButton != null) assignRButton.onClick.AddListener(() => OnAssignSlotClicked(PlayerCombat.SkillHotbarSlot.R));

            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (itemTemplate != null)
            {
                itemTemplate.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            FindPlayerReferences();
            SetOpenStateImmediate(false);
        }

        private void OnEnable()
        {
            FindPlayerReferences();
            RegisterPlayerEvents(true);
        }

        private void OnDisable()
        {
            RegisterPlayerEvents(false);
        }

        private void RegisterPlayerEvents(bool subscribe)
        {
            if (player == null) return;

            if (player.Stats != null)
            {
                if (subscribe)
                {
                    player.Stats.OnSkillPointsChanged += HandleSkillPointsChanged;
                    player.Stats.OnLevelUp += HandleLevelUp;
                }
                else
                {
                    player.Stats.OnSkillPointsChanged -= HandleSkillPointsChanged;
                    player.Stats.OnLevelUp -= HandleLevelUp;
                }
            }

            if (player.Combat != null)
            {
                if (subscribe) player.Combat.OnSkillSlotsChanged += HandleSkillSlotsChanged;
                else player.Combat.OnSkillSlotsChanged -= HandleSkillSlotsChanged;
            }

            if (player.SkillManager != null)
            {
                if (subscribe) player.SkillManager.OnSkillUpgraded += HandleSkillUpgraded;
                else player.SkillManager.OnSkillUpgraded -= HandleSkillUpgraded;
            }
        }

        private void Update()
        {
            if (inputReader != null && inputReader.ToggleSkillBookTriggered)
            {
                Toggle();
            }
        }

        public void FindPlayerReferences()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
            }

            if (player != null && inputReader == null)
            {
                inputReader = player.GetComponent<PlayerInputReader>() ?? FindObjectOfType<PlayerInputReader>();
            }
        }

        public void Toggle() => SetOpenState(!isOpen);
        public void Open() => SetOpenState(true);
        public void Close() => SetOpenState(false);

        public void SetOpenState(bool open)
        {
            isOpen = open;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(AnimatePanel(open));

            if (open)
            {
                RefreshAll();
            }
        }

        private void SetOpenStateImmediate(bool open)
        {
            isOpen = open;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = open ? 1f : 0f;
                canvasGroup.interactable = open;
                canvasGroup.blocksRaycasts = open;
            }
            if (panelRect != null)
            {
                panelRect.localScale = open ? Vector3.one : new Vector3(0.9f, 0.9f, 1f);
            }
        }

        private IEnumerator AnimatePanel(bool open)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            float targetAlpha = open ? 1f : 0f;

            Vector3 startScale = panelRect.localScale;
            Vector3 targetScale = open ? Vector3.one : new Vector3(0.92f, 0.92f, 1f);

            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                panelRect.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            panelRect.localScale = targetScale;
        }

        public void RefreshAll()
        {
            FindPlayerReferences();
            UpdateSkillPointsDisplay();
            PopulateSkillList();

            if (selectedSkill == null && spawnedItems.Count > 0)
            {
                SelectSkill(spawnedItems[0].SkillData);
            }
            else if (selectedSkill != null)
            {
                SelectSkill(selectedSkill);
            }
        }

        private void UpdateSkillPointsDisplay()
        {
            if (skillPointsText != null)
            {
                int sp = player?.Stats != null ? player.Stats.SkillPoints : 0;
                skillPointsText.text = $"Điểm Kỹ Năng: <color=#FFD54F>{sp} SP</color>";
            }
        }

        private void PopulateSkillList()
        {
            if (skillListContent == null || itemTemplate == null) return;

            SkillDatabase.Instance.EnsureLoaded();
            var allSkills = SkillDatabase.GetAllSkills();

            // Dọn dẹp items cũ
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null && spawnedItems[i] != itemTemplate)
                {
                    Destroy(spawnedItems[i].gameObject);
                }
            }
            spawnedItems.Clear();

            foreach (var kvp in allSkills)
            {
                SkillData skill = kvp.Value;
                if (skill == null) continue;

                GameObject itemObj = Instantiate(itemTemplate.gameObject, skillListContent);
                itemObj.SetActive(true);

                SkillBookItemUI itemUI = itemObj.GetComponent<SkillBookItemUI>();
                if (itemUI != null)
                {
                    int level = player?.SkillManager != null ? player.SkillManager.GetSkillLevel(skill.id) : 1;
                    PlayerCombat.SkillHotbarSlot? equippedSlot = player?.Combat != null ? player.Combat.GetEquippedSlot(skill.id) : null;
                    itemUI.Setup(skill, level, equippedSlot, SelectSkill);
                    spawnedItems.Add(itemUI);
                }
            }
        }

        public void SelectSkill(SkillData skill)
        {
            selectedSkill = skill;
            if (skill == null) return;

            // Highlight card
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                {
                    spawnedItems[i].SetSelected(spawnedItems[i].SkillData == skill);
                }
            }

            // Cập nhật Right Detail Inspector
            int currentLevel = player?.SkillManager != null ? player.SkillManager.GetSkillLevel(skill.id) : 1;
            int maxLevel = player?.SkillManager != null ? player.SkillManager.GetMaxSkillLevel(skill.id) : 5;

            if (detailIcon != null)
            {
                Sprite icon = skill.GetIconSprite();
                detailIcon.enabled = icon != null;
                detailIcon.sprite = icon;
            }

            if (detailNameText != null) detailNameText.text = skill.name;
            if (detailLevelText != null) detailLevelText.text = $"Cấp độ: <color=#FFD54F>{currentLevel}</color> / {maxLevel}";
            if (detailTypeText != null) detailTypeText.text = GetSkillTypeDisplayName(skill);
            if (detailDescriptionText != null)
            {
                string desc = !string.IsNullOrEmpty(skill.description) ? skill.description : "Chưa có mô tả bí kíp cho võ công này.";
                detailDescriptionText.text = desc;
            }

            // Stats
            float scaledCd = PlayerSkillManager.GetScaledCooldown(skill, currentLevel);
            float scaledMana = PlayerSkillManager.GetScaledManaCost(skill, currentLevel);
            float scaledDmg = PlayerSkillManager.GetScaledDamage(skill, currentLevel);
            float scaledPhys = PlayerSkillManager.GetScaledPhysScale(skill, currentLevel);
            float scaledMagic = PlayerSkillManager.GetScaledMagicScale(skill, currentLevel);
            float scaledHeal = PlayerSkillManager.GetScaledHeal(skill, currentLevel);

            if (detailCooldownText != null) detailCooldownText.text = scaledCd > 0f ? $"{scaledCd:F1}s" : "0s (Không CD)";
            if (detailManaCostText != null) detailManaCostText.text = scaledMana > 0f ? $"{scaledMana:F0} MP" : "0 (Không tốn)";

            if (detailDamageText != null)
            {
                if (skill.IsHeal)
                {
                    detailDamageText.text = scaledHeal > 0f ? $"Hồi máu: {scaledHeal:F0}" : "Hồi máu theo %";
                }
                else
                {
                    detailDamageText.text = scaledDmg > 0f ? $"Sát thương: {scaledDmg:F0}" : "Theo vũ khí";
                }
            }

            if (detailScalingText != null)
            {
                if (skill.IsHeal)
                {
                    detailScalingText.text = $"+{skill.healScale * 100f:F0}% Công Phép";
                }
                else
                {
                    List<string> scales = new List<string>(2);
                    if (scaledPhys > 0f) scales.Add($"{scaledPhys * 100f:F0}% Vật lý");
                    if (scaledMagic > 0f) scales.Add($"{scaledMagic * 100f:F0}% Phép");
                    detailScalingText.text = scales.Count > 0 ? string.Join(" + ", scales) : "Cố định";
                }
            }

            // Cập nhật các nút gán phím Q, E, R
            UpdateAssignButtons();

            // Cập nhật mục nâng cấp
            UpdateUpgradeSection(currentLevel, maxLevel);
        }

        private void UpdateAssignButtons()
        {
            if (player?.Combat == null || selectedSkill == null) return;

            PlayerCombat.SkillHotbarSlot? currentSlot = player.Combat.GetEquippedSlot(selectedSkill.id);

            UpdateSingleAssignButton(assignQButton, assignQText, "Q", currentSlot == PlayerCombat.SkillHotbarSlot.Q);
            UpdateSingleAssignButton(assignEButton, assignEText, "E", currentSlot == PlayerCombat.SkillHotbarSlot.E);
            UpdateSingleAssignButton(assignRButton, assignRText, "R", currentSlot == PlayerCombat.SkillHotbarSlot.R);
        }

        private void UpdateSingleAssignButton(Button btn, TextMeshProUGUI text, string keyName, bool isAssigned)
        {
            if (btn == null) return;
            if (text != null)
            {
                text.text = isAssigned ? $"[ Đang ở {keyName} ]" : $"Gán vào [ {keyName} ]";
                text.color = isAssigned ? new Color(1f, 0.85f, 0.35f, 1f) : Color.white;
            }
        }

        private void UpdateUpgradeSection(int currentLevel, int maxLevel)
        {
            if (selectedSkill == null) return;

            if (currentLevel >= maxLevel)
            {
                if (upgradeButton != null) upgradeButton.interactable = false;
                if (upgradeButtonText != null) upgradeButtonText.text = "ĐÃ ĐẠT TỐI ĐA";
                if (upgradeRequirementText != null) upgradeRequirementText.text = "Kỹ năng đã luyện đến cảnh giới tối thượng";
                if (nextLevelPreviewText != null) nextLevelPreviewText.text = "";
                return;
            }

            string reason = "Chưa kết nối dữ liệu";
            bool canUpgrade = player != null && player.SkillManager != null && player.SkillManager.CanUpgradeSkill(selectedSkill.id, out reason);
            if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
            if (upgradeButtonText != null) upgradeButtonText.text = "+ NÂNG CẤP (1 SP)";

            if (upgradeRequirementText != null)
            {
                int reqLevel = currentLevel * 2;
                upgradeRequirementText.text = canUpgrade ? $"<color=#81C784>Đủ điều kiện: Yêu cầu Nhân vật Cấp {reqLevel}</color>" : $"<color=#E57373>{reason}</color>";
            }

            if (nextLevelPreviewText != null)
            {
                int nextLv = currentLevel + 1;
                float nextDmg = PlayerSkillManager.GetScaledDamage(selectedSkill, nextLv);
                float nextCd = PlayerSkillManager.GetScaledCooldown(selectedSkill, nextLv);
                string change = $"+15% Hiệu quả";
                if (selectedSkill.cooldown > 0f) change += $", -0.2s Hồi chiêu";
                nextLevelPreviewText.text = $"Cấp tiếp theo ({nextLv}): <color=#FFD54F>{change}</color>";
            }
        }

        private void OnAssignSlotClicked(PlayerCombat.SkillHotbarSlot slot)
        {
            if (player?.Combat == null || selectedSkill == null) return;

            player.Combat.AssignSkillToSlot(slot, selectedSkill.id);
            UpdateAssignButtons();

            // Cập nhật badge trên item list
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                {
                    var eqSlot = player.Combat.GetEquippedSlot(spawnedItems[i].SkillData.id);
                    spawnedItems[i].UpdateEquippedSlot(eqSlot);
                }
            }
        }

        private void OnUpgradeClicked()
        {
            if (player?.SkillManager == null || selectedSkill == null) return;

            if (player.SkillManager.UpgradeSkill(selectedSkill.id))
            {
                UpdateSkillPointsDisplay();
                int newLevel = player.SkillManager.GetSkillLevel(selectedSkill.id);

                // Cập nhật card
                for (int i = 0; i < spawnedItems.Count; i++)
                {
                    if (spawnedItems[i] != null && spawnedItems[i].SkillData == selectedSkill)
                    {
                        spawnedItems[i].UpdateLevel(newLevel);
                        break;
                    }
                }

                // Cập nhật right detail
                SelectSkill(selectedSkill);
            }
        }

        private void HandleSkillPointsChanged(int points)
        {
            UpdateSkillPointsDisplay();
            if (selectedSkill != null)
            {
                int curLv = player?.SkillManager != null ? player.SkillManager.GetSkillLevel(selectedSkill.id) : 1;
                int maxLv = player?.SkillManager != null ? player.SkillManager.GetMaxSkillLevel(selectedSkill.id) : 5;
                UpdateUpgradeSection(curLv, maxLv);
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            UpdateSkillPointsDisplay();
            if (selectedSkill != null)
            {
                int curLv = player?.SkillManager != null ? player.SkillManager.GetSkillLevel(selectedSkill.id) : 1;
                int maxLv = player?.SkillManager != null ? player.SkillManager.GetMaxSkillLevel(selectedSkill.id) : 5;
                UpdateUpgradeSection(curLv, maxLv);
            }
        }

        private void HandleSkillSlotsChanged()
        {
            UpdateAssignButtons();
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null && player?.Combat != null)
                {
                    spawnedItems[i].UpdateEquippedSlot(player.Combat.GetEquippedSlot(spawnedItems[i].SkillData.id));
                }
            }
        }

        private void HandleSkillUpgraded(int skillId, int newLevel)
        {
            UpdateSkillPointsDisplay();
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null && spawnedItems[i].SkillData.id == skillId)
                {
                    spawnedItems[i].UpdateLevel(newLevel);
                }
            }
            if (selectedSkill != null && selectedSkill.id == skillId)
            {
                SelectSkill(selectedSkill);
            }
        }

        private string GetSkillTypeDisplayName(SkillData skill)
        {
            if (skill == null) return "Võ Học";
            if (skill.IsHeal) return "Kỹ Năng Trị Liệu / Hồi Phục";
            if (skill.HasProjectile) return "Kỹ Năng Tầm Xa / Đạn Đạo";
            if (skill.skillType == SkillType.Sector || skill.skillType == SkillType.Circle) return "Kỹ Năng Quần Thể (AoE)";
            if (skill.param2 > 0) return "Chiêu Thức Liên Hoàn (Combo)";
            return "Kỹ Năng Tấn Công Cận Chiến";
        }
    }
}
