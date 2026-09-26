using System;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Stats;
using TopDownGame.Combat;

namespace TopDownGame.Skills
{
    public enum SkillRelation
    {
        Enemy = 0,      // Tác dụng lên Kẻ địch (Gây sát thương, khống chế)
        Recover = 1,    // Hồi phục sinh lực (Bản thân và Đồng đội)
        Friend = 2,     // Hỗ trợ đồng minh (Buff công, thủ)
        Self = 3        // Tác dụng lên chính bản thân người thi triển
    }

    [Serializable]
    public class SkillEffectEvent
    {
        public int frame = 0;
        public string effectPath = "";
        public int slotId = 0;
        public float duration = 2.5f;
    }

    [Serializable]
    public class SkillData
    {
        // Tốc độ khung hình chuẩn theo chuẩn Logic Engine
        public const float ACTION_EVENT_FPS = 15f; // Chuẩn 15 FPS cho ActionEvent, Frame Timing (Animation)
        public const float LOGIC_GAME_FPS = 15f;   // Alias giữ tương thích ngược
        public const float COOLDOWN_FPS = 15f;     // Chuẩn 15 FPS cho TimePerCast / Cooldown trong Skill.csv

        // --- IDENTITY & ANIMATION ---
        public int id;
        public string name;
        public string iconPath;
        [Tooltip("Mã CastActionID theo chuẩn Database Animation")]
        public int castActionId = (int)CastActionID.at01;
        public float crossFade = 0.1f;

        // --- RELATION & EFFECT STYLES (Theo DATA_CONVENTIONS.md Mục 2) ---
        public SkillRelation relation = SkillRelation.Enemy;
        public string skillStyle = "damage";
        public bool targetSelf = false;
        public int subSkillId = 0; // Chiêu phụ liên kết kích hoạt cùng (như 306 gọi 307)
        public VfxStartPosType startPosType = VfxStartPosType.Caster; // Vị trí xuất hiện hiệu ứng (1: Caster, 2: Target, 3: HitPoint)
        public int slotId = 0; // Khớp xương gắn hiệu ứng (theo PartSlot.csv: 1: B_RH, 2: B_LH, 7: Spine1, 15: Head, 19/20: Foot)
        public SkillSelectorType selectorType = SkillSelectorType.None;
        public float selectorRange = 0f;

