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

        /// <summary>
        /// Kiểm tra event có hậu tố giới tính (Female / Male) hay không.
        /// Ví dụ: "Play_DS_03_01_Female", "Play_Wd_01_02Female", "Play_DS_02_01_Male"
        /// </summary>
        public bool HasGenderSuffix
        {
            get
            {
                string clean = CleanEventName;
                if (string.IsNullOrEmpty(clean)) return false;
                return clean.EndsWith("Female", StringComparison.OrdinalIgnoreCase) ||
                       clean.EndsWith("Male", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Giới tính được bóc tách từ tên event: "Female" hoặc "Male" (rỗng nếu không có)
        /// </summary>
        public string Gender
        {
            get
            {
                string clean = CleanEventName;
                if (string.IsNullOrEmpty(clean)) return "";
                if (clean.EndsWith("Female", StringComparison.OrdinalIgnoreCase)) return "Female";
                if (clean.EndsWith("Male", StringComparison.OrdinalIgnoreCase)) return "Male";
                return "";
            }
        }

        /// <summary>
        /// Tên Event cơ sở của âm thanh chiêu thức (đã loại bỏ tiền tố Play_ và hậu tố giới tính)
        /// Ví dụ: "Play_DS_03_01_Female" -> "DS_03_01"
        /// </summary>
        public string BaseEventName
        {
            get
            {
                string clean = CleanEventName;
                if (string.IsNullOrEmpty(clean)) return "";

                if (clean.EndsWith("Female", StringComparison.OrdinalIgnoreCase))
                {
                    string baseName = clean.Substring(0, clean.Length - 6);
                    return baseName.TrimEnd('_');
                }
                if (clean.EndsWith("Male", StringComparison.OrdinalIgnoreCase))
                {
                    string baseName = clean.Substring(0, clean.Length - 4);
                    return baseName.TrimEnd('_');
                }
                return clean;
            }
        }

        /// <summary>
        /// Tên Event lồng tiếng Voice tương ứng
        /// Ví dụ: "Play_DS_03_01_Female" -> "DS_03_01_VoFemale"
        /// </summary>
        public string VoiceEventName
        {
            get
            {
                if (!HasGenderSuffix) return "";
                return $"{BaseEventName}_Vo{Gender}";
            }
        }

        public override string ToString()
        {
            return $"[Sound #{soundId}] {description} | Bank: {bankBnk} | Event: {wwiseEvent} ({CleanEventName} -> Base: {BaseEventName}, Vo: {VoiceEventName}) | Wem: {audioFileWem}";
        }
    }
}
