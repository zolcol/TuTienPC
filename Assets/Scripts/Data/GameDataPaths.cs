using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    /// <summary>
    /// Centralized path resolver for all Game Data files (CSV, INI, etc.).
    /// Single Source of Truth for data directories with fallback mechanism.
    /// </summary>
    public static class GameDataPaths
    {
        public static readonly string RootDir = Path.Combine(Application.dataPath, "Settings", "GameData");
        public static readonly string LegacyRootDir = Path.Combine(Application.dataPath, "Settings", "N");

        // Subdirectories
        public static readonly string AiDir = Path.Combine(RootDir, "AI");
        public static readonly string CombatDir = Path.Combine(RootDir, "Combat");
        public static readonly string NpcDir = Path.Combine(RootDir, "NPC");
        public static readonly string ProgressionDir = Path.Combine(RootDir, "Progression");
        public static readonly string VfxSlotsDir = Path.Combine(RootDir, "VFX_Slots");
        public static readonly string FeedbackDir = Path.Combine(RootDir, "Feedback");
        public static readonly string DocsDir = Path.Combine(RootDir, "Docs");

        // Specific File Paths with fallback check
        public static string GetFilePath(string subDir, string fileName)
        {
            string primaryPath = Path.Combine(RootDir, subDir, fileName);
            if (File.Exists(primaryPath)) return primaryPath;

            // Fallback 1: Root GameData
            string rootPath = Path.Combine(RootDir, fileName);
            if (File.Exists(rootPath)) return rootPath;

            // Fallback 2: Legacy Settings/N subfolder or root
            string legacySubPath = Path.Combine(LegacyRootDir, subDir, fileName);
            if (File.Exists(legacySubPath)) return legacySubPath;

            string legacyPath = Path.Combine(LegacyRootDir, fileName);
            if (File.Exists(legacyPath)) return legacyPath;

            return primaryPath; // Return standard path if not found
        }

        // Quick Accessors
        public static string SkillCsv => GetFilePath("Combat", "Skill.csv");
        public static string ActionEventCsv => GetFilePath("Combat", "ActionEvent.csv");
        public static string ActionNameCsv => GetFilePath("Combat", "ActionName.csv");
        public static string MissileCsv => GetFilePath("Combat", "Missile.csv");

        public static string NpcTemplateCsv => GetFilePath("NPC", "NpcTemplate.csv");
        public static string CharacterCsv => GetFilePath("NPC", "Character.csv");
        public static string NpcStatsCsv => GetFilePath("NPC", "NpcStats.csv");
        public static string NpcAttributeCsv => GetFilePath("NPC", "NpcAttribute.csv");
        public static string NpcResCsv => GetFilePath("NPC", "NpcRes.csv");
        public static string FieldHeaderBossCsv => GetFilePath("NPC", "Field_HeaderBoss.csv");

        public static string AutoAiSkillCsv => GetFilePath("AI", "AutoAiSkill.csv");
        public static string GetAiIniDirectory()
        {
            if (Directory.Exists(AiDir)) return AiDir;
            string legacyAi = Path.Combine(LegacyRootDir, "AI");
            if (Directory.Exists(legacyAi)) return legacyAi;
            return AiDir;
        }

        public static string PlayerLevelCsv => GetFilePath("Progression", "PlayerLevel.csv");
        public static string ExpRuleCsv => GetFilePath("Progression", "ExpRule.csv");

        public static string EffectResCsv => GetFilePath("VFX_Slots", "EffectRes.csv");
        public static string StateEffectCsv => GetFilePath("VFX_Slots", "StateEffect.csv");
        public static string PartSlotCsv => GetFilePath("VFX_Slots", "PartSlot.csv");

        public static string SoundCsv => GetFilePath("Feedback", "Sound.csv");
        public static string FlyCharCsv => GetFilePath("Combat", "FlyChar.csv");
    }
}
