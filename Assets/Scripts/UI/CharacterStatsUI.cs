using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownGame.Player;
using TopDownGame.Stats;
using TopDownGame.Input;
using TopDownGame.Combat;

namespace TopDownGame.UI
{
    /// <summary>
    /// Bảng hiển thị thông số chi tiết của nhân vật (RPG Character Stats Window)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CharacterStatsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;

        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI expText;
        [SerializeField] private Slider expSlider;

        [Header("Survival / Resources")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI hpRegenText;
        [SerializeField] private TextMeshProUGUI mpText;
        [SerializeField] private TextMeshProUGUI mpRegenText;

        [Header("Offense Stats")]
        [SerializeField] private TextMeshProUGUI physicalDamageText;
        [SerializeField] private TextMeshProUGUI magicDamageText;
        [SerializeField] private TextMeshProUGUI attackSpeedText;
        [SerializeField] private TextMeshProUGUI critRateText;
        [SerializeField] private TextMeshProUGUI critDamageText;

        [Header("Defense & Mobility")]
        [SerializeField] private TextMeshProUGUI armorText;
        [SerializeField] private TextMeshProUGUI magicResistText;
        [SerializeField] private TextMeshProUGUI moveSpeedText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button toggleButton;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.15f;

        private bool isOpen = false;
        private PlayerStats playerStats;
        private Coroutine fadeCoroutine;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (panelRect == null) panelRect = GetComponent<RectTransform>();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }
        }

        private void Start()
        {
            FindPlayerReferences();

            // Khởi đầu ẩn bảng
            SetOpenStateImmediate(false);
        }

        private void OnEnable()
        {
            FindPlayerReferences();
            if (playerStats != null)
            {
                playerStats.OnLevelUp += HandleLevelUp;
                playerStats.OnExpChanged += HandleExpChanged;
            }
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.OnLevelUp -= HandleLevelUp;
                playerStats.OnExpChanged -= HandleExpChanged;
            }
        }

        private void Update()
        {
            if (inputReader != null && inputReader.ToggleCharacterStatsTriggered)
            {
                Toggle();
            }

            // Cập nhật realtime khi bảng đang mở
            if (isOpen)
            {
                RefreshStatsData();
            }
        }

        public void FindPlayerReferences()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
            }

            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
                if (inputReader == null)
                {
                    inputReader = player.GetComponent<PlayerInputReader>() ?? FindObjectOfType<PlayerInputReader>();
                }
            }
        }

        public void Toggle()
        {
            SetOpenState(!isOpen);
        }

        public void Open()
        {
            SetOpenState(true);
        }

        public void Close()
        {
            SetOpenState(false);
        }

        public void SetOpenState(bool open)
        {
            isOpen = open;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(AnimatePanel(open));

            if (open)
            {
                RefreshStatsData();
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
                t = t * t * (3f - 2f * t); // SmoothStep

                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                panelRect.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            panelRect.localScale = targetScale;
            fadeCoroutine = null;
        }

        public void RefreshStatsData()
        {
            if (playerStats == null)
            {
                FindPlayerReferences();
                if (playerStats == null) return;
            }

            // 1. Level & EXP
            if (levelText != null)
            {
                levelText.text = $"Cấp: <color=#FFD700>{playerStats.CurrentLevel}</color>";
            }

            long reqExp = playerStats.GetExpUpgradeForCurrentLevel();
            long curExp = playerStats.CurrentExp;
            if (expText != null)
            {
                expText.text = $"EXP: {PlayerHUD.FormatExpValue(curExp)} / {PlayerHUD.FormatExpValue(reqExp)}";
            }
            if (expSlider != null && reqExp > 0)
            {
                expSlider.value = Mathf.Clamp01((float)curExp / reqExp);
            }

            // 2. Resources (HP / MP)
            if (hpText != null)
            {
                hpText.text = $"{PlayerHUD.FormatStatValue(playerStats.Health.CurrentValue)} / {PlayerHUD.FormatStatValue(playerStats.Health.MaxValue)}";
            }
            if (hpRegenText != null)
            {
                hpRegenText.text = $"+{playerStats.Health.RegenRate:0.#}/s";
            }

            if (mpText != null)
            {
                mpText.text = $"{PlayerHUD.FormatStatValue(playerStats.Mana.CurrentValue)} / {PlayerHUD.FormatStatValue(playerStats.Mana.MaxValue)}";
            }
            if (mpRegenText != null)
            {
                mpRegenText.text = $"+{playerStats.Mana.RegenRate:0.#}/s";
            }

            // 3. Offense
            if (physicalDamageText != null)
            {
                physicalDamageText.text = $"{Mathf.RoundToInt(playerStats.PhysicalDamage):N0}";
            }
            if (magicDamageText != null)
            {
                magicDamageText.text = $"{Mathf.RoundToInt(playerStats.MagicDamage):N0}";
            }
            if (attackSpeedText != null)
            {
                float speedMultiplier = CombatFormula.CalculateAttackSpeedMultiplier(playerStats.AttackSpeed);
                attackSpeedText.text = $"{speedMultiplier:0.00}";
            }
            if (critRateText != null)
            {
                critRateText.text = $"{playerStats.CritRate:0.#}%";
            }
            if (critDamageText != null)
            {
                critDamageText.text = $"{playerStats.CritDamage:0.#}%";
            }

            // 4. Defense & Mobility
            if (armorText != null)
            {
                armorText.text = $"{Mathf.RoundToInt(playerStats.Armor):N0}";
            }
            if (magicResistText != null)
            {
                magicResistText.text = $"{Mathf.RoundToInt(playerStats.MagicResist):N0}";
            }
            if (moveSpeedText != null)
            {
                moveSpeedText.text = $"{playerStats.MoveSpeed:0.#} m/s";
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            if (isOpen) RefreshStatsData();
        }

        private void HandleExpChanged(int level, long curExp, long maxExp)
        {
            if (isOpen) RefreshStatsData();
        }
    }
}
