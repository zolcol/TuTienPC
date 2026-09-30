using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.Skills
{
    public class ActionEventSummary
    {
        public int instantDir = -1;
        public float instantDirSpeed = 1000f;
        public float crossFade = 0.1f;
        public int candoskill = -1;
        public int castSkill = 2;
        public List<SkillCastEvent> castEvents = new List<SkillCastEvent>();
        public int canDoRun = -1;
        public int castLinkSkill = -1;
        public int param1 = -1;
        public int param2 = -1;
        public int playsound = -1;
        public int playsoundFrame = -1;
        public string effectPath = "";
        public int slotId = 0;
        public float movePosDistance = 0f;
        public float movePosSpeed = 0f;
        public float movePosAccel = 0f;
        public int movePosFrame = -1;
        public List<SkillEffectEvent> effectEvents = new List<SkillEffectEvent>();
    }

    public static class ActionEventParser
    {
        public static Dictionary<int, ActionEventSummary> Parse(string actionEventPath)
        {
            var map = new Dictionary<int, ActionEventSummary>();
            if (!File.Exists(actionEventPath)) return map;

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
                            summary = new ActionEventSummary();
                            map[actEventId] = summary;
                        }

                        // EventType 1: Khởi đầu (Start)
                        if (eventType == 1)
                        {
                            if (eventName.Equals("CrossFade", StringComparison.OrdinalIgnoreCase))
                            {
                                float fadeVal = CsvParserHelper.ParseFloat(p1, 1f);
                                summary.crossFade = fadeVal > 10f ? (fadeVal / 1000f) : 0.05f;
                            }
                            else if (eventName.Equals("LinkSkillInit", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.param1 = CsvParserHelper.ParseInt(p1, -1);
                                summary.param2 = CsvParserHelper.ParseInt(p2, -1);
                            }
                            else if (eventName.Equals("InstantDir", StringComparison.OrdinalIgnoreCase))
                            {
                                float speed = CsvParserHelper.ParseFloat(p1, 1000f);
                                if (speed > 0f) summary.instantDirSpeed = speed;
                                summary.instantDir = 0;
                            }
                            else if (eventName.Equals("PlaySound", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.playsound = CsvParserHelper.ParseInt(p1, -1);
                                summary.playsoundFrame = 0;
                            }
                            else if (eventName.Equals("CastSkill", StringComparison.OrdinalIgnoreCase))
                            {
                                int targetSkillId = CsvParserHelper.ParseInt(p1, 0);
                                int skillLv = CsvParserHelper.ParseInt(p2, 1);
                                summary.castEvents.Add(new SkillCastEvent { frame = frame, skillId = targetSkillId, skillLevel = skillLv });
                                if (summary.castEvents.Count == 1) summary.castSkill = frame;
                            }
                            else if (eventName.Equals("PlayEffect", StringComparison.OrdinalIgnoreCase) ||
                                     eventName.Equals("PlayEffectNoClear", StringComparison.OrdinalIgnoreCase))
                            {
                                AddEffectEvent(summary, tokens, p1, frame);
                            }
                        }
                        // EventType 2: Dòng thời gian từng frame (Timeline)
                        else if (eventType == 2)
                        {
                            if (eventName.Equals("CanDoSkill", StringComparison.OrdinalIgnoreCase)) summary.candoskill = frame;
                            else if (eventName.Equals("CastSkill", StringComparison.OrdinalIgnoreCase))
                            {
                                int targetSkillId = CsvParserHelper.ParseInt(p1, 0);
                                int skillLv = CsvParserHelper.ParseInt(p2, 1);
                                summary.castEvents.Add(new SkillCastEvent { frame = frame, skillId = targetSkillId, skillLevel = skillLv });
                                if (summary.castEvents.Count == 1) summary.castSkill = frame;
                            }
                            else if (eventName.Equals("CastLinkSkill", StringComparison.OrdinalIgnoreCase)) summary.castLinkSkill = frame;
                            else if (eventName.Equals("CanDoRun", StringComparison.OrdinalIgnoreCase)) summary.canDoRun = frame;
                            else if (eventName.Equals("instantdir", StringComparison.OrdinalIgnoreCase))
                            {
                                float speed = CsvParserHelper.ParseFloat(p1, 1000f);
                                if (speed > 0f) summary.instantDirSpeed = speed;
                                summary.instantDir = frame;
                            }
                            else if (eventName.Equals("PlaySound", StringComparison.OrdinalIgnoreCase))
                            {
                                summary.playsound = CsvParserHelper.ParseInt(p1, -1);
                                summary.playsoundFrame = frame;
                            }
                            else if (eventName.Equals("PlayEffect", StringComparison.OrdinalIgnoreCase) ||
                                     eventName.Equals("PlayEffectNoClear", StringComparison.OrdinalIgnoreCase))
                            {
                                AddEffectEvent(summary, tokens, p1, frame);
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
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActionEventParser] ❌ Lỗi đọc ActionEvent.csv: {ex.Message}");
            }

            return map;
        }

        private static void AddEffectEvent(ActionEventSummary summary, string[] tokens, string p1, int frame)
        {
            int resId = CsvParserHelper.ParseInt(p1, 0);
            string path = EffectDatabase.GetEffectPath(resId);
            int slot = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 8), 0);
            float durFrames = CsvParserHelper.ParseFloat(CsvParserHelper.GetToken(tokens, 9), 0f);
            float durSec = durFrames > 0f ? (durFrames / 15.0f) : 2.5f;

            if (!string.IsNullOrEmpty(path))
            {
                if (string.IsNullOrEmpty(summary.effectPath)) summary.effectPath = path;
                if (summary.slotId <= 0 && slot > 0) summary.slotId = slot;

                summary.effectEvents.Add(new SkillEffectEvent
                {
                    frame = frame,
                    effectPath = path,
                    slotId = slot,
                    duration = durSec
                });
            }
        }
    }
}
