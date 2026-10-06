using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;
using TopDownGame.Combat;

namespace TopDownGame.Skills
{
    public static class SkillCsvParser
    {
        public static void LoadStandard(string skillPath, string actionEventPath, Dictionary<int, SkillData> skills)
        {
            var eventMap = ActionEventParser.Parse(actionEventPath);

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

                        float rawRadius = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "attackradius", 38), 500f);
                        float rangeInMeters = rawRadius > 0f ? (rawRadius / 100f) : 5f;

                        float timePerCast = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "timepercast", 16), 0f);
                        float waitTime = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "waittime", 9), 0f);
                        float cooldown = timePerCast > 0f ? (timePerCast / SkillData.COOLDOWN_FPS) : (waitTime > 0f ? (waitTime / SkillData.COOLDOWN_FPS) : 0f);
                        float manaCost = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "costvalue", 55), 0f);

                        SkillTypeDef skillTypeDef = Enum.IsDefined(typeof(SkillTypeDef), rawSkillType) ? (SkillTypeDef)rawSkillType : SkillTypeDef.None;
                        int rawSeries = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "series", 18), 0);
                        ElementalSeries series = Enum.IsDefined(typeof(ElementalSeries), rawSeries) ? (ElementalSeries)rawSeries : ElementalSeries.None;
                        float skillParam1 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param1", 42), 0f);
                        float skillParam2 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param2", 44), 0f);
                        float skillParam3 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param3", 46), 0f);
                        float skillParam4 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param4", 48), 0f);
                        float skillParam5 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param5", 50), 0f);
                        float skillParam6 = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "param6", 52), 0f);
                        AcceSpeedInfo acceSpeedInfo1 = AcceSpeedInfo.Parse(GetColRaw(tokens, colMap, "accespeedinfo1", 69));
                        AcceSpeedInfo acceSpeedInfo2 = AcceSpeedInfo.Parse(GetColRaw(tokens, colMap, "accespeedinfo2", 70));
                        AcceSpeedInfo acceSpeedInfo3 = AcceSpeedInfo.Parse(GetColRaw(tokens, colMap, "accespeedinfo3", 71));

                        string iconName = GetColRaw(tokens, colMap, "icon", 7);
                        string iconAtlas = GetColRaw(tokens, colMap, "iconatlas", 8);

                        string resolvedIconPath = "";
                        if (!string.IsNullOrEmpty(iconAtlas) && !string.IsNullOrEmpty(iconName))
                        {
                            string atlasFolder = iconAtlas.Replace(".prefab", "").Trim().Replace("\\", "/");
                            resolvedIconPath = $"{atlasFolder}/{iconName.Trim()}";
                        }
                        else if (!string.IsNullOrEmpty(iconName))
                        {
                            resolvedIconPath = iconName.Trim();
                        }

                        int childId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "childid", 10), skillId);
                        var missile = MissileDatabase.GetMissile(childId);

                        string rawRelation = GetColRaw(tokens, colMap, "relation", 15).Trim();
                        SkillRelation relation = SkillRelation.Enemy;
                        if (rawRelation.Equals("recover", StringComparison.OrdinalIgnoreCase)) relation = SkillRelation.Recover;
                        else if (rawRelation.Equals("friend", StringComparison.OrdinalIgnoreCase)) relation = SkillRelation.Friend;
                        else if (rawRelation.Equals("self", StringComparison.OrdinalIgnoreCase)) relation = SkillRelation.Self;

                        string skillStyle = GetColRaw(tokens, colMap, "skillstyle", 24).Trim();
                        bool targetSelf = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "targetself", 65), 0) == 1;

                        int startSkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "startskillid", 29), 0);
                        int flySkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "flyskillid", 30), 0);
                        int flyEventInterval = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "flyeventinterval", 31), 0);
                        int hitSkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "hitskillid", 34), 0);
                        int vanishedSkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "vanishedskillid", 36), 0);
                        int subSkillId = startSkillId > 0 ? startSkillId : 0;

                        int rawStartPosType = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "startpostype", 5), 1);
                        VfxStartPosType startPosType = Enum.IsDefined(typeof(VfxStartPosType), rawStartPosType) ? (VfxStartPosType)rawStartPosType : VfxStartPosType.Caster;
                        int slotId = 0;

                        SkillType skillType = SkillType.StraightRay;
                        int missileForm = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "missileform", 12), 0);
                        int childCount = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "childcount", 11), 1);
                        if (childCount <= 0) childCount = 1;
                        int msGenerate = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "msgenerate", 13), 1);
                        string msGenerateParam = GetColRaw(tokens, colMap, "msgenerateparam", 14).Trim();
                        float fanAngle = 0f;
                        if (missileForm == 2 && skillParam2 > 0f)
                        {
                            float stepDeg = skillParam2 * (360f / 64f);
                            fanAngle = (childCount > 1) ? ((childCount - 1) * stepDeg) : stepDeg;
                        }
                        float boxWidth = 1.6f;
                        bool notChangeActFrame = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "notchangeactframe", 75), 0) == 1;

                        int rawSelectorType = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "selectortype", 63), 0);
                        float rawSelectorRange = CsvParserHelper.ParseFloat(GetColRaw(tokens, colMap, "selectorrange", 62), 0f);
                        float selectorRange = rawSelectorRange > 0f ? (rawSelectorRange / 100f) : rangeInMeters;

                        SkillSelectorType selectorType = SkillSelectorType.None;
                        if (targetSelf || relation == SkillRelation.Self)
                        {
                            selectorType = SkillSelectorType.None;
                        }
                        else if (rawSelectorType == 2 || (missile != null && missile.moveKind == MissileMoveKind.Linear) || missileForm == 1 || missileForm == 2 || missileForm == 7)
                        {
                            selectorType = SkillSelectorType.DirectionalArrow;
                        }
                        else if (rawSelectorType == 1)
                        {
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
                                    case HitboxShape.Circle: skillType = SkillType.Circle; break;
                                    case HitboxShape.Fan: skillType = SkillType.Sector; if (fanAngle <= 0f) fanAngle = missile.dmgRangeY > 0f ? missile.dmgRangeY : 90f; break;
                                    case HitboxShape.LineBox: skillType = SkillType.StraightRay; boxWidth = missile.dmgRangeY > 0f ? (missile.dmgRangeY / 10.0f) : 1.8f; break;
                                    case HitboxShape.SingleTarget: skillType = SkillType.TargetLock; boxWidth = 1.6f; break;
                                }
                            }
                        }
                        else
                        {
                            if (skillTypeDef == SkillTypeDef.Missile || skillTypeDef == SkillTypeDef.InstMissile) skillType = SkillType.Projectile;
                            else if (skillTypeDef == SkillTypeDef.InstSingle) skillType = SkillType.TargetLock;
                            else if (rawSkillType == 2) skillType = SkillType.Circle;
                            else if (rawSkillType == 3) { skillType = SkillType.Sector; if (fanAngle <= 0f) fanAngle = 90f; }
                            else skillType = SkillType.StraightRay;
                        }

                        int rawCastSound = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "castsoundid", 68), -1);
                        int rawCastEffect = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "casteffectrestid", 73), 0);

                        ActionEventSummary evSummary = new ActionEventSummary
                        {
                            playsound = rawCastSound,
                            playsoundFrame = rawCastSound > 0 ? 0 : -1,
                            effectPath = EffectDatabase.GetEffectPath(rawCastEffect)
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
                            if (matchedSummary.instantDirSpeed > 0f) evSummary.instantDirSpeed = matchedSummary.instantDirSpeed;
                            if (matchedSummary.slotId > 0) evSummary.slotId = matchedSummary.slotId;
                            if (matchedSummary.movePosFrame >= 0)
                            {
                                evSummary.movePosFrame = matchedSummary.movePosFrame;
                                evSummary.movePosDistance = matchedSummary.movePosDistance;
                                evSummary.movePosSpeed = matchedSummary.movePosSpeed;
                                evSummary.movePosAccel = matchedSummary.movePosAccel;
                            }

                            if (matchedSummary.playsound > 0)
                            {
                                evSummary.playsound = matchedSummary.playsound;
                                evSummary.playsoundFrame = matchedSummary.playsoundFrame >= 0 ? matchedSummary.playsoundFrame : 0;
                            }
                            else if (evSummary.playsound > 0 && evSummary.playsoundFrame < 0)
                            {
                                evSummary.playsoundFrame = 0;
                            }

                            if (!string.IsNullOrEmpty(matchedSummary.effectPath)) evSummary.effectPath = matchedSummary.effectPath;
                            if (matchedSummary.effectEvents != null && matchedSummary.effectEvents.Count > 0)
                            {
                                evSummary.effectEvents = new List<SkillEffectEvent>(matchedSummary.effectEvents);
                            }
                            if (matchedSummary.castEvents != null && matchedSummary.castEvents.Count > 0)
                            {
                                evSummary.castEvents = new List<SkillCastEvent>(matchedSummary.castEvents);
                            }
                        }

                        if (evSummary.effectEvents.Count == 0 && !string.IsNullOrEmpty(evSummary.effectPath))
                        {
                            evSummary.effectEvents.Add(new SkillEffectEvent
                            {
                                frame = 0,
                                effectPath = evSummary.effectPath,
                                slotId = evSummary.slotId,
                                duration = 2.5f
                            });
                        }

                        if (relation == SkillRelation.Recover)
                        {
                            slotId = (int)BoneSlotID.RightFoot;
                        }
                        else if (evSummary.slotId > 0)
                        {
                            slotId = evSummary.slotId;
                        }

                        if (evSummary.playsound <= 0 && rawCastSound > 0)
                        {
                            evSummary.playsound = rawCastSound;
                            evSummary.playsoundFrame = 0;
                        }

                        SkillData data = new SkillData
                        {
                            id = skillId,
                            name = skillName,
                            iconAtlas = iconAtlas,
                            iconName = iconName,
                            iconPath = resolvedIconPath,
                            castActionId = castActionId,
                            crossFade = evSummary.crossFade,
                            relation = relation,
                            skillStyle = skillStyle,
                            targetSelf = targetSelf,
                            subSkillId = subSkillId,
                            startSkillId = startSkillId,
                            flySkillId = flySkillId,
                            flyEventInterval = flyEventInterval,
                            hitSkillId = hitSkillId,
                            vanishedSkillId = vanishedSkillId,
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
                            skillTypeDef = skillTypeDef,
                            range = rangeInMeters,
                            fanAngle = fanAngle,
                            boxWidth = boxWidth,
                            physScale = 1.0f,
                            magicScale = 1.0f,
                            manaCost = manaCost,
                            cooldown = cooldown,
                            canCancel = true,
                            notChangeActFrame = notChangeActFrame,
                            param1 = evSummary.param1,
                            param2 = evSummary.param2,
                            candoskill = evSummary.candoskill,
                            castSkill = evSummary.castSkill,
                            castEvents = new List<SkillCastEvent>(evSummary.castEvents),
                            canDoRun = evSummary.canDoRun,
                            castLinkSkill = evSummary.castLinkSkill,
                            instantDir = evSummary.instantDir,
                            instantDirSpeed = evSummary.instantDirSpeed,
                            playsound = evSummary.playsound,
                            playsoundFrame = evSummary.playsoundFrame,
                            effectPath = evSummary.effectPath,
                            stateEffectId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "stateeffectid", 23), 0),
                            effectEvents = new List<SkillEffectEvent>(evSummary.effectEvents),
                            series = series,
                            skillParam1 = skillParam1,
                            skillParam2 = skillParam2,
                            skillParam3 = skillParam3,
                            skillParam4 = skillParam4,
                            skillParam5 = skillParam5,
                            skillParam6 = skillParam6,
                            acceSpeedInfo1 = acceSpeedInfo1,
                            acceSpeedInfo2 = acceSpeedInfo2,
                            acceSpeedInfo3 = acceSpeedInfo3
                        };

                        if (data.castEvents.Count == 0)
                        {
                            data.castEvents.Add(new SkillCastEvent { frame = data.castSkill >= 0 ? data.castSkill : 2, skillId = skillId, skillLevel = 1 });
                        }
                        else if (data.castSkill < 0)
                        {
                            data.castSkill = data.castEvents[0].frame;
                        }

                        skills[skillId] = data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillCsvParser] ❌ Lỗi đọc Skill.csv: {ex.Message}");
            }
        }

        /// <summary>
        /// Nạp danh sách kỹ năng tùy chỉnh từ CustomSkill.csv (kế thừa visual từ baseSkills và ghi đè gameplay vào customSkills)
        /// </summary>
        public static void LoadCustomSkills(
            string customSkillPath, 
            Dictionary<int, SkillData> baseSkills, 
            Dictionary<int, SkillData> customSkills,
            Dictionary<int, int> baseToCustomMap = null)
        {
            if (string.IsNullOrEmpty(customSkillPath) || !File.Exists(customSkillPath)) return;

            try
            {
                using (var fs = new FileStream(customSkillPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                        if (tokens.Length < 2) continue;

                        int skillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "skillid", 0));
                        if (skillId <= 0) continue;

                        int baseSkillId = CsvParserHelper.ParseInt(GetColRaw(tokens, colMap, "baseskillid", 1));

                        SkillData data = null;
                        if (baseSkillId > 0 && baseSkills != null && baseSkills.TryGetValue(baseSkillId, out SkillData baseData))
                        {
                            data = baseData.Clone();
                        }
                        else
                        {
                            data = new SkillData();
                        }

                        data.id = skillId;
                        data.baseSkillId = baseSkillId;

                        string skillName = GetColRaw(tokens, colMap, "skillname", 2);
                        if (!string.IsNullOrEmpty(skillName)) data.name = skillName.Trim();

                        string desc = GetColRaw(tokens, colMap, "description", 3);
                        if (!string.IsNullOrEmpty(desc)) data.description = desc.Trim();

                        string iconPath = GetColRaw(tokens, colMap, "iconpath", 4);
                        if (!string.IsNullOrEmpty(iconPath))
                        {
                            data.iconPath = iconPath.Trim();
                            data.ResetCachedIcon();
                        }

                        string cdStr = GetColRaw(tokens, colMap, "cooldown", 5);
                        if (!string.IsNullOrEmpty(cdStr)) data.cooldown = CsvParserHelper.ParseFloat(cdStr, data.cooldown);

                        string manaStr = GetColRaw(tokens, colMap, "manacost", 6);
                        if (!string.IsNullOrEmpty(manaStr)) data.manaCost = CsvParserHelper.ParseFloat(manaStr, data.manaCost);

                        string baseDmgStr = GetColRaw(tokens, colMap, "basedamage", 7);
                        if (!string.IsNullOrEmpty(baseDmgStr)) data.baseDamage = CsvParserHelper.ParseFloat(baseDmgStr, 0f);

                        string physScaleStr = GetColRaw(tokens, colMap, "physscale", 8);
                        if (!string.IsNullOrEmpty(physScaleStr)) data.physScale = CsvParserHelper.ParseFloat(physScaleStr, data.physScale);

                        string magicScaleStr = GetColRaw(tokens, colMap, "magicscale", 9);
                        if (!string.IsNullOrEmpty(magicScaleStr)) data.magicScale = CsvParserHelper.ParseFloat(magicScaleStr, data.magicScale);

                        string baseHealStr = GetColRaw(tokens, colMap, "baseheal", 10);
                        if (!string.IsNullOrEmpty(baseHealStr)) data.baseHeal = CsvParserHelper.ParseFloat(baseHealStr, 0f);

                        string healScaleStr = GetColRaw(tokens, colMap, "healscale", 11);
                        if (!string.IsNullOrEmpty(healScaleStr)) data.healScale = CsvParserHelper.ParseFloat(healScaleStr, 0f);

                        customSkills[skillId] = data;

                        if (baseSkillId > 0 && baseToCustomMap != null)
                        {
                            baseToCustomMap[baseSkillId] = skillId;
                        }
                    }

                    // Tự động remap các ID liên kết (NextComboSkillId, sub-skills, castEvents) sang custom ID
                    if (baseToCustomMap != null && baseToCustomMap.Count > 0)
                    {
                        foreach (var data in customSkills.Values)
                        {
                            // 1. Ánh xạ combo kế tiếp (NextComboSkillId / param2)
                            if (data.param2 > 0 && baseToCustomMap.TryGetValue(data.param2, out int mappedComboId))
                            {
                                data.param2 = mappedComboId;
                            }

                            // 2. Ánh xạ các sub-skill liên kết
                            if (data.startSkillId > 0 && baseToCustomMap.TryGetValue(data.startSkillId, out int mappedStartId))
                            {
                                data.startSkillId = mappedStartId;
                            }
                            if (data.flySkillId > 0 && baseToCustomMap.TryGetValue(data.flySkillId, out int mappedFlyId))
                            {
                                data.flySkillId = mappedFlyId;
                            }
                            if (data.hitSkillId > 0 && baseToCustomMap.TryGetValue(data.hitSkillId, out int mappedHitId))
                            {
                                data.hitSkillId = mappedHitId;
                            }
                            if (data.vanishedSkillId > 0 && baseToCustomMap.TryGetValue(data.vanishedSkillId, out int mappedVanId))
                            {
                                data.vanishedSkillId = mappedVanId;
                            }
                            if (data.subSkillId > 0 && baseToCustomMap.TryGetValue(data.subSkillId, out int mappedSubId))
                            {
                                data.subSkillId = mappedSubId;
                            }

                            // 3. Ánh xạ castEvents
                            if (data.castEvents != null)
                            {
                                for (int i = 0; i < data.castEvents.Count; i++)
                                {
                                    var ev = data.castEvents[i];
                                    if (ev.skillId == data.baseSkillId)
                                    {
                                        ev.skillId = data.id;
                                    }
                                    else if (ev.skillId > 0 && baseToCustomMap.TryGetValue(ev.skillId, out int mappedEvId))
                                    {
                                        ev.skillId = mappedEvId;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillCsvParser] ❌ Lỗi đọc CustomSkill.csv: {ex.Message}");
            }
        }

        private static string GetColRaw(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < tokens.Length) return tokens[idx];
            return fallbackIndex < tokens.Length ? tokens[fallbackIndex] : "";
        }
    }
}
