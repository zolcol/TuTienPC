using System;
using UnityEngine;
using TopDownGame.NPC;
using TopDownGame.Skills;
using TopDownGame.Audio;

namespace TopDownGame.Data
{
    public static class GameDatabase
    {
        public static EffectDatabase Effects => EffectDatabase.Instance;
        public static FactionSkillDatabase FactionSkills => FactionSkillDatabase.Instance;
        public static MissileDatabase Missiles => MissileDatabase.Instance;
        public static NpcAttributeDatabase NpcAttributes => NpcAttributeDatabase.Instance;
        public static NpcResDatabase NpcRes => NpcResDatabase.Instance;
        public static PartSlotDatabase PartSlots => PartSlotDatabase.Instance;
        public static StateEffectDatabase StateEffects => StateEffectDatabase.Instance;
        public static NpcTemplateDatabase NpcTemplates => NpcTemplateDatabase.Instance;
        public static SkillDatabase Skills => SkillDatabase.Instance;
        public static SoundDatabase Sounds => SoundDatabase.Instance;
        public static FlyCharDatabase FlyChars => FlyCharDatabase.Instance;
        public static PlayerLevelDatabase PlayerLevels => PlayerLevelDatabase.Instance;
        public static ExpRuleDatabase ExpRules => ExpRuleDatabase.Instance;
        public static NpcAiDatabase NpcAi => NpcAiDatabase.Instance;

        private static bool isInitialized = false;

        public static bool IsAllLoaded => isInitialized &&
            Effects.IsLoaded &&
            FactionSkills.IsLoaded &&
            Missiles.IsLoaded &&
            NpcAttributes.IsLoaded &&
            NpcRes.IsLoaded &&
            PartSlots.IsLoaded &&
            StateEffects.IsLoaded &&
            NpcTemplates.IsLoaded &&
            Skills.IsLoaded &&
            Sounds.IsLoaded &&
            FlyChars.IsLoaded &&
            PlayerLevels.IsLoaded &&
            ExpRules.IsLoaded &&
            NpcAi.IsLoaded;

        public static void EnsureLoaded()
        {
            if (isInitialized) return;

            // Nạp theo đúng thứ tự phụ thuộc (Dependency Order)
            NpcAi.EnsureLoaded();
            Sounds.EnsureLoaded();
            FlyChars.EnsureLoaded();
            PlayerLevels.EnsureLoaded();
            ExpRules.EnsureLoaded();
            Effects.EnsureLoaded();
            Missiles.EnsureLoaded();
            StateEffects.EnsureLoaded();
            PartSlots.EnsureLoaded();
            FactionSkills.EnsureLoaded();
            NpcRes.EnsureLoaded();
            NpcAttributes.EnsureLoaded();
            NpcTemplates.EnsureLoaded();
            Skills.EnsureLoaded();

            isInitialized = true;
        }

        public static void ReloadAll()
        {
            isInitialized = false;

            NpcAi.Clear();
            Sounds.Clear();
            FlyChars.Clear();
            PlayerLevels.Clear();
            ExpRules.Clear();
            Effects.Clear();
            Missiles.Clear();
            StateEffects.Clear();
            PartSlots.Clear();
            FactionSkills.Clear();
            NpcRes.Clear();
            NpcAttributes.Clear();
            NpcTemplates.Clear();
            Skills.Clear();

            EnsureLoaded();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnEnterPlayMode()
        {
            ReloadAll();
        }
    }
}
