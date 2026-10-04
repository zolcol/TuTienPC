namespace TopDownGame.Combat
{
    /// <summary>
    /// Phân loại thực thể trong game theo DATA_CONVENTIONS.md (Mục 7 & 14)
    /// </summary>
    public enum NpcKind
    {
        None = -1,
        Monster = 0,    // Quái vật thường / Quái tinh anh / Boss
        Normal = 0,     // Alias: Quái vật / Kẻ địch
        Player = 1,     // Người chơi / Phân thân
        DialogNpc = 2,  // NPC giao tiếp / Nhiệm vụ
        Dialoger = 2,   // Alias: NPC giao tiếp
        Partner = 3,    // Đồng hành / Pet
        Silencer = 4,   // NPC tĩnh / Câm lặng / Cơ quan
        Portal = 4,     // Cổng dịch chuyển / Cơ quan (alias)
        GatherBox = 5,  // Rương báu / Lửa trại / Khoáng sản
        Gather = 5,     // Alias: Vật phẩm thu thập
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
    /// Phân loại hình thức kỹ năng theo DATA_CONVENTIONS_V2.md (Mục 2 & 18)
    /// </summary>
    public enum SkillTypeDef
    {
        None = 0,
        Melee = 1,          // Kỹ năng áp sát cận chiến / Khinh công (skill_type_melee)
        InstSingle = 2,     // Tác dụng tức thì đơn thể (skill_type_inst_single)
        Passivity = 3,      // Kỹ năng bị động / Buff nội tại (skill_type_passivity)
        InstMissile = 4,    // Đạn tức thì / Không delay (skill_type_inst_missile)
        Missile = 5         // Đạn có quỹ đạo bay (skill_type_missile)
    }

    /// <summary>
    /// Cơ chế phân loại tấn công UI & Selector theo DATA_CONVENTIONS_V2.md (Mục 7 & 18)
    /// </summary>
    public enum SkillAttackType
    {
        Normal = 1,         // Kỹ năng đánh thường / PBAOE quanh thân
        Direction = 2,      // Kỹ năng định hướng tự do (Linear Skillshot)
        Target = 3,         // Kỹ năng khóa mục tiêu (Target-Locked)
        Line = 4            // Kỹ năng đường thẳng xuyên thấu
    }

    /// <summary>
    /// Dạng đạn đạo / Hình thái kỹ năng theo DATA_CONVENTIONS_V2.md (Mục 2 & 18)
    /// </summary>
    public enum MissileFormType
    {
        None = 0,
        StraightLinear = 1, // Đạn bắn thẳng theo đường thẳng
        SpreadFan = 2,      // Đạn bắn chùm nhiều tia hình quạt
        CircularRing = 3,   // Vòng tròn tỏa rộng quanh Caster
        ChainBouncing = 4,  // Đạn nảy bật liên hoàn giữa các mục tiêu
        SkyDrop = 5,        // Mưa tên / Thiên thạch rơi từ trên trời xuống
        StaticCircle = 6,   // Vòng tròn AOE tĩnh
        MultiMissileWave = 7// Chùm đa đạn đồng loạt / Sóng nước tỏa rộng
    }

    /// <summary>
    /// Cơ chế di chuyển của đạn theo DATA_CONVENTIONS_V2.md (Mục 4 & 18)
    /// </summary>
    public enum MissileMoveKind
    {
        StaticTrap = 0,     // Đặt bẫy / Bãi nổ cố định tại chỗ
        Linear = 1,         // Bay thẳng theo vector ban đầu
        HomingTracking = 2, // Tự bám đuổi / uốn lượn theo mục tiêu đang khóa
        DashWithCaster = 3, // Di chuyển dính liền theo thân người lướt
        BoomerangCurved = 5,// Bay uốn lượn / quay ngược trở về
        OrbitAroundCaster = 6// Xoay vòng quanh người ra chiêu
    }

    /// <summary>
    /// Kiểu sinh đạn theo nhịp theo DATA_CONVENTIONS_V2.md (Mục 3 & 18)
    /// </summary>
    public enum MSGenerateType
    {
        Instant = 0,        // Sinh tức thời 1 lần
        Trail = 1,          // Sinh theo vệt đường đi
        AreaDoT = 2,        // Duy trì bãi sát thương tại chỗ
        MeteorRain = 3,     // Mưa rơi ngẫu nhiên liên hoàn
        TimedTrap = 4,      // Bẫy hẹn giờ phát nổ
        ChargeAccumulate = 5// Tụ lực tăng dần số lượng đạn
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

    /// <summary>
    /// Trạng thái bất lợi & khống chế theo DATA_CONVENTIONS_V2.md (Mục 18 - NpcDefine.lua)
    /// </summary>
    public enum NpcSpecialState
    {
        Hurt = 0,           // Bị thương
        Zhican = 1,         // Tàn phế
        SlowAll = 2,        // Trì hoãn
        Palsy = 3,          // Tê liệt
        Stun = 4,           // Choáng
        Fixed = 5,          // Định thân
        Weak = 6,           // Suy yếu
        Burn = 7,           // Thiêu đốt
        SlowRun = 8,        // Giảm tốc chạy
        Freeze = 9,         // Đóng băng
        Confuse = 10,       // Hỗn loạn
        Knock = 11,         // Đẩy lùi
        Drag = 12,          // Kéo lại
        Silence = 13,       // Câm lặng
        Float = 14,         // Hất tung
        SelfFreeze = 15,    // Tự đóng băng (Hộ mệnh)
        Sleep = 16,         // Ngủ say
        Knock2 = 17,        // Lùi xa
        NoJump = 18,        // Cấm khinh công
        ForceAtk = 19,      // Khiêu khích
        DragFloat = 20,     // Kéo từ trên không xuống đất
        NpcHurt = 21,       // Npc bị thương
        NpcKnock = 22,      // Npc bị đẩy lùi
        NpcHide = 23,       // Tàng hình
        Shield = 24,        // Khiên hộ thể
        FixShield = 25,     // Khiên cố định
        ShieldExt = 26,     // Khiên mở rộng
        ShieldShare = 27    // Khiên chia sẻ đồng đội
    }

    /// <summary>
    /// Thuật toán ưu tiên chọn mục tiêu AI theo DATA_CONVENTIONS_V2.md (Mục 21)
    /// </summary>
    public enum AiTargetSelectType
    {
        StrikeBack = 0, // Ưu tiên đánh trả kẻ vừa đánh mình
        Nearest = 1,    // Ưu tiên mục tiêu gần nhất
        Poorest = 2,    // Ưu tiên mục tiêu ít máu nhất (% hoặc HP)
        Richest = 3,    // Ưu tiên mục tiêu nhiều máu nhất
        Random = 4,     // Chọn ngẫu nhiên trong tầm nhìn
        Player = 5      // Ưu tiên người chơi hơn pet/phân thân
    }

    /// <summary>
    /// Struct phân tích chuỗi Gia Tốc / Lướt / Khinh Công theo DATA_CONVENTIONS_V2.md (Mục 3 & 18)
    /// </summary>
    [System.Serializable]
    public struct AcceSpeedInfo
    {
        public float Acceleration;  // Gia tốc (m/s^2)
        public float InitialSpeed;   // Vận tốc ban đầu (m/s)
        public float MaxSpeed;       // Vận tốc kẹp tối đa (m/s)

        public static AcceSpeedInfo Parse(string rawString)
        {
            var info = new AcceSpeedInfo();
            if (string.IsNullOrEmpty(rawString)) return info;

            string[] parts = rawString.Split('|');
            if (parts.Length >= 3)
            {
                float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out info.Acceleration);
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out info.InitialSpeed);
                float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out info.MaxSpeed);
            }
            return info;
        }
    }
}
