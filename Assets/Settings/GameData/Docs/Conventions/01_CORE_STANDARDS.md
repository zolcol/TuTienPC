# 📏 QUY CHUẨN ĐƠN VỊ ĐO, ANIMATION SPEED & BẢNG MÃ C# CỐT LÕI

> **Tài liệu thành phần:** Nằm trong bộ quy chuẩn dữ liệu [DATA_CONVENTIONS_V2.md](../DATA_CONVENTIONS_V2.md).
> **Phạm vi:** Đơn vị đo lường không gian, chuẩn 15 FPS, công thức Attack Speed và toàn bộ Enums / Structs C# dùng trong Unity.

---

## 1. HỆ THỐNG ĐƠN VỊ ĐO LƯỜNG & CÔNG THỨC QUY ĐỔI UNITY

Toàn bộ hệ thống logic thời gian và chuyển động trong database được thiết kế chạy trên nền **chuẩn 15 FPS**.

| Đại lượng trong CSV | Đơn vị gốc | Tỉ lệ quy đổi | Đơn vị Unity C# | Công thức tính trong Unity | Bằng chứng mã nguồn |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **Thời gian Frame (`LifeTime`, `TimePerCast`, `ReviveFrame`...)** | 1 frame (chuẩn **15 FPS**) | $\div 15$ | Giây (Seconds) | `float timeSec = frame / 15.0f;` | `AutoSkillTimeDelay=15` (1 giây kiểm tra 1 lần) |
| **Góc quay tức thời (`InstantDir`)** | 1 frame góc quay (chuẩn **45 FPS**) | $\div 45$ | Độ/giây (°/s) | `float rotSpeed = val * 45.0f;` | `ActionEventDes.tab`: "Frame này 45 frame/giây" |
| **Góc chia xòe quạt (`Param2` trong `MissileForm = 2`)** | Binary Angle ($64\text{ units} = 360^\circ$) | $\times 5.625^\circ$ | Độ (Degrees) | `float angleDeg = val * (360f / 64f);` | `Skill.tab`: "tổng góc là 64° = 360°" |
| **Khoảng cách / Tầm đánh (`AttackRadius`, `PosOffsetLenght`, `VisionRadius`, `CastRadius`, `DamageRadius`)** | Centimet (cm) | $\div 100$ | Mét (Meters) | `float rangeMeter = val / 100.0f;` | `ActionEventDes.tab`: "1 là 1 cm", `PreciseCastSkill.tab` |
| **Bán kính sát thương Đạn (`DmgRange`, `DmgRangeY`, `IgnoreDmgRange`)** | Decimet (dm) | $\div 10$ | Mét (Meters) | `float dmgRadius = dmgRange / 10.0f;` | `Missile.tab` |
| **Vận tốc bay (`Speed`)** | Game Speed Unit | $\div 10$ | Mét/giây (m/s) | `float velocity = speed / 10.0f;` | Đối chiếu đạn tầm xa 301 |
| **Gia tốc (`AcceSpeed`)** | Game Acce Unit | $\div 10$ | $m/s^2$ | `float accel = acceSpeed / 10.0f;` | `Missile.tab` |
| **Tốc độ di chuyển (`RunSpeed`, `WalkSpeed`)** | cm/frame (tại 15 FPS) | $\times 15 \div 100$ | Mét/giây (m/s) | `float moveSpeed = runSpeed * 15.0f / 100.0f;` | `AutoRunSpeed.lua`: `nTimeFrame = nPathLen / nRunSpeed` với `nPathLen` đơn vị cm và `GAME_FPS=15`. VD: `RunSpeed=27` → `27×15/100 = 4.05 m/s` |
| **Cường độ chớp sáng (`CollBrigth`, `AlphaEffect`)** | $0 \sim 1000$ | $\div 1000$ | $0.0f \sim 1.0f$ | `float flashAlpha = collBrigth / 1000.0f;` | `SkillSetting.ini`: `AlphaEffect=500` ($0.5$) |
| **Tỉ lệ phần trăm (`%`)** | $0 \sim 100$ | $\div 100$ | $0.0f \sim 1.0f$ | `float percent = val / 100.0f;` | Standard percentage |

---

---

## 6. NGUYÊN LÝ SCALE ANIMATION & CÔNG THỨC TỐC ĐÁNH (ATTACK SPEED) CHUẨN

