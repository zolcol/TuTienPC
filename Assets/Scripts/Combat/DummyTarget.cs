using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TopDownGame.UI;

namespace TopDownGame.Combat
{
    public class DummyTarget : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Display Settings")]
        [Tooltip("Bật/tắt thanh máu nhỏ trên đầu khối hộp")]
        [SerializeField] private bool showFloatingHealthBar = true;

        [Header("Hit Reaction Feedback")]
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private float knockbackResistance = 0.5f;

        private MeshRenderer meshRenderer;
        private Color originalColor;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            currentHealth = maxHealth;
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            currentHealth = Mathf.Max(0f, currentHealth - amount);

            Vector3 spawnPos = (hitPoint != Vector3.zero) ? hitPoint : (transform.position + Vector3.up * 1.5f);
            FloatingTextManager.Instance.SpawnDamage(amount, spawnPos, false);

            if (meshRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRedRoutine());
            }

            StartCoroutine(HitShakeRoutine(hitDirection));

            if (currentHealth <= 0f)
            {
                // Debug.Log($"💀 {gameObject.name} đã hết máu! Tự động hồi đầy máu sau 1s để test tiếp...");
                Invoke(nameof(ResetDummy), 1f);
            }
        }

        private IEnumerator FlashRedRoutine()
        {
            meshRenderer.material.color = hitColor;
            yield return new WaitForSeconds(flashDuration);
            meshRenderer.material.color = originalColor;
        }

        private IEnumerator HitShakeRoutine(Vector3 hitDirection)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + hitDirection.normalized * (1f - knockbackResistance) * 0.3f;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 15f;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 10f;
                transform.position = Vector3.Lerp(targetPos, startPos, t);
                yield return null;
            }
            transform.position = startPos;
        }

        public void ResetDummy()
        {
            currentHealth = maxHealth;
            if (meshRenderer != null) meshRenderer.material.color = originalColor;
        }

        private GameObject healthBarCanvasGO;
        private RectTransform healthBarCanvasRect;
        private RectTransform healthBarFillRect;
        private Image healthBarFillImage;
        private Transform mainCamTransform;

        private void Start()
        {
            if (showFloatingHealthBar)
            {
                BuildWorldHealthBar();
            }
        }

        private void BuildWorldHealthBar()
        {
            if (healthBarCanvasGO != null) return;

            healthBarCanvasGO = new GameObject("HealthBar_WorldCanvas");
            healthBarCanvasGO.transform.SetParent(transform, false);
            healthBarCanvasGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            Canvas canvas = healthBarCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;

            healthBarCanvasRect = healthBarCanvasGO.GetComponent<RectTransform>();
            healthBarCanvasRect.sizeDelta = new Vector2(100f, 12f);
            healthBarCanvasRect.localScale = Vector3.one * 0.012f;

            GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(healthBarCanvasGO.transform, false);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.8f);
            bgImg.raycastTarget = false;

            GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(healthBarCanvasGO.transform, false);
            healthBarFillRect = fillGO.GetComponent<RectTransform>();
            healthBarFillRect.anchorMin = new Vector2(0f, 0f);
            healthBarFillRect.anchorMax = new Vector2(0f, 1f);
            healthBarFillRect.pivot = new Vector2(0f, 0.5f);
            healthBarFillRect.anchoredPosition = new Vector2(1f, 0f);
            healthBarFillRect.sizeDelta = new Vector2(98f, -2f);

            healthBarFillImage = fillGO.GetComponent<Image>();
            healthBarFillImage.color = Color.green;
            healthBarFillImage.raycastTarget = false;
        }

        private void LateUpdate()
        {
            if (!showFloatingHealthBar || healthBarCanvasGO == null) return;

            if (mainCamTransform == null && UnityEngine.Camera.main != null)
            {
                mainCamTransform = UnityEngine.Camera.main.transform;
            }

            if (mainCamTransform != null)
            {
                healthBarCanvasRect.rotation = mainCamTransform.rotation;
            }

            float hpPercent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            healthBarFillRect.sizeDelta = new Vector2(98f * hpPercent, -2f);
            healthBarFillImage.color = Color.Lerp(Color.red, Color.green, hpPercent);
        }
    }
}
