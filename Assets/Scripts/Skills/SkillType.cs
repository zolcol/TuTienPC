namespace TopDownGame.Skills
{
    public enum SkillType
    {
        StraightRay = 0,    // Tia thẳng phía trước (Raycast / Beam)
        Sector = 1,         // Hình quạt trước mặt (Quét góc fanAngle)
        Circle = 2,         // Vòng tròn xung quanh bản thân (AOE nổ quanh thân)
        TargetLock = 3,     // Khóa và đánh trực tiếp vào mục tiêu chỉ định
        Projectile = 4      // Bắn ra đạn đạo / kiếm khí / phi tiêu bay (Missile / Projectile)
    }
}
