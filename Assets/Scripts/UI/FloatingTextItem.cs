using System;
using UnityEngine;
using TMPro;
using TopDownGame.Data;

namespace TopDownGame.UI
{
    /// <summary>
    /// Item hiển thị chữ/số nảy trên không gian 3D (Billboard quay theo Camera, Scale Pop, Fade Alpha)
    /// Tái sử dụng 100% trong FloatingTextManager Pool, Zero-GC
    /// </summary>
    public class FloatingTextItem : MonoBehaviour
    {
        [SerializeField] private TextMeshPro tmpText;

        private FloatingTextResData config;
        private Vector3 startPosition;
        private Color baseColor;
        private float timer;
        private bool isPlaying;
        private Transform cameraTransform;
        private Action<FloatingTextItem> onCompleteCallback;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (tmpText == null)
            {
                tmpText = GetComponent<TextMeshPro>();
                if (tmpText == null)
                {
                    tmpText = gameObject.AddComponent<TextMeshPro>();
                }
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.enableWordWrapping = false;
                tmpText.sortingOrder = 100;
            }
        }

        public void Initialize(FloatingTextResData configData, string content, Vector3 position, Transform camTransform, Action<FloatingTextItem> onComplete)
        {
            EnsureComponents();
            config = configData;
            cameraTransform = camTransform;
            onCompleteCallback = onComplete;

            // Tính vị trí xuất phát với độ lệch ngẫu nhiên (Jitter) để các số liên tiếp không đè lên nhau
            Vector3 jitter = Vector3.zero;
            if (config.randomJitter > 0f)
            {
                jitter = new Vector3(
                    UnityEngine.Random.Range(-config.randomJitter, config.randomJitter),
                    UnityEngine.Random.Range(0f, config.randomJitter * 0.5f),
                    UnityEngine.Random.Range(-config.randomJitter * 0.5f, config.randomJitter * 0.5f)
                );
            }

            startPosition = position + jitter;
            transform.position = startPosition;

            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }

            // Gán nội dung và phong cách hiển thị
            tmpText.text = $"{config.prefix}{content}{config.suffix}";
            tmpText.fontSize = config.fontSize;
            tmpText.fontStyle = config.isBold ? FontStyles.Bold : FontStyles.Normal;

            baseColor = config.Color;
            tmpText.color = baseColor;

            transform.localScale = Vector3.one * config.startScale;

            timer = 0f;
            isPlaying = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Billboard: Luôn quay mặt text vuông góc với Camera
            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }

            timer += Time.deltaTime;
            float duration = Mathf.Max(0.01f, config.duration);

            if (timer >= duration)
            {
                isPlaying = false;
                onCompleteCallback?.Invoke(this);
                return;
            }

            // 1. Cập nhật vị trí (Bay theo hướng vận tốc moveVelocity)
            transform.position = startPosition + config.moveVelocity * timer;

            // 2. Cập nhật hiệu ứng Scale Pop
            float currentScale;
            float popDur = Mathf.Clamp(config.popDuration, 0.01f, duration * 0.8f);

            if (timer < popDur)
            {
                float t = timer / popDur;
                // Pop phóng to nhanh lúc đầu
                currentScale = Mathf.Lerp(config.startScale, config.peakScale, t);
            }
            else
            {
                float t = (timer - popDur) / (duration - popDur);
                // Thu nhỏ dần về endScale
                currentScale = Mathf.Lerp(config.peakScale, config.endScale, t);
            }
            transform.localScale = Vector3.one * currentScale;

            // 3. Cập nhật hiệu ứng Fade Out Alpha
            float fadeStart = Mathf.Clamp(config.fadeStartTime, 0f, duration);
            if (timer >= fadeStart)
            {
                float fadeT = (timer - fadeStart) / Mathf.Max(0.01f, duration - fadeStart);
                Color c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, fadeT);
                tmpText.color = c;
            }
            else
            {
                tmpText.color = baseColor;
            }
        }
    }
}
