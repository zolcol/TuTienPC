using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.NPC
{
    public class NpcTemplateDatabase : ICsvTable
    {
        private static NpcTemplateDatabase instance;
        public static NpcTemplateDatabase Instance => instance ?? (instance = new NpcTemplateDatabase());

        private readonly Dictionary<int, NpcTemplateData> templates = new Dictionary<int, NpcTemplateData>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || templates.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            templates.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            NpcResDatabase.Instance.EnsureLoaded();
            NpcAttributeDatabase.Instance.EnsureLoaded();

            string nTemplatePath = GameDataPaths.NpcTemplateCsv;
            string nCharacterPath = GameDataPaths.CharacterCsv;

            if (File.Exists(nTemplatePath))
            {
                NpcTemplateCsvParser.LoadStandard(nTemplatePath, templates);
                if (File.Exists(nCharacterPath))
                {
                    NpcTemplateCsvParser.LoadStandard(nCharacterPath, templates);
                }

                if (templates.Count > 0)
                {
                    IsLoaded = true;
                    return;
                }
            }

            string legacyPath = Path.Combine(Application.dataPath, "Settings", "NpcTemplate.csv");
            NpcTemplateCsvParser.LoadLegacy(legacyPath, templates);
            IsLoaded = true;
        }

        public static NpcTemplateData GetTemplate(int id)
        {
            if (id <= 0) return null;
            Instance.EnsureLoaded();
            Instance.templates.TryGetValue(id, out NpcTemplateData data);
            return data;
        }

        public static bool HasTemplate(int id)
        {
            if (id <= 0) return false;
            Instance.EnsureLoaded();
            return Instance.templates.ContainsKey(id);
        }

        public static Dictionary<int, NpcTemplateData> GetAllTemplates()
        {
            Instance.EnsureLoaded();
            return Instance.templates;
        }

        public static GameObject LoadPrefab(string path)
        {
            return NpcResDatabase.LoadPrefab(path);
        }

        public static void Reload()
        {
            Instance.Load();
        }
    }
}
