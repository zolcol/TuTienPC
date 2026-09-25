using System.Collections;
using UnityEngine;

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

            Debug.Log($"💥 <color=orange>[HIT]</color> {gameObject.name} bị chém -{amount} Máu! (Còn lại: {currentHealth}/{maxHealth})");

            if (meshRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRedRoutine());
            }

            StartCoroutine(HitShakeRoutine(hitDirection));

            if (currentHealth <= 0f)
            {
                Debug.Log($"💀 {gameObject.name} đã hết máu! Tự động hồi đầy máu sau 1s để test tiếp...");
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

        private void OnGUI()
        {
            if (!showFloatingHealthBar) return;

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
            if (screenPos.z > 0)
            {
                float barWidth = 100f;
                float barHeight = 12f;
                float hpPercent = currentHealth / maxHealth;

                Rect bgRect = new Rect(screenPos.x - barWidth * 0.5f, Screen.height - screenPos.y, barWidth, barHeight);
                Rect fillRect = new Rect(bgRect.x, bgRect.y, barWidth * hpPercent, barHeight);

                GUI.color = Color.black;
                GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

                GUI.color = Color.Lerp(Color.red, Color.green, hpPercent);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

                GUI.color = Color.white;
                GUI.Label(new Rect(bgRect.x, bgRect.y - 18, barWidth, 20), $"{currentHealth:F0}/{maxHealth:F0}", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11, fontStyle = FontStyle.Bold });
            }
        }
    }
}
