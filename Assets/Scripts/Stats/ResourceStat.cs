using System;
using UnityEngine;

namespace TopDownGame.Stats
{
    [Serializable]
    public class ResourceStat
    {
        [Tooltip("Giá trị tối đa")]
        [SerializeField] private float maxValue = 100f;

        [Tooltip("Giá trị hiện tại")]
        [SerializeField] private float currentValue;

        [Tooltip("Tốc độ tự hồi phục mỗi giây (để 0 nếu không tự hồi)")]
        [SerializeField] private float regenRate = 1f;

        public float CurrentValue => currentValue;
        public float MaxValue => maxValue;
        public float RegenRate => regenRate;
        public float Percentage => maxValue > 0f ? currentValue / maxValue : 0f;

        /// <summary>
        /// Sự kiện phát ra khi giá trị thay đổi: Action(currentValue, maxValue)
        /// </summary>
        public event Action<float, float> OnValueChanged;

        public void Initialize()
        {
            currentValue = maxValue;
            OnValueChanged?.Invoke(currentValue, maxValue);
        }

        public void Tick(float deltaTime)
        {
            if (regenRate > 0f && currentValue < maxValue)
            {
                Modify(regenRate * deltaTime);
            }
        }

        public void Modify(float amount)
        {
            float previous = currentValue;
            currentValue = Mathf.Clamp(currentValue + amount, 0f, maxValue);

            if (!Mathf.Approximately(previous, currentValue))
            {
                OnValueChanged?.Invoke(currentValue, maxValue);
            }
        }

        public bool Consume(float amount)
        {
            if (currentValue < amount) return false;
            Modify(-amount);
            return true;
        }

        public void SetMaxValue(float newMax, bool fillCurrent = false)
        {
            maxValue = Mathf.Max(1f, newMax);
            if (fillCurrent || currentValue > maxValue)
            {
                currentValue = maxValue;
            }
            OnValueChanged?.Invoke(currentValue, maxValue);
        }

        public void SetCurrentValue(float value)
        {
            float previous = currentValue;
            currentValue = Mathf.Clamp(value, 0f, maxValue);
            if (!Mathf.Approximately(previous, currentValue))
            {
                OnValueChanged?.Invoke(currentValue, maxValue);
            }
        }

        public void ResetToMax()
        {
            Initialize();
        }

        public void SetRegenRate(float newRate)
        {
            regenRate = Mathf.Max(0f, newRate);
        }
    }
}
