using System;

namespace TopDownGame.Audio
{
    [Serializable]
    public class SoundData
    {
        public int soundId;
        public string description;
        public string bankBnk;
        public string wwiseEvent;
        public string audioFileWem;

        /// <summary>
        /// Tên Event đã loại bỏ tiền tố "Play_" hoặc "Play"
        /// Ví dụ: "Play_Em_01_01" -> "Em_01_01"
        /// </summary>
        public string CleanEventName
        {
            get
            {
                if (string.IsNullOrEmpty(wwiseEvent)) return "";
                if (wwiseEvent.StartsWith("Play_", StringComparison.OrdinalIgnoreCase))
                {
                    return wwiseEvent.Substring(5);
                }
                if (wwiseEvent.StartsWith("Play", StringComparison.OrdinalIgnoreCase) && wwiseEvent.Length > 4)
                {
                    return wwiseEvent.Substring(4);
                }
                return wwiseEvent;
            }
        }

        public override string ToString()
        {
            return $"[Sound #{soundId}] {description} | Bank: {bankBnk} | Event: {wwiseEvent} ({CleanEventName}) | Wem: {audioFileWem}";
        }
    }
}
