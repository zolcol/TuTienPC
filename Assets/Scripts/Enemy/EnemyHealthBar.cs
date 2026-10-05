using UnityEngine;
using UnityEngine.UI;
using TopDownGame.Stats;

namespace TopDownGame.Enemy
{
    /// <summary>
    /// Thanh máu World Space Canvas tự động sinh bằng code tại runtime.
    /// Giải quyết triệt để vấn đề OnGUI đè lên uGUI HUD của người chơi.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float barWidth = 80f;
        [SerializeField] private float barHeight = 8f;
        [SerializeField] private float scale = 0.012f;
        [SerializeField] private float offsetY = 2.0f;
        [SerializeField] private bool hideWhenFull = false;

        private EntityStats stats;
        private Transform mainCamTransform;
        private GameObject canvasGO;
        private RectTransform canvasRect;
        private RectTransform fillRect;
        private Image fillImage;
        private float currentFill = 1f;
        private bool isInitialized = false;

        public void Initialize(EntityStats targetStats, float offset = 2.0f)
        {
            stats = targetStats;
            offsetY = offset;
            BuildRuntimeHealthBar();
        }

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<EntityStats>();
            }
        }

        private void Start()
        {
            if (stats != null && !isInitialized)
            {
                BuildRuntimeHealthBar();
            }

            CacheCamera();
        }

        private void CacheCamera()
        {
            if (UnityEngine.Camera.main != null)
            {
                mainCamTransform = UnityEngine.Camera.main.transform;
            }
        }

        private void BuildRuntimeHealthBar()
        {
            if (isInitialized || canvasGO != null) return;

            // 1. Root Canvas GameObject
            canvasGO = new GameObject("HealthBar_WorldCanvas");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, offsetY, 0f);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;

            canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(barWidth, barHeight);

            Vector3 lossy = transform.lossyScale;
            float invX = lossy.x != 0f ? 1f / lossy.x : 1f;
            float invY = lossy.y != 0f ? 1f / lossy.y : 1f;
            float invZ = lossy.z != 0f ? 1f / lossy.z : 1f;
            canvasRect.localScale = new Vector3(scale * invX, scale * invY, scale * invZ);

            // 2. Background
            GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImage = bgGO.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.8f);
            bgImage.raycastTarget = false;

            // 3. Fill Bar (Sử dụng RectTransform horizontal stretch - không phụ thuộc custom sprite)
            GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(canvasGO.transform, false);
            fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(1f, 0f);
            fillRect.sizeDelta = new Vector2(Mathf.Max(0f, barWidth - 2f), -2f);

            fillImage = fillGO.GetComponent<Image>();
            fillImage.color = new Color(0.2f, 0.9f, 0.2f);
            fillImage.raycastTarget = false;

            if (stats != null && stats.Health != null)
            {
                currentFill = stats.Health.Percentage;
                UpdateFillDisplay(currentFill);
            }

            isInitialized = true;
        }

        public void SetOffsetY(float offset)
        {
            offsetY = offset;
            if (canvasGO != null)
            {
                canvasGO.transform.localPosition = new Vector3(0f, offsetY, 0f);
            }
        }

        private void UpdateFillDisplay(float fill)
        {
            if (fillRect == null || fillImage == null) return;

            float innerWidth = Mathf.Max(0f, barWidth - 2f);
            fillRect.sizeDelta = new Vector2(innerWidth * Mathf.Clamp01(fill), -2f);
            fillImage.color = Color.Lerp(Color.red, new Color(0.2f, 0.9f, 0.2f), fill);
        }

        private void LateUpdate()
        {
            if (stats == null || canvasGO == null) return;

            if (stats.IsDead)
            {
                if (canvasGO.activeSelf) canvasGO.SetActive(false);
                return;
            }

            float targetFill = stats.Health.Percentage;

            if (hideWhenFull && Mathf.Approximately(targetFill, 1f))
            {
                if (canvasGO.activeSelf) canvasGO.SetActive(false);
                return;
            }

            if (!canvasGO.activeSelf)
            {
                canvasGO.SetActive(true);
            }

            // Billboard: Luôn xoay mặt phẳng vuông góc với góc nhìn Camera
            if (mainCamTransform == null)
            {
                CacheCamera();
            }

            if (mainCamTransform != null)
            {
                canvasRect.rotation = mainCamTransform.rotation;
            }

            // Smooth Lerp thanh máu
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 15f);
            UpdateFillDisplay(currentFill);
        }
    }
}
