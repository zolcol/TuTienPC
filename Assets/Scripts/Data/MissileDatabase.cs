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
        public MissileMoveKind moveKind = MissileMoveKind.Linear; // 0: StaticTrap, 1: Linear, 2: HomingTracking, 3: DashWithCaster
        public float speed;             // Đơn vị tốc độ trong bảng (Velocity = Speed / 10.0f m/s)
        public float acceSpeed;         // Gia tốc tăng tốc (Acceleration = AcceSpeed / 10.0f m/s^2)
        public HitboxShape hitboxShape = HitboxShape.SingleTarget;
        public float dmgRange;          // Bán kính va chạm / quét đòn (DmgRange / 10.0f m)
        public float dmgRangeY;         // Chiều rộng hộp (m) hoặc góc mở quạt (độ)
        public float dmgInterval;       // Khoảng cách nhịp giữa 2 lần gây dame liên tục (Frames)
        public float lifeTime;          // Thời gian bay tối đa (số Frame chuẩn 30 FPS, LifeTime / 30.0f s)
        public float delayDeleteFrame;  // Số frame trễ trước khi Destroy (Frames)
        public bool isDmgVanish;        // 1 = Đạn chạm trúng 1 mục tiêu là nổ và biến mất ngay lập tức
        public bool canRepeatDmg;       // Có thể xuyên qua nhiều mục tiêu hay chạm nổ (dành cho đạn xuyên / nảy)
        public int missileResID;        // ID hiệu ứng đạn bay (trỏ EffectRes.csv)
        public int collResID;           // ID hiệu ứng nổ / va chạm khi trúng đích (trỏ EffectRes.csv)
        public int vanishResID;         // ID hiệu ứng khi đạn tan biến không trúng đích
        public float posOffsetLength;   // Khoảng cách xuất phát trước mặt người bắn (PosOffsetLenght / 100.0f m)
        public int collSoundID;         // Âm thanh khi chạm trúng đích (Trỏ Sound.csv)
        public int flySoundID;          // Âm thanh khi đạn đang bay liên tục
        public bool isFollowTarget;     // Có tự động bẻ lái bám mục tiêu hay không

        /// <summary>
        /// Kỹ năng có bắn ra viên đạn / ám khí / kiếm khí thực tế hay không
        /// </summary>
        public bool IsProjectile => moveKind > MissileMoveKind.StaticTrap || missileResID > 0;

        /// <summary>
        /// Vận tốc bay thực tế (m/s) trong không gian Unity 3D (Chuẩn DATA_CONVENTIONS.md: Speed / 10.0f)
        /// </summary>
        public float SpeedInUnitsPerSec => speed > 0f ? (speed / 10.0f) : 10.0f;

        /// <summary>
        /// Gia tốc thực tế (m/s^2) trong không gian Unity 3D (Chuẩn DATA_CONVENTIONS.md: AcceSpeed / 10.0f)
        /// </summary>
        public float AccelerationInUnitsPerSec2 => acceSpeed > 0f ? (acceSpeed / 10.0f) : 0f;

        /// <summary>
        /// Thời gian bay tối đa tính bằng giây (Chuẩn game tick 15 FPS: LifeTime / 15.0f)
        /// Kiểm chứng: Missile 301 LifeTime=10 → 10/15 = 0.667s → bay 8m ✅ đủ AttackRadius 5.5m
        /// </summary>
        public float LifeTimeInSeconds => lifeTime > 0f ? (lifeTime / 15.0f) : 2.5f;

        /// <summary>
        /// Bán kính va chạm của viên đạn (mét) (Chuẩn DATA_CONVENTIONS.md: DmgRange / 10.0f)
        /// </summary>
        public float CollisionRadius => dmgRange > 0f ? (dmgRange / 10.0f) : 0.6f;

        /// <summary>
        /// Khoảng cách nhô ra phía trước người bắn để đạn không xuất phát từ bụng nhân vật (PosOffsetLenght / 100.0f)
        /// </summary>
        public float SpawnOffsetDistance => posOffsetLength > 0f ? (posOffsetLength / 100.0f) : 1.2f;

        /// <summary>
        /// Đường dẫn Prefab hiệu ứng bay
        /// </summary>
        public string FlyEffectPath => EffectDatabase.GetEffectPath(missileResID);

        /// <summary>
        /// Đường dẫn Prefab hiệu ứng nổ / trúng đòn
        /// </summary>
        public string HitEffectPath => EffectDatabase.GetEffectPath(collResID);

        /// <summary>
        /// Đường dẫn Prefab hiệu ứng khi đạn tan biến không trúng mục tiêu
        /// </summary>
        public string VanishEffectPath => EffectDatabase.GetEffectPath(vanishResID);

        /// <summary>
        /// Xác định viên đạn có tự hủy khi chạm trúng mục tiêu hay không (theo DATA_CONVENTIONS.md Mục 3)
        /// - isDmgVanish == true: Tự hủy ngay lập tức khi trúng mục tiêu
        /// - isDmgVanish == false hoặc canRepeatDmg == true: Đạn xuyên thấu / nảy, KHÔNG tự hủy khi va chạm
        /// </summary>
        public bool ShouldVanishOnHit
        {
            get
            {
                if (canRepeatDmg) return false;
                return isDmgVanish;
            }
        }
    }

    public class MissileDatabase : MonoBehaviour
    {
        private static MissileDatabase instance;
        public static MissileDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<MissileDatabase>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[MissileDatabase]");
                        instance = go.AddComponent<MissileDatabase>();
                        DontDestroyOnLoad(go);
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        private readonly Dictionary<int, MissileData> missiles = new Dictionary<int, MissileData>();
        private bool isLoaded = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureLoaded();
        }

        public void EnsureLoaded()
        {
            if (!isLoaded || missiles.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại Missile Database")]
        public void LoadDatabase()
        {
            missiles.Clear();

            string filePath = Path.Combine(Application.dataPath, "Settings", "N", "Missile.csv");
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

                isLoaded = true;
                // Debug.Log($"✅ <color=cyan>[MissileDatabase]</color> Đã nạp thành công <b>{missiles.Count}</b> cấu hình Hitbox từ Settings/N/Missile.csv!");
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
