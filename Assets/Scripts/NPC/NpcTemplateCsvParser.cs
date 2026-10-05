using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;
using TopDownGame.Combat;

namespace TopDownGame.NPC
{
    public static class NpcTemplateCsvParser
    {
        public static void LoadStandard(string filePath, Dictionary<int, NpcTemplateData> templates)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                        if (tokens.Length < 3) continue;

                        int id = GetColInt(tokens, colMap, "templateid", 1);
                        if (id <= 0) continue;

                        string name = GetColString(tokens, colMap, "name", 2);
                        NpcKind kind = ParseKind(GetColString(tokens, colMap, "kind", 3));
                        int camp = GetColInt(tokens, colMap, "camp", 4, 1);
                        int resId = GetColInt(tokens, colMap, "npcresid", 7);
                        int attribId = GetColInt(tokens, colMap, "npcattribid", 12);

                        int skill1 = GetColInt(tokens, colMap, "normalskill1", 13);
                        int skill2 = GetColInt(tokens, colMap, "normalskill2", 15);
                        int skill3 = GetColInt(tokens, colMap, "normalskill3", 17);

                        float rawVision = GetColFloat(tokens, colMap, "visionradius", 19, 0f);
                        float rawActive = GetColFloat(tokens, colMap, "activeradius", 20, 0f);
                        float visionMeters = rawVision / 100f;
                        float activeMeters = rawActive / 100f;

                        string aiFile = GetColString(tokens, colMap, "aifile", 21);
                        if (string.IsNullOrWhiteSpace(aiFile)) aiFile = "CommonActive";

                        int forbitMoveVal = GetColInt(tokens, colMap, "forbitmove", 27, 0);
                        bool forbitMove = forbitMoveVal == 1;

                        float rawSpeed = GetColFloat(tokens, colMap, "runspeed", 25, 0f);
                        float runSpeed = rawSpeed > 0f ? (rawSpeed * 15.0f / 100.0f) : 5.0f;

                        float rawWalkSpeed = GetColFloat(tokens, colMap, "walkspeed", 34, 0f);
                        float walkSpeed = rawWalkSpeed > 0f ? (rawWalkSpeed * 15.0f / 100.0f) : (runSpeed * 0.5f);

                        string prefab = "";
                        var resData = NpcResDatabase.GetRes(resId);
                        if (resData != null)
                        {
                            prefab = resData.resFile;
                        }

                        NpcTemplateData data = new NpcTemplateData
                        {
                            id = id,
                            name = name,
                            kind = kind,
                            camp = camp,
                            npcResId = resId,
                            npcAttribId = attribId,
                            skill = 0,
                            skill1 = skill1,
                            skill2 = skill2,
                            skill3 = skill3,
                            visionRadius = visionMeters,
                            activeRadius = activeMeters,
                            aiFile = aiFile,
                            forbitMove = forbitMove,
                            runSpeed = runSpeed,
                            walkSpeed = walkSpeed,
                            prefab = prefab
                        };

                        templates[id] = data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcTemplateCsvParser] ❌ Lỗi đọc {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        public static void LoadLegacy(string filePath, Dictionary<int, NpcTemplateData> templates)
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
                if (tokens.Length < 3) continue;

                int id = CsvParserHelper.ParseInt(tokens[0]);
                if (id <= 0) continue;

                NpcTemplateData data = new NpcTemplateData
                {
                    id = id,
                    name = CsvParserHelper.GetToken(tokens, 1),
                    kind = ParseKind(CsvParserHelper.GetToken(tokens, 2)),
                    skill = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 3)),
                    skill1 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 4)),
                    skill2 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 5)),
                    skill3 = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 6)),
                    prefab = CsvParserHelper.GetToken(tokens, 7)
                };

                templates[id] = data;
            }
        }

        private static string GetColRaw(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < tokens.Length) return tokens[idx];
            return fallbackIndex < tokens.Length ? tokens[fallbackIndex] : "";
        }

        private static string GetColString(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex)
        {
            return GetColRaw(tokens, colMap, key, fallbackIndex).Trim();
        }

        private static int GetColInt(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, int def = 0)
        {
            string raw = GetColRaw(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseInt(raw, def);
        }

        private static float GetColFloat(string[] tokens, Dictionary<string, int> colMap, string key, int fallbackIndex, float def = 0f)
        {
            string raw = GetColRaw(tokens, colMap, key, fallbackIndex);
            return CsvParserHelper.ParseFloat(raw, def);
        }

        private static NpcKind ParseKind(string token)
        {
            if (string.IsNullOrEmpty(token)) return NpcKind.Normal;
            token = token.Trim().ToLowerInvariant();

            if (int.TryParse(token, out int intVal) && Enum.IsDefined(typeof(NpcKind), intVal))
            {
                return (NpcKind)intVal;
            }

            switch (token)
            {
                case "normal":
                case "monster":
                case "enemy":
                    return NpcKind.Normal;
                case "player":
                    return NpcKind.Player;
                case "dialoger":
                case "dialog":
                case "npc":
                    return NpcKind.Dialoger;
                case "partner":
                case "pet":
                    return NpcKind.Partner;
                case "silencer":
                case "portal":
                    return NpcKind.Silencer;
                case "gather":
                case "chest":
                    return NpcKind.Gather;
                case "trap":
                    return NpcKind.Trap;
                default:
                    return NpcKind.Normal;
            }
        }
    }
}
