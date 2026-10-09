using System;

namespace TopDownGame.Data
{
    /// <summary>
    /// DTO chứa cấu hình tên animation, biến thể và tọa kỵ theo ActId từ ActionName.csv
    /// </summary>
    [Serializable]
    public class ActionNameData
    {
        public int actId;
        public string actName = string.Empty;
        public string[] actVariants = new string[3]; // ActName1..3
        public string rideActNameDefault = string.Empty; // RideActName
        public string[] rideActNames = new string[58]; // RideActName1..58
        public int hidePart = 0;
        public int rideHidePart = 0;
        public string description = string.Empty;

        /// <summary>
        /// Lấy tên Animation Clip tương ứng với trạng thái (đi bộ / cưỡi thú / biến thể vũ khí)
        /// </summary>
        /// <param name="mountId">0: Đi bộ, 1..58: Loại thú cưỡi tương ứng</param>
        /// <param name="variantIndex">0: Mặc định, 1..3: Biến thể vũ khí ActName1..3</param>
        /// <returns>Tên Animation Clip</returns>
        public string GetClipName(int mountId = 0, int variantIndex = 0)
        {
            // 1. Nếu đang cưỡi thú (mountId > 0)
            if (mountId > 0)
            {
                int mountIdx = mountId - 1;
                if (mountIdx >= 0 && mountIdx < rideActNames.Length && !string.IsNullOrEmpty(rideActNames[mountIdx]))
                {
                    return rideActNames[mountIdx];
                }

                // Fallback về RideActName mặc định
                if (!string.IsNullOrEmpty(rideActNameDefault))
                {
                    return rideActNameDefault;
                }
            }

            // 2. Nếu có biến thể vũ khí / trạng thái phụ (variantIndex 1..3)
            if (variantIndex > 0)
            {
                int varIdx = variantIndex - 1;
                if (varIdx >= 0 && varIdx < actVariants.Length && !string.IsNullOrEmpty(actVariants[varIdx]))
                {
                    return actVariants[varIdx];
                }
            }

            // 3. Clip mặc định đi bộ
            return !string.IsNullOrEmpty(actName) ? actName : string.Empty;
        }
    }
}
