using System;
using UnityEngine;

namespace TopDownGame.Data
{
    public enum FloatingTextType
    {
        NormalDamage = 1,
        CritDamage = 2,
        Heal = 3,
        PlayerDamaged = 4,
        ManaRestored = 5,
        Miss = 6,
        ExpGain = 7,
        LevelUp = 8,
        Custom = 99
    }

    [Serializable]
    public class FloatingTextResData
    {
        public int id;
        public string typeName;
        public float duration = 0.85f;
        public float startScale = 0.7f;
        public float peakScale = 1.2f;
        public float endScale = 0.9f;
        public float popDuration = 0.12f;
        public float fadeStartTime = 0.45f;
        public string colorHex = "#FFFFFF";
        public float fontSize = 4.2f;
        public Vector3 moveVelocity = new Vector3(0f, 1.8f, 0f);
        public float randomJitter = 0.35f;
        public string prefix = "";
        public string suffix = "";
        public bool isBold = true;
        public string desc = "";

        // Cached runtime color
        [NonSerialized] private Color? cachedColor;

        public Color Color
        {
            get
            {
                if (!cachedColor.HasValue)
                {
                    if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color c))
                    {
                        cachedColor = c;
                    }
                    else
                    {
                        cachedColor = Color.white;
                    }
                }
                return cachedColor.Value;
            }
        }
    }
}