        /// <summary>
        /// Kiểm tra chiêu thức có phải là kỹ năng hồi máu / hồi sinh lực hay không
        /// </summary>
        public bool IsHeal => relation == SkillRelation.Recover || (!string.IsNullOrEmpty(skillStyle) && skillStyle.IndexOf("heal", StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Có chiêu thức phụ đi kèm hay không
        /// </summary>
        public bool HasSubSkill => subSkillId > 0 && subSkillId != id;

        /// <summary>
        /// Lấy tên Animation Clip thực tế tương ứng với CastActionID
        /// </summary>
        public string ClipName => CastActionHelper.GetClipName(castActionId);
        public string clipName => ClipName; // Giữ tương thích ngược với code cũ

        // --- MOVEMENT & SHAPE ---
        public float movePosDistance = 0f;
        public float movePosSpeed = 0f;
        public float movePosAccel = 0f;
        public int movePosFrame = -1;
        public SkillType skillType = SkillType.StraightRay;
        public float range = 5f;
        public float fanAngle = 0f;
        public float boxWidth = 1.5f;

        // --- MISSILE / PROJECTILE (Đạn đạo theo chuẩn DATA_CONVENTIONS.md Mục 2 & 3) ---
        public int childId = 0;             // ID tra cứu cấu hình đạn đạo từ Missile.csv
        public int childCount = 1;          // Số lượng đạn/tia sinh ra trong 1 lần xuất chiêu (ChildCount trong Skill.csv)
        public int missileForm = 0;          // Dạng đạn đạo: 1=Thẳng, 2=Quạt, 3=Tròn, 4=Nảy bật, 5=Rơi trời
        public int msGenerate = 1;          // Kiểu sinh đạn: 1=Đồng loạt, 2=Tuần tự cách quãng, 3=Xoay tròn
        public string msGenerateParam = ""; // Tham số đi kèm (góc lệch độ hoặc delay frame giữa các đợt)
        public MissileFormType missileFormType => Enum.IsDefined(typeof(MissileFormType), missileForm) ? (MissileFormType)missileForm : MissileFormType.None;
        public bool isMelee = true;          // Kỹ năng cận chiến (true) hay tầm xa bắn đạn (false)

        // Ngũ hành thuộc tính (DATA_CONVENTIONS.md Mục 2 & 14)
        public ElementalSeries series = ElementalSeries.None;

        // Các tham số logic mở rộng từ Skill.csv (DATA_CONVENTIONS.md Mục 2: Param1..Param4)
        public float skillParam1 = 0f;    // vd khi MissileForm=4: Số lần nảy tối đa
        public float skillParam2 = 0f;    // vd khi MissileForm=4: 1 = Bật tìm mục tiêu kế tiếp
        public float skillParam3 = 0f;    // vd khi MissileForm=4: Bán kính tìm mục tiêu nảy (cm)
        public float skillParam4 = 0f;    // vd khi MissileForm=4: Số lần lặp lại trên 1 người (0=không hạn chế)

        /// <summary>
        /// Kỹ năng có bắn ra viên đạn / ám khí / kiếm khí thực tế hay không
        /// </summary>
        public bool HasProjectile
        {
            get
            {
                if (skillType == SkillType.Projectile) return true;
                if (childId <= 0) return false;
                var missile = TopDownGame.Data.MissileDatabase.GetMissile(childId);
                return missile != null && missile.IsProjectile;
            }
        }

        // --- COMBAT SCALING & RESOURCE ---
        public float physScale = 1f;
        public float magicScale = 0f;
        public float manaCost = 0f;
        public float cooldown = 0f;
        public bool canCancel = true;

        // --- FRAME TIMING & COMBO (Mốc tính bằng Frame ActionEvent chuẩn 30 FPS theo DATA_CONVENTIONS.md Mục 4) ---
        public int linkskillinit = -1;
        public int param1 = -1;          // Thời gian cửa sổ combo mở từ khi candoskill đến khi kết thúc (số frame)
        public int param2 = -1;          // Skill ID của chiêu tiếp theo trong combo (-1 nếu không nối combo)
        public int candoskill = -1;      // Bắt đầu mở cửa sổ ấn phím (mốc frame, -1 nếu không ngắt)
        public int castSkill = 0;        // Bắt đầu áp damage (mốc frame, mặc định 0)
        public int canDoRun = -1;        // Có thể hủy hoạt ảnh để di chuyển (mốc frame, -1 nếu phải đánh hết clip)
        public int castLinkSkill = -1;   // Mốc bắt đầu chuyển sang animation chiêu combo tiếp theo (mốc frame)
        public int instantDir = -1;      // Mốc khóa xoay hướng (mốc frame)

        // --- AUDIO & VFX ---
        public int playsound = -1;           // Sound ID tra cứu từ Sound.csv (-1 nếu không có âm thanh)
        public int playsoundFrame = -1;      // Frame phát âm thanh từ ActionEvent.csv (-1 nếu theo mặc định)
        public string effectPath = "";
        public int stateEffectId = 0;      // Đường dẫn Prefab VFX trong Resources
        public List<SkillEffectEvent> effectEvents = new List<SkillEffectEvent>(); // Danh sách toàn bộ sự kiện hiệu ứng từ ActionEvent.csv

        // --- CHUYỂN ĐỔI SANG GIÂY (TIME = FRAME / ACTION_EVENT_FPS, Chuẩn 30 FPS theo DATA_CONVENTIONS.md) ---
        public float CastSkillTime => castSkill >= 0 ? (castSkill / ACTION_EVENT_FPS) : 0f;
        public float CanDoSkillTime => candoskill >= 0 ? (candoskill / ACTION_EVENT_FPS) : -1f;
        public float ComboEndTime => (candoskill >= 0 && param1 > 0) ? ((candoskill + param1) / ACTION_EVENT_FPS) : -1f;
        public float CanDoRunTime => canDoRun >= 0 ? (canDoRun / ACTION_EVENT_FPS) : -1f;
        public float CastLinkSkillTime => castLinkSkill >= 0 ? (castLinkSkill / ACTION_EVENT_FPS) : (candoskill >= 0 ? (candoskill / ACTION_EVENT_FPS) : -1f);
        public float InstantDirTime => instantDir >= 0 ? (instantDir / ACTION_EVENT_FPS) : -1f;
        public float PlaySoundTime => playsoundFrame >= 0 ? (playsoundFrame / ACTION_EVENT_FPS) : 0f;
        public float MovePosTime => movePosFrame >= 0 ? (movePosFrame / ACTION_EVENT_FPS) : -1f;

        // --- HELPER FLAGS KIỂM TRA HỢP LỆ ---
        public bool HasSound => playsound > 0;
        public bool HasCombo => param2 > 0;
        public int NextComboSkillId => param2 > 0 ? param2 : -1;
        public bool CanCancelBySkill => canCancel && candoskill >= 0;
        public bool CanCancelByRun => canDoRun >= 0;

        private Sprite cachedIcon;
        private bool attemptedIconLoad = false;

        /// <summary>
        /// Lấy Sprite Icon từ thư mục Resources dựa theo đường dẫn iconPath
        /// </summary>
        public Sprite GetIconSprite()
        {
            if (cachedIcon != null) return cachedIcon;
            if (attemptedIconLoad) return null;

            attemptedIconLoad = true;
            if (!string.IsNullOrEmpty(iconPath))
            {
                cachedIcon = Resources.Load<Sprite>(iconPath);
            }
            return cachedIcon;
        }

        /// <summary>
        /// Tính tổng sát thương dựa trên chỉ số Vật Lý và Phép của thực thể tung chiêu
        /// </summary>
        public float CalculateDamage(EntityStats attackerStats)
        {
            if (attackerStats == null)
            {
                return (20f * physScale) + (10f * magicScale);
            }
            return (attackerStats.PhysicalDamage * physScale) + (attackerStats.MagicDamage * magicScale);
        }

        /// <summary>
        /// Tính tổng lượng hồi máu dựa trên chỉ số Phép / Nội công của thực thể tung chiêu
        /// </summary>
        public float CalculateHeal(EntityStats casterStats)
        {
            float baseHeal = 100f * (magicScale > 0f ? magicScale : 1f);
            if (casterStats == null)
            {
                return baseHeal;
            }
            return baseHeal + (casterStats.MagicDamage * 2.2f);
        }
    }
}

