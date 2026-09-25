namespace TopDownGame.NPC
{
    /// <summary>
    /// Phân loại thực thể trong game theo bảng NpcTemplate
    /// </summary>
    public enum NpcKind
    {
        /// <summary>
        /// 0 - Quái vật / Kẻ địch (Monster / Enemy):
        /// Có thanh máu, tự tìm đánh người chơi, có thể bị tiêu diệt và rơi đồ.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 1 - Nhân vật Người chơi (Player):
        /// Nhân vật do người chơi hoặc bot chiến trường điều khiển.
        /// </summary>
        Player = 1,

        /// <summary>
        /// 2 - NPC Hội thoại / Tính năng (Dialog NPC):
        /// NPC trong thành/thôn (chưởng môn, thương nhân, giao nhiệm vụ). 
        /// Người chơi lại gần bấm vào để nói chuyện / mở Shop.
        /// </summary>
        Dialoger = 2,

        /// <summary>
        /// 3 - Đồng hành / Thú cưng (Pet / Companion):
        /// Đệ tử / Đồng hành đi theo hỗ trợ người chơi chiến đấu.
        /// </summary>
        Partner = 3,

        /// <summary>
        /// 4 - Cơ quan / Cổng dịch chuyển (Portal / Obstacle):
        /// Cổng truyền tống, bia đá, cửa ngục — không đánh được, không có thoại, dùng để chuyển map hoặc kích hoạt cơ chế.
        /// </summary>
        Silencer = 4,

        /// <summary>
        /// 5 - Vật phẩm thu thập (Chest / Resource):
        /// Rương kho báu, lửa trại, cây thuốc, mỏ khoáng — người chơi lại gần hiện nút "Thu Thập / Mở".
        /// </summary>
        Gather = 5,

        /// <summary>
        /// 6 - Cạm bẫy (Trap):
        /// Bẫy gai, bẫy lửa, bẫy độc dưới sàn trong phó bản.
        /// </summary>
        Trap = 6
    }
}
