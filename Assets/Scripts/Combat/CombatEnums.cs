namespace TopDownGame.Combat
{
    /// <summary>
    /// Phân loại thực thể trong game theo DATA_CONVENTIONS.md (Mục 7 & 14)
    /// </summary>
    public enum NpcKind
    {
        Monster = 0,    // Quái vật thường / Quái tinh anh
        Player = 1,     // Người chơi / Phân thân
        DialogNpc = 2,  // NPC giao tiếp / Nhiệm vụ
        Partner = 3,    // Đồng hành / Pet
        Portal = 4,     // Cổng dịch chuyển / Cơ quan
        GatherBox = 5,  // Rương báu / Lửa trại / Khoáng sản
        Trap = 6        // Cạm bẫy
    }

    /// <summary>
    /// Phe phái chiến đấu theo DATA_CONVENTIONS.md (Mục 7 & 14)
    /// </summary>
    public enum NpcCamp
    {
        Player = 0,     // Phe Người chơi
        Monster = 1,    // Phe Quái vật (Thù địch)
        Neutral = 2,    // Phe Trung lập
        Song = 3,       // Phe Tống
        Jin = 4         // Phe Kim
    }

    /// <summary>
    /// Dạng đạn đạo / Hình thái kỹ năng theo DATA_CONVENTIONS.md (Mục 2 & 14)
    /// </summary>
    public enum MissileFormType
    {
        None = 0,
        StraightLinear = 1, // Đạn bắn thẳng theo đường thẳng
        SpreadFan = 2,      // Đạn bắn chùm nhiều tia hình quạt
        CircularRing = 3,   // Vòng tròn tỏa rộng quanh Caster
        ChainBouncing = 4,  // Đạn nảy bật liên hoàn giữa các mục tiêu
        SkyDrop = 5         // Mưa tên / Thiên thạch rơi từ trên trời xuống
    }

    /// <summary>
    /// Cơ chế di chuyển của đạn theo DATA_CONVENTIONS.md (Mục 3 & 14)
    /// </summary>
    public enum MissileMoveKind
    {
        StaticTrap = 0,     // Đặt bẫy / Bãi nổ cố định tại chỗ
        Linear = 1,         // Bay thẳng theo vector ban đầu
        HomingTracking = 2, // Tự bám đuổi / uốn lượn theo mục tiêu đang khóa
        DashWithCaster = 3  // Di chuyển dính liền theo thân người lướt
    }

    /// <summary>
    /// Hình dạng Hitbox quét va chạm theo DATA_CONVENTIONS.md (Mục 3 & 14)
    /// </summary>
    public enum HitboxShape
    {
        SingleTarget = 0,   // Đơn mục tiêu (Raycast)
        CircleSphere = 1,   // Hình tròn / Khối cầu (SphereCast / OverlapSphere)
        SectorFan = 2,      // Hình cánh quạt (Bán kính + Góc mở)
        LineBox = 3,        // Hình chữ nhật đâm tới (BoxCast)
        RingTorus = 4,      // Vòng xuyến / Trụ rỗng

        // Aliases tương thích code cũ
        Circle = 1,
        Fan = 2,
        Ring = 4
    }

    /// <summary>
    /// Vị trí xuất phát của Kỹ năng theo DATA_CONVENTIONS.md (Mục 2 & 14)
    /// </summary>
    public enum SkillStartPosType
    {
        CasterOrigin = 1,   // Xuất phát từ người ra chiêu (Caster)
        TargetPosition = 2, // Xuất phát tại / hướng tới mục tiêu (Target)
        HitPoint = 3        // Xuất phát tại điểm va chạm (HitPoint)
    }

    /// <summary>
    /// Ngũ hành thuộc tính theo DATA_CONVENTIONS.md (Mục 2, 9 & 14)
    /// </summary>
    public enum ElementalSeries
    {
        None = 0,   // Vô hệ
        Metal = 1,  // Hệ Kim (Thiếu Lâm, Thiên Vương)
        Wood = 2,   // Hệ Mộc (Đường Môn, Ngũ Độc)
        Water = 3,  // Hệ Thủy (Nga Mi, Thúy Yên)
        Fire = 4,   // Hệ Hỏa (Thiên Nhẫn, Đào Hoa)
        Earth = 5   // Hệ Thổ (Võ Đang, Côn Lôn)
    }

    /// <summary>
    /// Khớp xương gắn hiệu ứng (Bone Slot) theo DATA_CONVENTIONS.md (Mục 11 & 14)
    /// </summary>
    public enum BoneSlotID
    {
        RightHand = 1,      // B_RH (Bàn tay phải / Kiếm)
        LeftHand = 2,       // B_LH (Bàn tay trái / Cung / Khiên)
        SpineUpper = 3,     // B_Spine2 (Xương sống trên)
        Back = 6,           // back (Phi phong / Cánh sau lưng)
        ChestCenter = 7,    // Bip01 Spine1 (Ngực / Khiên hộ thể)
        Head = 15,          // S_Hat / Bip001 Head (Đỉnh đầu / Buff / Stun)
        RightFoot = 19,     // Bip01 R Foot (Chân phải)
        LeftFoot = 20       // Bip01 L Foot (Chân trái / Trận pháp đất)
    }

    /// <summary>
    /// Loại vòng ngắm / Chỉ thị mục tiêu (Skill Indicator)
    /// </summary>
    public enum SkillSelectorType
    {
        None = 0,
        SmartcastCircleAOE = 1, // Vòng tròn chọn vùng đất
        DirectionalArrow = 2    // Mũi tên định hướng xoay theo Joystick
    }

    /// <summary>
    /// Chế độ di chuyển của Indicator
    /// </summary>
    public enum SelectorMoveType
    {
        Rotate = 0, // Cố định gốc, chỉ xoay theo hướng Joystick
        Move = 1    // Kéo tâm di chuyển tự do trên mặt đất
    }

    /// <summary>
    /// ResID Prefab VFX Indicator chuẩn trong EffectRes.csv
    /// </summary>
    public static class IndicatorVfxResID
    {
        public const int DirectionArrow = 7;     // Mũi tên định hướng
        public const int TargetPoint = 8;        // Tâm điểm chỉ định
        public const int SelectedEnemyAOE = 9;   // Vòng đỏ/vàng chọn địch
        public const int TargetArrowIcon = 10;   // Icon mũi tên trên đầu
        public const int SelectedAllyAOE = 11;   // Vòng xanh lá hỗ trợ đồng đội
        public const int DangerWarning = 14;     // Vùng cảnh báo nguy hiểm Boss
    }

    /// <summary>
    /// Hằng số định danh Layer và Tag chuẩn cho toàn bộ hệ thống Combat
    /// </summary>
    public static class CombatLayersAndTags
    {
        public const string TagPlayer = "Player";
        public const string TagEnemy = "Enemy";
        public const string LayerPlayer = "Player";
        public const string LayerEnemy = "Enemy";
    }
}
