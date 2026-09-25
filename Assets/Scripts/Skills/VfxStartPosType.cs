namespace TopDownGame.Skills
{
    /// <summary>
    /// Vị trí xuất phát của kỹ năng / hiệu ứng theo DATA_CONVENTIONS.md (Mục 7) và StartPosType trong Skill.csv
    /// </summary>
    public enum VfxStartPosType
    {
        Caster = 1,     // 1: Xuất hiện tại vị trí người tung chiêu (Caster - dưới chân / quanh thân)
        Target = 2,     // 2: Xuất hiện tại vị trí mục tiêu (Target - trên đầu / thân thể địch)
        HitPoint = 3    // 3: Xuất hiện tại điểm va chạm thực tế (Hit Point)
    }
}
