using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.Skills
{
    public static class LegacySkillCsvParser
    {
        public static void Load(string filePath, Dictionary<int, SkillData> skills)
        {
            if (!File.Exists(filePath)) return;

            string csvContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
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
        }

        private static SkillType ParseSkillType(string s)
        {
            if (Enum.TryParse<SkillType>(s, true, out var result))
            {
                return result;
            }
            return SkillType.StraightRay;
        }
    }
}
