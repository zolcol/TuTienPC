using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Combat;

namespace TopDownGame.Data
{
    [Serializable]
    public class MissileData
    {
        public int missileId;
        public string missileName;
        public MissileMoveKind moveKind = MissileMoveKind.Linear;
        public float speed;
        public float acceSpeed;
        public HitboxShape hitboxShape = HitboxShape.SingleTarget;
        public float dmgRange;
        public float dmgRangeY;
        public float dmgInterval;
        public float lifeTime;
        public float delayDeleteFrame;
        public bool isDmgVanish;
        public bool canRepeatDmg;
        public bool isIgnoreBarrier;
        public int missileResID;
        public int collResID;
        public int vanishResID;
        public float posOffsetLength;
        public int collSoundID;
        public int flySoundID;
        public bool isFollowTarget;

        public bool IsProjectile => moveKind > MissileMoveKind.StaticTrap || missileResID > 0;
        public float SpeedInUnitsPerSec => (moveKind == MissileMoveKind.StaticTrap || speed <= 0f) ? 0f : (speed / 10.0f);
        public float AccelerationInUnitsPerSec2 => (moveKind == MissileMoveKind.StaticTrap || acceSpeed <= 0f) ? 0f : (acceSpeed / 10.0f);
        public float LifeTimeInSeconds => lifeTime > 0f ? (lifeTime / 15.0f) : 2.5f;
        public float DmgIntervalInSeconds => dmgInterval > 0f ? (dmgInterval / 15.0f) : 0f;
        public float CollisionRadius => dmgRange > 0f ? (dmgRange / 10.0f) : 0.6f;
        public float SpawnOffsetDistance => posOffsetLength > 0f ? (posOffsetLength / 100.0f) : 1.2f;

        public string FlyEffectPath => EffectDatabase.GetEffectPath(missileResID);
        public string HitEffectPath => EffectDatabase.GetEffectPath(collResID);
        public string VanishEffectPath => EffectDatabase.GetEffectPath(vanishResID);

        public bool ShouldVanishOnHit => !canRepeatDmg && isDmgVanish;
    }

    public class MissileDatabase : ICsvTable
    {
        private static MissileDatabase instance;
        public static MissileDatabase Instance => instance ?? (instance = new MissileDatabase());

        private readonly Dictionary<int, MissileData> missiles = new Dictionary<int, MissileData>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || missiles.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            missiles.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = GameDataPaths.MissileCsv;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[MissileDatabase] ⚠️ Không tìm thấy file tại: {filePath}");
                return;
            }

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] tokens = CsvParserHelper.SplitCsvLine(line);
                        if (tokens.Length < 7) continue;

                        int id = CsvParserHelper.ParseInt(tokens[0]);
                        if (id <= 0) continue;

                        string name = CsvParserHelper.GetToken(tokens, 1);
                        int rawMoveKind = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 2));
                        MissileMoveKind moveKind = Enum.IsDefined(typeof(MissileMoveKind), rawMoveKind) ? (MissileMoveKind)rawMoveKind : MissileMoveKind.Linear;
                        float speed = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 3));
                        int rawRangeType = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 4));
                        float dmgRange = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 5));
                        float dmgRangeY = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 6));

                        HitboxShape shape = HitboxShape.SingleTarget;
                        if (Enum.IsDefined(typeof(HitboxShape), rawRangeType))
                        {
                            shape = (HitboxShape)rawRangeType;
                        }

                        float dmgInterval = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 8), 0f);
                        float lifeTime = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 9), 30f);
                        float delayDeleteFrame = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 10), 0f);
                        
                        string rawVanish = CsvParserHelper.GetToken(tokens, 11).Trim();
                        string rawRepeat = CsvParserHelper.GetToken(tokens, 12).Trim();
                        bool isDmgVanish = rawVanish.Equals("1");
                        bool canRepeatDmg = rawRepeat.Equals("1");

                        int missileResID = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 13), 0);
                        int collResID = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 14), 0);
                        float posOffsetLength = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 17), 120f);
                        int collSoundID = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 20), -1);
                        int flySoundID = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 21), -1);
                        bool isFollowTarget = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 23), 0) == 1;
                        float acceSpeed = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 24), 0f);
                        bool isIgnoreBarrier = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 27), 0) == 1;
                        int vanishResID = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 29), 0);

                        MissileData data = new MissileData
                        {
                            missileId = id,
                            missileName = name,
                            moveKind = moveKind,
                            speed = speed,
                            acceSpeed = acceSpeed,
                            hitboxShape = shape,
                            dmgRange = dmgRange,
                            dmgRangeY = dmgRangeY,
                            dmgInterval = dmgInterval,
                            lifeTime = lifeTime,
                            delayDeleteFrame = delayDeleteFrame,
                            isDmgVanish = isDmgVanish,
                            canRepeatDmg = canRepeatDmg,
                            isIgnoreBarrier = isIgnoreBarrier,
                            missileResID = missileResID,
                            collResID = collResID,
                            vanishResID = vanishResID,
                            posOffsetLength = posOffsetLength,
                            collSoundID = collSoundID,
                            flySoundID = flySoundID,
                            isFollowTarget = isFollowTarget
                        };

                        missiles[id] = data;
                    }
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissileDatabase] ❌ Lỗi đọc Missile.csv: {ex.Message}");
            }
        }

        public static MissileData GetMissile(int missileId)
        {
            if (missileId <= 0) return null;
            Instance.EnsureLoaded();
            Instance.missiles.TryGetValue(missileId, out MissileData data);
            return data;
        }

        public static bool HasMissile(int missileId)
        {
            if (missileId <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.missiles.ContainsKey(missileId);
        }
    }
}