Theo cấu hình gốc [SkillSetting.ini](file:///C:/Users/zolcol/Desktop/Data/unpacked_data/Setting/Skill/SkillSetting.ini#L83-L95), công thức rút ngắn Frame của hoạt ảnh ra đòn dựa trên Tốc Độ Đánh (`AttackSpeed` trong `NpcAttribute.csv`):

### 📐 1. Công Thức Tính Frame Động Tác Đích:
$$\text{Calculated Frame} = \text{Original Frame} \times \left(1.0 - \frac{\lfloor \text{AttackSpeed} / 10 \rfloor}{20}\right)$$

$$\text{Final Action Frame} = \text{Clamp}(\text{Calculated Frame}, \text{Min} = 9, \text{Max} = 100)$$

*(Cứ mỗi $10\%$ Tốc Đánh sẽ giảm $1/20 = 5\%$ tổng thời lượng Frame của hoạt ảnh, giới hạn thấp nhất không bao giờ dưới 9 Frames).*

### ⏱️ 2. Công Thức Scale Speed Trong Unity Animator:
$$\text{Target Duration (giây)} = \frac{\text{Final Action Frame}}{15.0f}$$

$$\text{Animator Speed Multiplier} = \frac{\text{clip.length}}{\text{Target Duration}} = \frac{\text{clip.length} \times 15.0f}{\text{Final Action Frame}}$$

---

---

## 19. TỔNG HỢP TOÀN BỘ BẢNG MÃ ENUM & STRUCT CHUẨN C# CHO UNITY

```csharp
using System;
using UnityEngine;

namespace GameData.Combat
{
    // Phân loại NPC chuẩn theo NpcKind.cs (Assembly-CSharp) & NpcDefine.lua
    public enum NpcKind
    {
        None = -1,
        Normal = 0,             // Quái vật thường / Boss
        Player = 1,             // Người chơi
        Dialoger = 2,           // NPC đàm thoại / Nhiệm vụ
        Partner = 3,            // Đồng hành / Pet
        Silencer = 4,           // NPC tĩnh / Câm lặng / Cơ quan
        SilencerNonename = 5,   // NPC câm lặng không tên
        God = 6,                // Vô địch / Thực thể đặc biệt
        Call = 7,               // Vật triệu hồi / Baby / Pet phụ
        Mirror = 8,             // Phân thân / Clone
        Puppet = 9,             // Rối / Bù nhìn
        Num = 10
    }

    // Phe phái chiến đấu (NpcDefine.lua - Npc.CampTypeDef)
    public enum NpcCamp
    {
        Player = 0,     // Phe Người chơi
        Monster = 1,    // Phe Quái vật (Thù địch)
        Neutral = 2,    // Phe Trung lập
        Song = 3,       // Phe Tống
        Jin = 4         // Phe Kim
    }

    // Phân loại hình thức kỹ năng (Define.lua - FightSkill.SkillTypeDef)
    public enum SkillTypeDef
    {
        None = 0,
        Melee = 1,          // Kỹ năng áp sát cận chiến / Khinh công (skill_type_melee)
        InstSingle = 2,     // Tác dụng tức thì đơn thể (skill_type_inst_single)
        Passivity = 3,      // Kỹ năng bị động / Buff nội tại (skill_type_passivity)
        InstMissile = 4,    // Đạn tức thì / Không delay (skill_type_inst_missile)
        Missile = 5         // Đạn có quỹ đạo bay (skill_type_missile)
    }

    // Cơ chế phân loại tấn công UI & Selector (AttackSkill.tab - AttackType)
    public enum SkillAttackType
    {
        Normal = 1,         // Kỹ năng đánh thường / PBAOE quanh thân
        Direction = 2,      // Kỹ năng định hướng tự do (Linear Skillshot)
        Target = 3,         // Kỹ năng khóa mục tiêu (Target-Locked)
        Line = 4            // Kỹ năng đường thẳng xuyên thấu
    }

    // Loại vòng ngắm / Chỉ thị mục tiêu (Skill Indicator Reticle)
    public enum SkillSelectorType
    {
        None = 0,
        SmartcastCircleAOE = 1, // Vòng tròn chọn vùng đất
        DirectionalArrow = 2,   // Mũi tên định hướng xoay theo Joystick
        TargetLock = 3          // Vòng khóa mục tiêu đơn thể (Target Lock Reticle)
    }

    // Dạng đạn đạo / Hình thái kỹ năng (Skill.tab - MissileForm)
    public enum MissileFormType
    {
        StraightLinear = 1, // Đạn bay thẳng bình thường
        SpreadFan = 2,      // Đạn bắn chùm hình quạt
        CircularRing = 3,   // Vòng tròn tỏa rộng quanh Tâm Spawn (Target hoặc Caster theo StartPosType)
        ChainBouncing = 4,  // Đạn nảy bật liên hoàn giữa các mục tiêu
        SkyDrop = 5,        // Rơi từ trên trời xuống
        StaticCircle = 6,   // Vòng tròn AOE tĩnh
        MultiMissileWave = 7// Chùm đa đạn đồng loạt / Sóng nước tỏa rộng
    }

    // Cơ chế di chuyển của đạn (Missile.tab - MoveKind)
    public enum MissileMoveKind
    {
        StaticTrap = 0,     // Đặt bẫy / Điểm hồi máu / Trận pháp cố định
        Linear = 1,         // Bay thẳng theo vector ban đầu
        HomingTracking = 2, // Tự bám đuổi / uốn lượn theo mục tiêu đang khóa
        DashWithCaster = 3, // Di chuyển dính liền theo thân người lướt
        StaticTower = 4,    // Cột bẫy / Tháp bắn tĩnh
        BoomerangCurved = 5,// Bay uốn lượn / quay ngược trở về
        OrbitAroundCaster = 6// Xoay vòng quanh người ra chiêu
    }

    // Kiểu sinh đạn theo nhịp (Skill.tab - MSGenerate)
    public enum MSGenerateType
    {
        Instant = 0,        // Sinh tức thời 1 lần
        Trail = 1,          // Sinh theo vệt đường đi
        AreaDoT = 2,        // Duy trì bãi sát thương tại chỗ
        MeteorRain = 3,     // Mưa rơi ngẫu nhiên liên hoàn
        TimedTrap = 4,      // Bẫy hẹn giờ phát nổ
        ChargeAccumulate = 5// Tụ lực tăng dần số lượng đạn
    }

    // Ngũ hành thuộc tính (NpcDefine.lua - Npc.Series)
    public enum ElementalSeries
    {
        None = 0,   // Vô hệ
        Metal = 1,  // Hệ Kim
        Wood = 2,   // Hệ Mộc
        Water = 3,  // Hệ Thủy
        Fire = 4,   // Hệ Hỏa
        Earth = 5   // Hệ Thổ
    }

    // Trạng thái bất lợi & khống chế (NpcDefine.lua - Npc.STATE)
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

    // Khớp xương gắn hiệu ứng (PartSlot.tab - BoneSlotID)
    public enum BoneSlotID
    {
        RightHand = 1,      // B_RH (Bàn tay phải / Kiếm)
        LeftHand = 2,       // B_LH (Bàn tay trái / Cung / Khiên)
        SpineUpper = 3,     // B_Spine2 (Xương sống trên / Cánh)
        BodyRoot = 4,       // Bip001 (Trọng tâm cơ thể 1)
        BodyRoot2 = 5,      // Bip01 (Trọng tâm cơ thể 2)
        Back = 6,           // back (Sau lưng / Phi phong)
        Spine = 7,          // Bip01 Spine1 (Ngực / Khiên hộ thể)
        RightHandAlt = 8,   // Bone001 (Bàn tay phải biến thể NPC)
        NpcRightHand = 11,  // S_RH (NPC tay phải)
        NpcLeftHand = 12,   // S_LH (NPC tay trái)
        NpcRightFoot = 13,  // S_RH_01 (NPC chân phải)
        NpcLeftFoot = 14,   // S_LH_01 (NPC chân trái)
        HeadTop = 15,       // S_Hat (Đỉnh đầu / Buff / Choáng)
        HeadTopDummy = 16,  // S_HAT_01 (Đỉnh đầu NPC)
        RightHandBip = 17,  // Bip01 R Hand
        LeftHandBip = 18,   // Bip01 L Hand
        RightFoot = 19,     // Bip01 R Foot
        LeftFoot = 20,      // Bip01 L Foot (Trận pháp đất)
        Head = 21,          // Bip001 Head (Đầu nhân vật)
        Pelvis = 22         // Bip01 Pelvis (Hông / Xương chậu)
    }

    // Kiểu chọn mục tiêu AI (Readme.ini / CommonActive.ini - SelectTarget)
    public enum AiTargetSelectType
    {
        Nearest,    // Gần nhất
        Poorest,    // Ít máu nhất
        Richest,    // Nhiều máu nhất
        StrikeBack, // Kẻ vừa tấn công mình
        Random,     // Ngẫu nhiên
        Player      // Ưu tiên người chơi
    }

    // Cấu hình AI cơ bản cho quái thường
    [Serializable]
    public struct NpcAiConfig
    {
        public bool Attack;             // Chủ động tấn công (1=Có, 0=Không)
        public bool StrikeBack;         // Phản đòn khi bị đánh (1=Có, 0=Không)
        public int RandomMovePercent;   // Tỉ lệ % đi lang thang (RandmonMove)
        public float BreathTimeSec;     // Nhịp suy nghĩ AI (AiBreathTime / 15.0f giây)
        public float ChangeTargetSec;   // Thời gian giữ mục tiêu (ChangeTargetTime / 15.0f giây)
        public AiTargetSelectType TargetType; // Quy tắc chọn mục tiêu
    }

    // Struct phân tích chuỗi Gia Tốc / Lướt / Khinh Công (AcceSpeedInfo)
    [Serializable]
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
                float.TryParse(parts[0], out info.Acceleration);
                float.TryParse(parts[1], out info.InitialSpeed);
                float.TryParse(parts[2], out info.MaxSpeed);
            }
            return info;
        }
    }
}
```

---
