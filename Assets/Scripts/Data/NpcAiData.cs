using System;
using TopDownGame.Combat;

namespace TopDownGame.Data
{
    [Serializable]
    public class NpcAiData
    {
        public string fileName = "CommonActive";
        public bool isAggressive = true;             // Attack == 1
        public bool canStrikeBack = true;            // StrikeBack == 1
        public int wanderChance = 0;                 // RandmonMove (%)
        public float fleeHpPercent = 0f;             // FleeHpPrecent (%)
        public float fleeNearRate = 0f;              // FleeNearRate (%)
        public float breathTimeSec = 1.0f;           // AiBreathTime / 15.0f
        public AiTargetSelectType selectTarget = AiTargetSelectType.StrikeBack;
        public float lockDuration = 4.0f;            // ChangeTargetTime / 15.0f

        public static NpcAiData CreateDefaultActive()
        {
            return new NpcAiData
            {
                fileName = "CommonActive",
                isAggressive = true,
                canStrikeBack = true,
                wanderChance = 0,
                fleeHpPercent = 0f,
                fleeNearRate = 0f,
                breathTimeSec = 1.0f,
                selectTarget = AiTargetSelectType.StrikeBack,
                lockDuration = 4.0f
            };
        }

        public static NpcAiData CreateDefaultPassive()
        {
            return new NpcAiData
            {
                fileName = "CommonPassive",
                isAggressive = false,
                canStrikeBack = true,
                wanderChance = 10,
                fleeHpPercent = 0f,
                fleeNearRate = 0f,
                breathTimeSec = 2.0f,
                selectTarget = AiTargetSelectType.StrikeBack,
                lockDuration = 4.0f
            };
        }
    }
}
