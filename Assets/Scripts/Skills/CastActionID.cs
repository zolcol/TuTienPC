using System;
using System.Collections.Generic;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Bảng mã CastActionID chuẩn Database Game cho toàn bộ hoạt ảnh nhân vật và quái vật.
    /// </summary>
    public enum CastActionID
    {
        None = 0,

        // =========================================================================
        // 1. NHÓM DI CHUYỂN & ĐỨNG CƠ BẢN (Locomotion & Idle)
        // =========================================================================
        st = 1,          // Đứng chờ phi chiến đấu (Normal Stand / Idle)
        run = 2,         // Di chuyển / Chạy bộ (Run)
        sta = 7,         // Đứng thủ thế chiến đấu (Combat Ready / Battle Idle)
        wlk = 10,        // Đi bộ / Tản bộ (Walk) - [Dự phòng: Dùng cho NPC đi tuần hoặc người chơi đi dạo]
        st01 = 13,       // Động tác đứng đặc biệt 1 (Special Idle 1 / Cảnh quan)
        st02 = 14,       // Động tác đứng đặc biệt 2 (Special Idle 2 / Cảnh quan)
        jsrun = 31,      // Chạy nước rút / Tăng tốc (Sprint / Fast Run) - [Dự phòng: Khi kích hoạt bùa tăng tốc / buff]

        // =========================================================================
        // 2. NHÓM TẤN CÔNG THƯỜNG (Normal Attack / Combo)
        // =========================================================================
        at = 15,         // Đòn đánh đơn cơ bản của quái vật / NPC
        at01 = 16,       // Đánh thường đòn 1 (Combo 1)
        at02 = 17,       // Đánh thường đòn 2 (Combo 2)
        at03 = 18,       // Đánh thường đòn 3 (Combo 3)
        at04 = 19,       // Đánh thường đòn 4 (Combo 4)
        // (Lưu ý: Hậu tố _g như at01_g dành cho biến thể động tác khi đang cưỡi thú cưỡi / ngựa)

        // =========================================================================
        // 3. NHÓM KỸ NĂNG MÔN PHÁI & TUYỆT KỸ (Skills)
        // =========================================================================
        jn01 = 21,       // Kỹ năng môn phái 1 (Skill Q)
        jn02 = 22,       // Kỹ năng môn phái 2 (Skill W)
        jn03 = 23,       // Kỹ năng môn phái 3 (Skill E)
        jn04 = 24,       // Kỹ năng môn phái 4 (Skill R)
        jn05 = 25,       // Tuyệt kỹ Nộ (Ultimate Skill)
        jn02b = 27,      // Nhịp phụ / Động tác biến thể của chiêu 2b
        jn02a = 28,      // Nhịp phụ / Động tác biến thể của chiêu 2a
        jn01a = 40,      // Nhịp phụ / Động tác biến thể của chiêu 1a
        jn06 = 55,       // Kỹ năng đặc biệt giang hồ / Chiêu mở rộng

        // =========================================================================
        // 4. NHÓM BỊ ĐÁNH & KHỐNG CHẾ (Hit Reactions & Crowd Control)
        // =========================================================================
        jt = 4,          // Bị đòn nặng đẩy lùi về phía sau (Knockback / Pushback) - [Dự phòng cho combat nâng cao]
        bat_pull = 5,    // Bị kéo / Hút về phía mục tiêu (Pull / Hook) - [Dự phòng cho chiêu hút quái/kéo dây]
        bat = 9,         // Bị thương nhẹ / Giật mình tại chỗ (Hit Flinch / Stagger)
        jf = 26,         // Bị đánh bay lên trời ➔ Rơi xuống đất ➔ Đứng dậy (Knockup & Get Up) - [Dự phòng hất tung]

        // =========================================================================
        // 5. NHÓM TỬ VONG (Death)
        // =========================================================================
        die = 3,         // Chết thường / Gục ngã tại chỗ (Normal Death)
        jfd = 20,        // Chết văng / Bị đánh bay đập đất và nằm im (Knockback Death) - [Dự phòng đòn kết liễu uy lực]

        // =========================================================================
        // 6. NHÓM KHINH CÔNG & NGOẠI TRANG (Qinggong & Wings) - [Dự phòng hệ thống Khinh công]
        // =========================================================================
        qg = 6,          // Khinh công lướt cơ bản
        qg01 = 29,       // Khinh công bước 1 (Bật nhảy lên)
        qg02 = 30,       // Khinh công bước 2 (Lướt trên không)
        qg03 = 32,       // Khinh công bước 3 (Đạp gió / Xoay người)
        qg04 = 33,       // Khinh công bước 4 (Tiếp tục lướt)
        qg05 = 34,       // Khinh công bước 5 (Tiếp đất an toàn)
        zc = 49,         // Giương cánh ngoại trang (Wings Open)
        hx = 50,         // Lướt cánh ngoại trang (Wings Glide)

        // =========================================================================
        // 7. NHÓM TƯƠNG TÁC & SINH HOẠT (Social, Rest & Interaction) - [Dự phòng mở rộng sau này]
        // =========================================================================
        zx = 11,         // Động tác ngồi ghế / ngồi nghỉ (Sit)
        zst = 12,        // Ngồi thiền / Đả tọa hồi phục (Meditation Standby)
        dz = 35,         // Ngồi thiền / Đả tọa tu luyện (Meditation)
        ts01 = 36,       // Động tác biểu cảm 1 / Chào hỏi (Emote Wave/Bow)
        ts02 = 37,       // Động tác biểu cảm 2 / Khiêu khích (Emote Taunt)
        ts03 = 38,       // Động tác biểu cảm 3 / Khiêu vũ (Emote Dance)
        sit = 39,        // Động tác ngồi bệt (Sit on Ground)
        open = 52,       // Động tác mở hòm / rương kho báu (Open Chest)
        up = 53,         // Leo trèo thang / vách đá (Climb Up)
        down = 54        // Cúi nhặt đồ / thu thập tài nguyên (Pickup / Gather)
    }

    /// <summary>
    /// Bộ chuyển đổi hai chiều giữa CastActionID (Số nguyên) và Tên Clip Animation (Chuỗi string)
    /// </summary>
    public static class CastActionHelper
    {
        private static readonly Dictionary<int, string> idToClipMap = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> clipToIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        static CastActionHelper()
        {
            Register(CastActionID.st, "st");
            Register(CastActionID.run, "run");
            Register(CastActionID.die, "die");
            Register(CastActionID.jt, "jt");
            Register(CastActionID.bat_pull, "bat");
            Register(CastActionID.qg, "qg");
            Register(CastActionID.sta, "sta");
            Register(CastActionID.bat, "bat");
            Register(CastActionID.wlk, "wlk");
            Register(CastActionID.zx, "zx");
            Register(CastActionID.zst, "zst");
            Register(CastActionID.st01, "st01");
            Register(CastActionID.st02, "st02");

            Register(CastActionID.at, "at");
            Register(CastActionID.at01, "at01");
            Register(CastActionID.at02, "at02");
            Register(CastActionID.at03, "at03");
            Register(CastActionID.at04, "at04");
            Register(CastActionID.jfd, "jfd");

            Register(CastActionID.jn01, "jn01");
            Register(CastActionID.jn02, "jn02");
            Register(CastActionID.jn03, "jn03");
            Register(CastActionID.jn04, "jn04");
            Register(CastActionID.jn05, "jn05");
            Register(CastActionID.jf, "jf");
            Register(CastActionID.jn02b, "jn02b");
            Register(CastActionID.jn02a, "jn02a");
            Register(CastActionID.jn01a, "jn01a");
            Register(CastActionID.jn06, "jn06");

            Register(CastActionID.qg01, "qg01");
            Register(CastActionID.qg02, "qg02");
            Register(CastActionID.jsrun, "jsrun");
            Register(CastActionID.qg03, "qg03");
            Register(CastActionID.qg04, "qg04");
            Register(CastActionID.qg05, "qg05");

            Register(CastActionID.dz, "dz");
            Register(CastActionID.ts01, "ts01");
            Register(CastActionID.ts02, "ts02");
            Register(CastActionID.ts03, "ts03");
            Register(CastActionID.sit, "sit");
            Register(CastActionID.zc, "zc");
            Register(CastActionID.hx, "hx");
            Register(CastActionID.open, "open");
            Register(CastActionID.up, "up");
            Register(CastActionID.down, "down");
        }

        private static void Register(CastActionID id, string clipName)
        {
            int intId = (int)id;
            idToClipMap[intId] = clipName;
            if (!clipToIdMap.ContainsKey(clipName))
            {
                clipToIdMap[clipName] = intId;
            }
        }

        /// <summary>
        /// Lấy tên Clip Animation từ CastActionID (int)
        /// </summary>
        public static string GetClipName(int actionId)
        {
            if (idToClipMap.TryGetValue(actionId, out string clip))
            {
                return clip;
            }
            return actionId.ToString();
        }

        /// <summary>
        /// Lấy tên Clip Animation từ CastActionID Enum
        /// </summary>
        public static string GetClipName(CastActionID actionId)
        {
            return GetClipName((int)actionId);
        }

        /// <summary>
        /// Tìm CastActionID tương ứng từ tên clip chuỗi string (hỗ trợ đọc cả ID số dạng chuỗi hoặc tên clip)
        /// </summary>
        public static int ParseActionId(string token, int defaultId = (int)CastActionID.at01)
        {
            if (string.IsNullOrEmpty(token)) return defaultId;

            // Nếu người dùng nhập số ID trực tiếp (vd: 16, 21)
            if (int.TryParse(token.Trim(), out int parsedId))
            {
                return parsedId;
            }

            // Nếu nhập tên chuỗi (vd: at01, jn01)
            if (clipToIdMap.TryGetValue(token.Trim(), out int matchedId))
            {
                return matchedId;
            }

            return defaultId;
        }
    }
}
