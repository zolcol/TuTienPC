using UnityEngine;

namespace TopDownGame
{
    /// <summary>
    /// Bảng hằng số và định danh Engine tập trung (Tags, Layers, Animation Clips, Resource Paths).
    /// Loại bỏ triệt để việc hardcode chuỗi rải rác trong dự án.
    /// </summary>
    public static class GameConstants
    {
        #region TAGS
        public static class Tags
        {
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string MainCamera = "MainCamera";
            public const string Untagged = "Untagged";
        }
        #endregion

        #region LAYERS
        public static class Layers
        {
            public const string Default = "Default";
            public const string TransparentFX = "TransparentFX";
            public const string IgnoreRaycast = "Ignore Raycast";
            public const string Water = "Water";
            public const string UI = "UI";
            public const string Player = "Player";
            public const string Enemy = "Enemy";

            public const int DefaultLayerIndex = 0;
            public const int TransparentFXIndex = 1;
            public const int IgnoreRaycastIndex = 2;
            public const int WaterIndex = 4;
            public const int UILayerIndex = 5;
            public const int DefaultPlayerLayerIndex = 3;
            public const int DefaultEnemyLayerIndex = 6;

            public static int PlayerLayer => LayerMask.NameToLayer(Player);
            public static int EnemyLayer => LayerMask.NameToLayer(Enemy);
            public static int UILayer => LayerMask.NameToLayer(UI);
            public static int IgnoreRaycastLayer => LayerMask.NameToLayer(IgnoreRaycast);

            public static int PlayerMask => LayerMask.GetMask(Player);
            public static int EnemyMask => LayerMask.GetMask(Enemy);
            public static int UIMask => LayerMask.GetMask(UI);
        }
        #endregion

        #region ANIMATION CLIPS
        public static class AnimClips
        {
            // Căn bản
            public const string Stand = "st";
            public const string BattleStand = "sta";
            public const string Run = "run";
            public const string Walk = "wlk";
            public const string FastRun = "jsrun";
            public const string Die = "die";
            public const string DieAlt = "jfd";
            public const string Hurt = "bat";
            public const string HurtAlt = "jt";

            // Đòn đánh thường
            public const string At = "at";
            public const string At01 = "at01";
            public const string At02 = "at02";
            public const string At03 = "at03";
            public const string At04 = "at04";

            // Kỹ năng
            public const string Jn01 = "jn01";
            public const string Jn02 = "jn02";
            public const string Jn03 = "jn03";
            public const string Jn04 = "jn04";
            public const string Jn05 = "jn05";
            public const string Jn06 = "jn06";
            public const string Jn01a = "jn01a";
            public const string Jn02a = "jn02a";
            public const string Jn02b = "jn02b";

            // Khinh công & bổ trợ
            public const string Qg = "qg";
            public const string Qg01 = "qg01";
            public const string Jf = "jf";
            public const string Zx = "zx";
            public const string Zst = "zst";
            public const string St01 = "st01";
            public const string St02 = "st02";
        }
        #endregion

        #region RESOURCE PATHS
        public static class ResourcePaths
        {
            public const string SoundCsv = "Sound";
            public const string NpcPrefabsFolder1 = "Players/Npcs/Prefabs/";
            public const string NpcPrefabsFolder2 = "Player/Npcs/Prefabs/";
            public const string SkillIconAtlasFolder = "UI/Atlas/SkillIcon/";
            public const string SkillIconFolder = "UI/SkillIcon/";
        }
        #endregion
    }
}
