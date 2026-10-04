using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    public class PartSlotDatabase : ICsvTable
    {
        private static PartSlotDatabase instance;
        public static PartSlotDatabase Instance => instance ?? (instance = new PartSlotDatabase());

        private readonly Dictionary<int, PartSlotData> slotById = new Dictionary<int, PartSlotData>();
        private readonly Dictionary<string, PartSlotData> slotByName = new Dictionary<string, PartSlotData>(StringComparer.OrdinalIgnoreCase);
        
        private static readonly Dictionary<(int rootId, int slotId), Transform> transformCacheById = new Dictionary<(int, int), Transform>();
        private static readonly Dictionary<(int rootId, string slotName), Transform> transformCacheByName = new Dictionary<(int, string), Transform>();

        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || slotById.Count == 0) Load();
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            slotById.Clear();
            slotByName.Clear();
            transformCacheById.Clear();
            transformCacheByName.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string filePath = GameDataPaths.PartSlotCsv;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[PartSlotDatabase] ⚠️ Không tìm thấy file tại: {filePath}");
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
                        if (tokens.Length < 3) continue;

                        string des = tokens[0].Trim();
                        int slotId = CsvParserHelper.ParseInt(tokens[1]);
                        string slotName = tokens[2].Trim();

                        if (slotId <= 0 || string.IsNullOrEmpty(slotName)) continue;

                        PartSlotData data = new PartSlotData(slotId, slotName, des);
                        slotById[slotId] = data;

                        if (!slotByName.ContainsKey(slotName))
                        {
                            slotByName[slotName] = data;
                        }
                    }
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PartSlotDatabase] ❌ Lỗi đọc PartSlot.csv: {ex.Message}");
            }
        }

        public static Transform GetSlotTransform(Transform characterRoot, int slotId)
        {
            if (characterRoot == null) return null;
            if (slotId <= 0) return characterRoot;

            int rootId = characterRoot.GetInstanceID();
            var key = (rootId, slotId);
            if (transformCacheById.TryGetValue(key, out Transform cached) && cached != null)
            {
                return cached;
            }

            Instance.EnsureLoaded();
            if (Instance.slotById.TryGetValue(slotId, out PartSlotData data))
            {
                Transform found = FindBoneInHierarchy(characterRoot, data.aliasNames);
                if (found != null)
                {
                    transformCacheById[key] = found;
                    return found;
                }
            }

            transformCacheById[key] = characterRoot;
            return characterRoot;
        }

        public static Transform GetSlotTransform(Transform characterRoot, string slotName)
        {
            if (characterRoot == null) return null;
            if (string.IsNullOrEmpty(slotName)) return characterRoot;

            int rootId = characterRoot.GetInstanceID();
            var key = (rootId, slotName);
            if (transformCacheByName.TryGetValue(key, out Transform cached) && cached != null)
            {
                return cached;
            }

            Instance.EnsureLoaded();
            List<string> namesToSearch = new List<string> { slotName };
            if (Instance.slotByName.TryGetValue(slotName, out PartSlotData data))
            {
                namesToSearch = data.aliasNames;
            }

            Transform found = FindBoneInHierarchy(characterRoot, namesToSearch);
            Transform result = found != null ? found : characterRoot;
            transformCacheByName[key] = result;
            return result;
        }

        private static Transform FindBoneInHierarchy(Transform current, List<string> names)
        {
            string currentName = current.name;
            for (int i = 0; i < names.Count; i++)
            {
                if (currentName.Equals(names[i], StringComparison.OrdinalIgnoreCase)) return current;
            }

            int childCount = current.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform found = FindBoneInHierarchy(current.GetChild(i), names);
                if (found != null) return found;
            }

            return null;
        }

        public static string GetSlotName(int slotId)
        {
            Instance.EnsureLoaded();
            return Instance.slotById.TryGetValue(slotId, out var data) ? data.slotName : "";
        }

        public static TopDownGame.Combat.VfxRotationMode GetSlotRotationMode(int slotId, bool isLockRotateFromCsv = false)
        {
            if (isLockRotateFromCsv) return TopDownGame.Combat.VfxRotationMode.FlatGround;

            switch (slotId)
            {
                case 1: case 2: case 3: case 6: case 8: case 11: case 12:
                case 13: case 14: case 17: case 18: case 21: case 152: case 153:
                case 154: case 155: case 156:
                    return TopDownGame.Combat.VfxRotationMode.FollowBoneFull;

                case 19: case 20:
                    return TopDownGame.Combat.VfxRotationMode.FlatGround;

                case 15: case 16:
                    return TopDownGame.Combat.VfxRotationMode.FixedWorld;

                case 4: case 5: case 7: case 22:
                    return TopDownGame.Combat.VfxRotationMode.UprightBody;

                default:
                    return slotId > 0 ? TopDownGame.Combat.VfxRotationMode.FollowBoneFull : TopDownGame.Combat.VfxRotationMode.UprightBody;
            }
        }
    }
}
