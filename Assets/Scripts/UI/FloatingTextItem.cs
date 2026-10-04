using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TopDownGame.Data;

namespace TopDownGame.UI
{
    /// <summary>
    /// Item hiển thị chữ/số nảy 3D Billboard sử dụng TextMeshPro.
    /// Tích hợp thuật toán chuyển động vector theo đường cong chuẩn Kingsoft FlyChar.csv.
    /// Tái sử dụng 100% trong FloatingTextManager Pool, Zero-GC.
    /// </summary>
    public class FloatingTextItem : MonoBehaviour
    {
        [SerializeField] private TextMeshPro tmpText;

        private FlyCharResData config;
        private Vector3 startPosition;
        private Color baseColor;
        private Color baseOutlineColor;
        private float timer;
        private bool isPlaying;
        private Transform cameraTransform;
        private Action<FloatingTextItem> onCompleteCallback;
        private float horizontalSign = 1.0f; // Đảo chiều ngẫu nhiên (-1 hoặc 1) cho các hiệu ứng bay dạt góc

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

        public void Initialize(FlyCharResData configData, string content, Vector3 position, Transform camTransform, Action<FloatingTextItem> onComplete)
        {
            EnsureComponents();
            config = configData;
            cameraTransform = camTransform;
            onCompleteCallback = onComplete;

            // Đảo chiều trái/phải ngẫu nhiên cho các góc văng
            horizontalSign = UnityEngine.Random.value > 0.5f ? 1.0f : -1.0f;

            // Độ lệch vị trí xuất phát ngẫu nhiên
            Vector3 jitter = Vector3.zero;
            if (config.randomJitter > 0f)
            {
                jitter = new Vector3(
                    UnityEngine.Random.Range(-config.randomJitter, config.randomJitter),
                    UnityEngine.Random.Range(-config.randomJitter * 0.3f, config.randomJitter * 0.5f),
                    UnityEngine.Random.Range(-config.randomJitter * 0.3f, config.randomJitter * 0.3f)
                );
            }

            startPosition = position + jitter;
            transform.position = startPosition;

            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }

            // Gán nội dung TextMeshPro và Style
            tmpText.text = $"{config.prefix}{content}{config.suffix}";
            tmpText.fontSize = config.fontSize;
            tmpText.fontStyle = config.isBold ? FontStyles.Bold : FontStyles.Normal;

            baseColor = config.color;
            baseOutlineColor = config.outlineColor;

            tmpText.color = baseColor;
            tmpText.outlineColor = baseOutlineColor;
            tmpText.outlineWidth = config.outlineWidth;

            transform.localScale = Vector3.one * Mathf.Max(0.001f, config.scaleCurve.Evaluate(0f));

            timer = 0f;
            isPlaying = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Billboard: Luôn hướng mặt chữ theo Camera
            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }

            timer += Time.deltaTime;
            float duration = Mathf.Max(0.05f, config.duration);

            if (timer >= duration)
            {
                isPlaying = false;
                onCompleteCallback?.Invoke(this);
                return;
            }

            // 1. Tính toán quỹ đạo chuyển động vector theo FlyChar.csv
            Vector2 movement = CalcMovement(timer, config.offsetList, config.angleList);

            Vector3 camRight = (cameraTransform != null) ? cameraTransform.right : Vector3.right;
            Vector3 camUp = (cameraTransform != null) ? cameraTransform.up : Vector3.up;

            Vector3 offsetWorld = (camRight * (movement.x * horizontalSign) + camUp * movement.y) * config.pixelToWorldScale;
            transform.position = startPosition + offsetWorld;

            // 2. Co giãn Scale theo đường cong AnimationCurve
            float scale = Mathf.Max(0.001f, config.scaleCurve.Evaluate(timer));
            transform.localScale = Vector3.one * scale;

            // 3. Mờ dần Alpha cho cả chữ và viền Outline
            float alpha = Mathf.Clamp01(config.alphaCurve.Evaluate(timer));

            Color c = baseColor;
            c.a = baseColor.a * alpha;
            tmpText.color = c;

            Color oc = baseOutlineColor;
            oc.a = baseOutlineColor.a * alpha;
            tmpText.outlineColor = oc;
        }

        private static float CalcDistance(List<Vector2> offsetList, float nTime)
        {
            if (offsetList == null || offsetList.Count == 0)
            {
                return 0f;
            }

            for (int i = 0; i < offsetList.Count - 1; i++)
            {
                float t0 = offsetList[i].x;
                float t1 = offsetList[i + 1].x;
                if (nTime >= t0 && nTime < t1)
                {
                    float v0 = offsetList[i].y;
                    float v1 = offsetList[i + 1].y;
                    float span = Mathf.Max(0.0001f, t1 - t0);
                    return v0 + ((nTime - t0) / span) * (v1 - v0);
                }
            }

            return offsetList[offsetList.Count - 1].y;
        }

        private static Vector2 CalcMovement(float currentTime, List<Vector2> offsetList, List<Vector2> angleList)
        {
            Vector2 vector = Vector2.zero;
            if (angleList == null || angleList.Count == 0 || offsetList == null || offsetList.Count == 0)
            {
                return vector;
            }

            for (int i = 0; i < angleList.Count; i++)
            {
                float t0 = angleList[i].x;
                float angle = angleList[i].y;

                if (currentTime - t0 <= 0f)
                {
                    return vector;
                }

                if (i < angleList.Count - 1)
                {
                    float dist0 = CalcDistance(offsetList, angleList[i].x);
                    float nextTime = (currentTime - angleList[i + 1].x > 0f) ? angleList[i + 1].x : currentTime;
                    float dist1 = CalcDistance(offsetList, nextTime);

                    float rad = (angle * Mathf.PI) / 180f;
                    float cos = Mathf.Cos(rad);
                    float sin = Mathf.Sin(rad);
                    float delta = dist1 - dist0;

                    // Tọa độ cực: 90 độ là hướng lên trên (+Y), 120 độ là chếch sang trái (+X âm)
                    vector.x += delta * cos;
                    vector.y += delta * sin;
                }
                else
                {
                    // Phân đoạn cuối cùng
                    float dist0 = CalcDistance(offsetList, angleList[i].x);
                    float dist1 = CalcDistance(offsetList, currentTime);

                    float rad = (angle * Mathf.PI) / 180f;
                    float cos = Mathf.Cos(rad);
                    float sin = Mathf.Sin(rad);
                    float delta = dist1 - dist0;

                    vector.x += delta * cos;
                    vector.y += delta * sin;
                }
            }

            return vector;
        }
    }
}
