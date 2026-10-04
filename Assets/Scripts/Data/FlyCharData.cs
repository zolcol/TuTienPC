using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Data
{
    /// <summary>
    /// Các loại chữ/số nhảy chiến đấu chuẩn Kingsoft / JX
    /// </summary>
    public enum FlyCharType
    {
        None = 0,
        HitNormal = 1,          // HIT_NORMAL: Đòn thường gây lên quái/địch (Số trắng/vàng nhạt)
        HitDeadly = 2,          // HIT_DEADLY: Bạo kích gây lên quái/địch (Số vàng kim to + hiệu ứng nảy)
        HitMiss = 3,            // HIT_MISS: Đánh trượt mục tiêu (MISS)
        HurtNormal = 4,         // HURT_NORMAL: Người chơi bị đánh trúng (Số đỏ cam)
        HurtDeadly = 5,         // HURT_DEADLY: Người chơi bị bạo kích (Số đỏ thẫm/tím)
        HurtMiss = 6,           // HURT_MISS: Người chơi né đòn thành công (DODGE / NÉ ĐÒN)
        Treatment = 7,          // TREATMENT: Hồi phục sinh lực (+HP xanh lá)
        AddExp = 8,             // ADD_EXP: Nhận kinh nghiệm (+EXP vàng cam)
        Vitality = 9,           // VITALITY: Tăng thể chất
        Strength = 10,          // STRENGTH: Tăng sức mạnh
        Dexterity = 11,         // DEXTERITY: Tăng thân pháp
        Energy = 12,            // ENERGY: Tăng linh hoạt
        HitMissIgnore = 13,     // HIT_MISS_IGNORE: Mục tiêu miễn nhiễm
        HurtMissIgnore = 14     // HURT_MISS_IGNORE: Bản thân miễn nhiễm
    }

    /// <summary>
    /// DTO chứa cấu hình đường cong chuyển động, tỉ lệ co giãn, mờ dần và màu sắc TextMeshPro
    /// </summary>
    [Serializable]
    public class FlyCharResData
    {
        public FlyCharType type;
        public string typeName = "None";

        // Hoạt ảnh đường cong nạp từ FlyChar.csv
        public AnimationCurve scaleCurve = new AnimationCurve();
        public AnimationCurve alphaCurve = new AnimationCurve();
        public AnimationCurve offsetCurve = new AnimationCurve();
        public List<Vector2> angleList = new List<Vector2>();
        public List<Vector2> offsetList = new List<Vector2>();

        // Thời gian tồn tại tối đa (tính theo keyframe cuối cùng)
        public float duration = 1.0f;

        // Cấu hình hiển thị TextMeshPro
        public float fontSize = 3.8f;
        public Color color = Color.white;
        public Color outlineColor = Color.black;
        public float outlineWidth = 0.22f;
        public bool isBold = true;
        public string prefix = "";
        public string suffix = "";

        // Chuyển đổi pixel 2D trong CSV sang không gian thế giới 3D
        public float pixelToWorldScale = 0.012f;
        public float randomJitter = 0.25f;
    }
}
