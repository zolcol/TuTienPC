using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;
using TopDownGame.Combat;

namespace TopDownGame.Skills
{
    public class SkillDatabase : MonoBehaviour
    {
        private static SkillDatabase instance;
        public static SkillDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SkillDatabase>();
                    if (instance == null)
                    {
                        GameObject dbObj = new GameObject("[SkillDatabase]");
                        instance = dbObj.AddComponent<SkillDatabase>();
                        DontDestroyOnLoad(dbObj);
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        [Tooltip("File CSV dự phòng (nếu không đọc trực tiếp từ Settings/N)")]
        [SerializeField] private TextAsset fallbackCsvFile;

        private readonly Dictionary<int, SkillData> skills = new Dictionary<int, SkillData>();
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
            if (!isLoaded || skills.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại Database từ CSV")]
        public void LoadDatabase()
        {
            skills.Clear();

            // Đảm bảo các Database phụ trợ đã nạp trước
            EffectDatabase.Instance.EnsureLoaded();
            FactionSkillDatabase.Instance.EnsureLoaded();

            string nSkillPath = Path.Combine(Application.dataPath, "Settings", "N", "Skill.csv");
            string nActionEventPath = Path.Combine(Application.dataPath, "Settings", "N", "ActionEvent.csv");

            // 1. ƯU TIÊN NẠP BỘ DATA CHUẨN MỚI TỪ Settings/N
            if (File.Exists(nSkillPath) && File.Exists(nActionEventPath))
            {
                LoadFromStandardNDatabase(nSkillPath, nActionEventPath);
                if (skills.Count > 0)
                {
                    isLoaded = true;
                    Debug.Log($"✅ <color=green>[SkillDatabase]</color> Đã nạp thành công <b>{skills.Count}</b> kỹ năng từ chuẩn Settings/N (Skill.csv + ActionEvent.csv)!");
                    return;
                }
            }

            // 2. FALLBACK VỀ FILE SKILLS.CSV CŨ NẾU CẦN
            LoadFromLegacySkillsCsv();
        }

        private struct ActionEventSummary
        {
            public int instantDir;
            public float crossFade;
            public int candoskill;
            public int castSkill;
            public int canDoRun;
            public int castLinkSkill;
            public int param1;
            public int param2;
            public int playsound;
            public int playsoundFrame;
            public string effectPath;
            public int slotId;
            public float movePosDistance;
            public float movePosSpeed;
            public float movePosAccel;
            public int movePosFrame;
        }

        /// <summary>
        /// Nạp và liên kết đa tầng giữa Skill.csv và ActionEvent.csv
        /// </summary>
        private void LoadFromStandardNDatabase(string skillPath, string actionEventPath)
        {
            // Bước A: Quét toàn bộ ActionEvent.csv và gom nhóm theo ActEventID
            var eventMap = ParseActionEventFile(actionEventPath);

            // Bước B: Đọc Skill.csv và ghép dữ liệu mốc thời gian từ ActionEvent
            try
            {
                using (var fs = new FileStream(skillPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return;

                    string[] headers = CsvParserHelper.SplitCsvLine(headerLine);
                    var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < headers.Length; i++)
                    {
                        string norm = headers[i].Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
                        colMap[norm] = i;
                    }

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] tokens = CsvParserHelper.SplitCsvLine(line);
                        if (tokens.Length < 10) continue;

                        int skillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "skillid", 0));
                        if (skillId <= 0) continue;

                        string skillName = GetColRaw(tokens, colMap, "skillname", 1);
                        int rawSkillType = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "skilltype", 3));
                        int castActionId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "castactionid", 19), 16);
                        int actionEventId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "actioneventid", 20), 0);

                        // AttackRadius trong game gốc là cm (ví dụ 500, 550) -> Đổi sang mét
                        float rawRadius = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "attackradius", 38), 500f);
                        float rangeInMeters = rawRadius > 0f ? (rawRadius / 100f) : 5f;

                        // Cooldown & Mana Cost
                        float timePerCast = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "timepercast", 16), 0f);
                        float waitTime = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "waittime", 9), 0f);
                        float cooldown = timePerCast > 0f ? (timePerCast / SkillData.COOLDOWN_FPS) : (waitTime > 0f ? (waitTime / SkillData.COOLDOWN_FPS) : 0f);
                        float manaCost = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "costvalue", 55), 0f);

                        // Ngũ hành thuộc tính & Các tham số mở rộng (DATA_CONVENTIONS.md Mục 2)
                        int rawSeries = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "series", 18), 0);
                        ElementalSeries series = Enum.IsDefined(typeof(ElementalSeries), rawSeries) ? (ElementalSeries)rawSeries : ElementalSeries.None;
                        float skillParam1 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param1", 42), 0f);
                        float skillParam2 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param2", 44), 0f);
                        float skillParam3 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param3", 46), 0f);
                        float skillParam4 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param4", 48), 0f);

                        // Icon từ FactionSkill hoặc cột Icon
                        string iconName = GetColRaw(tokens, colMap, "icon", 7);
                        var fSkill = FactionSkillDatabase.GetFactionSkill(skillId);
                        if (fSkill != null && !string.IsNullOrEmpty(fSkill.btnIcon))
                        {
                            iconName = fSkill.btnIcon;
                        }

                        // Tra cứu cấu hình Hitbox chuẩn từ Missile.csv thông qua ChildID
                        int childId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "childid", 10), skillId);
                        var missile = MissileDatabase.GetMissile(childId);

                        // Quan hệ mục tiêu & Kiểu chiêu (Relation & SkillStyle theo DATA_CONVENTIONS.md)
                        string rawRelation = GetColRaw(tokens, colMap, "relation", 15).Trim();
                        SkillRelation relation = SkillRelation.Enemy;
                        if (rawRelation.Equals("recover", StringComparison.OrdinalIgnoreCase)) relation = SkillRelation.Recover;
                        else if (rawRelation.Equals("friend", StringComparison.OrdinalIgnoreCase)) relation = SkillRelation.Friend;
                        else if (rawRelation.Equals("self", StringComparison.OrdinalIgnoreCase)) relation = SkillRelation.Self;

                        string skillStyle = GetColRaw(tokens, colMap, "skillstyle", 24).Trim();
                        bool targetSelf = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "targetself", 65), 0) == 1;

                        // Tra cứu Chiêu thức phụ / Hiệu quả kèm theo (SubSkill / FlySkill / HitSkill)
                        int flySkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "flyskillid", 30), 0);
                        int startSkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "startskillid", 29), 0);
                        int flyEventInterval = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "flyeventinterval", 31), 0);
                        int hitSkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "hitskillid", 34), 0);
                        int subSkillId = flySkillId > 0 ? flySkillId : (startSkillId > 0 ? startSkillId : hitSkillId);

                        int rawStartPosType = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "startpostype", 5), 1);
                        VfxStartPosType startPosType = Enum.IsDefined(typeof(VfxStartPosType), rawStartPosType) ? (VfxStartPosType)rawStartPosType : VfxStartPosType.Caster;
                        int slotId = 0;

                        // Mặc định kiểu vùng quét đòn theo Missile hoặc Fallback
                        SkillType skillType = SkillType.StraightRay;
                        int missileForm = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "missileform", 12), 0);
                        int childCount = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "childcount", 11), 1);
                        if (childCount <= 0) childCount = 1;
                        int msGenerate = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "msgenerate", 13), 1);
                        string msGenerateParam = GetColRaw(tokens, colMap, "msgenerateparam", 14).Trim();
                        float fanAngle = 0f;
                        float boxWidth = 1.6f;

                        // Xử lý SelectorType & SelectorRange chuẩn hóa từ Database theo DATA_CONVENTIONS.md
                        int rawSelectorType = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "selectortype", 63), 0);
                        float rawSelectorRange = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "selectorrange", 62), 0f);
                        float selectorRange = rawSelectorRange > 0f ? (rawSelectorRange / 100f) : rangeInMeters;

                        SkillSelectorType selectorType = SkillSelectorType.None;
                        if (targetSelf || relation == SkillRelation.Self)
                        {
                            // Chiêu tự thân / Buff / Hồi phục -> Quick Cast (Không hiện selector)
                            selectorType = SkillSelectorType.None;
                        }
                        else if (rawSelectorType == 2 || (missile != null && missile.moveKind == MissileMoveKind.Linear) || missileForm == 1 || missileForm == 2 || missileForm == 7)
                        {
                            // Chiêu bắn đạn định hướng / lướt tới -> Mũi tên định hướng (Directional Arrow)
                            selectorType = SkillSelectorType.DirectionalArrow;
                        }
                        else if (rawSelectorType == 1)
                        {
                            // Chiêu chọn vùng đất -> Vòng tròn chọn vùng (Smartcast Circle AOE)
                            selectorType = SkillSelectorType.SmartcastCircleAOE;
                        }
                        else
                        {
                            selectorType = Enum.IsDefined(typeof(SkillSelectorType), rawSelectorType) ? (SkillSelectorType)rawSelectorType : SkillSelectorType.None;
                        }

                        if (missile != null)
                        {
                            if (missile.IsProjectile)
                            {
                                skillType = SkillType.Projectile;
                            }
                            else
                            {
                                switch (missile.hitboxShape)
                                {
                                    case HitboxShape.Circle:
                                        skillType = SkillType.Circle;
                                        break;
                                    case HitboxShape.Fan:
                                        skillType = SkillType.Sector;
                                        fanAngle = missile.dmgRangeY > 0f ? missile.dmgRangeY : 90f;
                                        break;
                                    case HitboxShape.LineBox:
                                        skillType = SkillType.StraightRay;
                                        boxWidth = missile.dmgRangeY > 0f ? (missile.dmgRangeY / 100f) : 1.8f;
                                        break;
                                    case HitboxShape.SingleTarget:
                                        skillType = SkillType.TargetLock;
                                        boxWidth = 1.6f;
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (rawSkillType == 2) skillType = SkillType.Circle;
                            else if (rawSkillType == 3) { skillType = SkillType.Sector; fanAngle = 90f; }
                            else skillType = SkillType.StraightRay;
                        }

                        // Bước C: Trích xuất các mốc Frame Timing từ ActionEvent
                        int rawCastSound = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "castsoundid", 68), -1);
                        int rawCastEffect = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "casteffectrestid", 73), 0);
                        if (rawCastEffect <= 0) rawCastEffect = skillId;

                        ActionEventSummary evSummary = new ActionEventSummary
                        {
                            crossFade = 0.1f,
                            candoskill = -1,
                            castSkill = 2,
                            canDoRun = -1,
                            castLinkSkill = -1,
                            param1 = -1,
                            param2 = -1,
                            playsound = rawCastSound,
                            playsoundFrame = rawCastSound > 0 ? 0 : -1,
                            effectPath = EffectDatabase.GetEffectPath(rawCastEffect),
                            slotId = 0,
                            movePosDistance = 0f,
                            movePosSpeed = 0f,
                            movePosAccel = 0f,
                            movePosFrame = -1,
                            instantDir = -1
                        };

                        if (actionEventId > 0 && eventMap.TryGetValue(actionEventId, out ActionEventSummary matchedSummary))
                        {
                            if (matchedSummary.crossFade > 0f) evSummary.crossFade = matchedSummary.crossFade;
                            if (matchedSummary.candoskill >= 0) evSummary.candoskill = matchedSummary.candoskill;
                            if (matchedSummary.castSkill >= 0) evSummary.castSkill = matchedSummary.castSkill;
                            if (matchedSummary.canDoRun >= 0) evSummary.canDoRun = matchedSummary.canDoRun;
                            if (matchedSummary.castLinkSkill >= 0) evSummary.castLinkSkill = matchedSummary.castLinkSkill;
                            if (matchedSummary.param1 >= 0) evSummary.param1 = matchedSummary.param1;
                            if (matchedSummary.param2 >= 0) evSummary.param2 = matchedSummary.param2;
                            if (matchedSummary.instantDir >= 0) evSummary.instantDir = matchedSummary.instantDir;
                            if (matchedSummary.slotId > 0) evSummary.slotId = matchedSummary.slotId;
                            if (matchedSummary.movePosFrame >= 0)
                            {
                                evSummary.movePosFrame = matchedSummary.movePosFrame;
                                evSummary.movePosDistance = matchedSummary.movePosDistance;
                                evSummary.movePosSpeed = matchedSummary.movePosSpeed;
                                evSummary.movePosAccel = matchedSummary.movePosAccel;
                            }

                            // Ưu tiên âm thanh trong ActionEvent nếu có, nếu không thì giữ nguyên âm thanh từ Skill.csv
                            if (matchedSummary.playsound > 0)
                            {
                                evSummary.playsound = matchedSummary.playsound;
                                evSummary.playsoundFrame = matchedSummary.playsoundFrame >= 0 ? matchedSummary.playsoundFrame : 0;
                            }
                            else if (evSummary.playsound > 0 && evSummary.playsoundFrame < 0)
                            {
                                evSummary.playsoundFrame = 0;
                            }

                            // Ưu tiên hiệu ứng trong ActionEvent nếu có, nếu không thì giữ nguyên từ Skill.csv
                            if (!string.IsNullOrEmpty(matchedSummary.effectPath))
                            {
                                evSummary.effectPath = matchedSummary.effectPath;
                            }
                        }

                        // Xác định Slot gắn hiệu ứng chuẩn hóa theo DATA_CONVENTIONS.md (Mục 11)
                        if (evSummary.slotId > 0)
                        {
                            slotId = evSummary.slotId;
                        }
                        else if (relation == SkillRelation.Recover)
                        {
                            slotId = (int)BoneSlotID.RightFoot; // 19: Bàn chân / Mặt đất
                        }
                        else if (missile != null && missile.IsProjectile)
                        {
                            slotId = (int)BoneSlotID.RightHand; // 1: Tay phải phóng đạn
                        }

                        // Nếu effectPath vẫn trống, tra cứu fallback theo missileResID từ Missile.csv
                        if (string.IsNullOrEmpty(evSummary.effectPath) && missile != null && missile.missileResID > 0)
                        {
                            evSummary.effectPath = EffectDatabase.GetEffectPath(missile.missileResID);
                        }

                        // Nếu âm thanh vẫn trống, gán âm thanh mặc định từ Skill.csv
                        if (evSummary.playsound <= 0 && rawCastSound > 0)
                        {
                            evSummary.playsound = rawCastSound;
                            evSummary.playsoundFrame = 0;
                        }

                        SkillData data = new SkillData
                        {
                            id = skillId,
                            name = skillName,
                            iconPath = iconName,
                            castActionId = castActionId,
                            crossFade = evSummary.crossFade,
                            relation = relation,
                            skillStyle = skillStyle,
                            targetSelf = targetSelf,
                            subSkillId = subSkillId,
                            startPosType = startPosType,
                            selectorType = selectorType,
                            selectorRange = selectorRange,
                            slotId = slotId,
                            childId = childId,
                            childCount = childCount,
                            missileForm = missileForm,
                            msGenerate = msGenerate,
                            msGenerateParam = msGenerateParam,
                            isMelee = missile == null || !missile.IsProjectile,
                            movePosDistance = evSummary.movePosDistance,
                            movePosSpeed = evSummary.movePosSpeed,
                            movePosAccel = evSummary.movePosAccel,
                            movePosFrame = evSummary.movePosFrame,
                            skillType = skillType,
                            range = rangeInMeters,
                            fanAngle = fanAngle,
                            boxWidth = boxWidth,
                            physScale = 1.0f,
                            magicScale = 1.0f,
                            manaCost = manaCost,
                            cooldown = cooldown,
                            canCancel = true,
                            param1 = evSummary.param1,
                            param2 = evSummary.param2,
                            candoskill = evSummary.candoskill,
                            castSkill = evSummary.castSkill,
                            canDoRun = evSummary.canDoRun,
                            castLinkSkill = evSummary.castLinkSkill,
                            instantDir = evSummary.instantDir,
                            playsound = evSummary.playsound,
                            playsoundFrame = evSummary.playsoundFrame,
                            effectPath = evSummary.effectPath,
                            series = series,
                            skillParam1 = skillParam1,
                            skillParam2 = skillParam2,
                            skillParam3 = skillParam3,
                            skillParam4 = skillParam4
                        };

                        skills[skillId] = data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillDatabase] ❌ Lỗi đọc Skill.csv: {ex.Message}");
            }
        }

        private Dictionary<int, ActionEventSummary> ParseActionEventFile(string actionEventPath)
        {
            var map = new Dictionary<int, ActionEventSummary>();

            try
            {
                using (var fs = new FileStream(actionEventPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return map;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] tokens = CsvParserHelper.SplitCsvLine(line);
                        if (tokens.Length < 5) continue;

                        int actEventId = CsvParserHelper.ParseInt(tokens[1]);
                        if (actEventId <= 0) continue;

                        int eventType = CsvParserHelper.ParseInt(tokens[2], 0);
                        int frame = CsvParserHelper.ParseInt(tokens[3], 0);
                        string eventName = tokens[4].Trim();
                        string p1 = CsvParserHelper.GetToken(tokens, 5);
                        string p2 = CsvParserHelper.GetToken(tokens, 6);

                        if (!map.TryGetValue(actEventId, out ActionEventSummary summary))
                        {
                            summary = new ActionEventSummary
                            {
                                crossFade = 0.08f,
                                candoskill = -1,
                                castSkill = 2,
                                canDoRun = -1,
                                castLinkSkill = -1,
                                param1 = -1,
                                param2 = -1,
                                playsound = -1,
                                playsoundFrame = -1,
                                effectPath = "",
                                slotId = 0,
                                movePosDistance = 0f,
                                movePosSpeed = 0f,
                                movePosAccel = 0f,
                                movePosFrame = -1
                            };
                        }

                        // EventType 1: Bắt đầu hành động (Start)
                        if (eventType == 1)
                        {
                            if (eventName.Equals("CrossFade", StringComparison.OrdinalIgnoreCase))
                            {
                                float fadeVal = CsvParserHelper.ParseFloat(p1, 1f);
                                float fadeSec = fadeVal > 10f ? (fadeVal / 1000f) : 0.05f;
                                summary.crossFade = fadeSec;
                            }
                            else if (eventName.Equals("LinkSkillInit", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.param1 = CsvParserHelper.ParseInt(p1, -1);
                                summary.param2 = CsvParserHelper.ParseInt(p2, -1);
                            }
                            else if (eventName.Equals("PlaySound", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.playsound = CsvParserHelper.ParseInt(p1, -1);
                                summary.playsoundFrame = 0;
                            }
                            else if (eventName.Equals("PlayEffect", StringComparison.OrdinalIgnoreCase))
                            {
                                int resId = CsvParserHelper.ParseInt(p1, 0);
                                string path = EffectDatabase.GetEffectPath(resId);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    summary.effectPath = path;
                                }
                                int slot = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 8), 0);
                                if (slot > 0)
                                {
                                    summary.slotId = slot;
                                }
                            }
                        }
                        // EventType 2: Dòng thời gian từng frame (Timeline)
                        else if (eventType == 2)
                        {
                            if (eventName.Equals("CanDoSkill", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.candoskill = frame;
                            }
                            else if (eventName.Equals("CastSkill", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.castSkill = frame;
                            }
                            else if (eventName.Equals("CastLinkSkill", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.castLinkSkill = frame;
                            }
                            else if (eventName.Equals("CanDoRun", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.canDoRun = frame;
                            }
                            else if (eventName.Equals("instantdir", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.instantDir = frame;
                            }
                            else if (eventName.Equals("PlaySound", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.playsound = CsvParserHelper.ParseInt(p1, -1);
                                summary.playsoundFrame = frame;
                            }
                            else if (eventName.Equals("PlayEffect", StringComparison.OrdinalIgnoreCase))
                            {
                                int resId = CsvParserHelper.ParseInt(p1, 0);
                                string path = EffectDatabase.GetEffectPath(resId);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    summary.effectPath = path;
                                }
                                int slot = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 8), 0);
                                if (slot > 0)
                                {
                                    summary.slotId = slot;
                                }
                            }
                            else if (eventName.Equals("MovePos", StringComparison.OrdinalIgnoreCase))
                            {
                                string p3 = CsvParserHelper.GetToken(tokens, 7);
                                summary.movePosDistance = CsvParserHelper.ParseFloat(p1, 0f) / 100f;
                                summary.movePosSpeed = CsvParserHelper.ParseFloat(p2, 0f) / 10f;
                                summary.movePosAccel = CsvParserHelper.ParseFloat(p3, 0f);
                                summary.movePosFrame = frame;
                            }
                        }
                        // EventType 3: Kết thúc hành động (End / Exit) -> KHÔNG ghi đè summary.crossFade của chiêu vào!

                        map[actEventId] = summary;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillDatabase] ❌ Lỗi đọc ActionEvent.csv: {ex.Message}");
            }

            return map;
        }

        private void LoadFromLegacySkillsCsv()
        {
            string filePath = Path.Combine(Application.dataPath, "Settings", "Skills.csv");
            if (!File.Exists(filePath) && fallbackCsvFile == null)
            {
                Debug.LogWarning("[SkillDatabase] ⚠️ Không tìm thấy file dữ liệu Skills.csv cũ.");
                return;
            }

            string csvContent = fallbackCsvFile != null ? fallbackCsvFile.text : File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] tokens = CsvParserHelper.SplitCsvLine(line);
                if (tokens.Length < 10) continue;

                int id = CsvParserHelper.ParseInt(tokens[0]);
                if (id <= 0) continue;

                SkillData data = new SkillData
                {
                    id = id,
                    name = CsvParserHelper.GetToken(tokens, 1),
                    iconPath = CsvParserHelper.GetToken(tokens, 2),
                    castActionId = CastActionHelper.ParseActionId(CsvParserHelper.GetToken(tokens, 3), (int)CastActionID.at01),
                    crossFade = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 4), 0.1f),
                    movePosSpeed = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 5), 0f),
                    movePosDistance = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 5), 0f) * CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 6), 0f),
                    movePosFrame = 0,
                    skillType = ParseSkillType(CsvParserHelper.GetToken(tokens, 7)),
                    range = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 8), 5f),
                    fanAngle = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 9), 0f),
                    physScale = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 10), 1f),
                    magicScale = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 11), 0f),
                    manaCost = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 12), 0f),
                    cooldown = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 13), 0f),
                    canCancel = CsvParserHelper.ParseBool(CsvParserHelper.GetToken(tokens, 14), true),
                    linkskillinit = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 15), -1),
                    param1 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 16), -1),
                    param2 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 17), -1),
                    candoskill = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 18), -1),
                    castSkill = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 19), 0),
                    canDoRun = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 20), -1),
                    castLinkSkill = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 21), -1),
                    playsound = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 22), -1),
                    playsoundFrame = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 23), -1),
                    effectPath = CsvParserHelper.GetToken(tokens, 24)
                };

                skills[id] = data;
            }

            isLoaded = true;
            Debug.Log($"✅ [SkillDatabase] Đã nạp {skills.Count} kỹ năng từ file cũ (Fallback)!");
        }

        private string GetColRaw(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < tokens.Length) return tokens[idx];
            return fallbackIndex < tokens.Length ? tokens[fallbackIndex] : "";
        }

        private SkillType ParseSkillType(string s)
        {
            if (Enum.TryParse<SkillType>(s, true, out var result))
            {
                return result;
            }
            return SkillType.StraightRay;
        }

        public static SkillData GetSkill(int id)
        {
            if (id <= 0) return null;
            Instance.EnsureLoaded();
            Instance.skills.TryGetValue(id, out SkillData data);
            return data;
        }

        public static bool HasSkill(int id)
        {
            if (id <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.skills.ContainsKey(id);
        }

        public static Dictionary<int, SkillData> GetAllSkills()
        {
            Instance.EnsureLoaded();
            return Instance.skills;
        }

        public static void Reload()
        {
            Instance.LoadDatabase();
        }
    }
}

