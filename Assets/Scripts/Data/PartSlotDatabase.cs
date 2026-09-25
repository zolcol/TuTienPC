using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class PartSlotData
    {
        public int slotId;
        public string slotName;
        public string description;
        public List<string> aliasNames = new List<string>();

        public PartSlotData(int id, string name, string des)
        {
            this.slotId = id;
            this.slotName = name;
            this.description = des;
            this.aliasNames.Add(name);

            // Bổ sung các tên định danh tương đương giữa các model FBX/Prefab khác nhau
            if (name.Equals("B_RH", StringComparison.OrdinalIgnoreCase) || id == 1)
            {
                aliasNames.Add("Bip01 R Hand");
                aliasNames.Add("R_Hand");
                aliasNames.Add("Weapon_R");
            }
            else if (name.Equals("B_LH", StringComparison.OrdinalIgnoreCase) || id == 2)
            {
                aliasNames.Add("Bip01 L Hand");
                aliasNames.Add("L_Hand");
                aliasNames.Add("Weapon_L");
            }
            else if (name.Equals("Bip01 Spine1", StringComparison.OrdinalIgnoreCase) || id == 7)
            {
                aliasNames.Add("Spine1");
                aliasNames.Add("Bip01 Spine");
                aliasNames.Add("Spine");
            }
            else if (name.Equals("S_Hat", StringComparison.OrdinalIgnoreCase) || id == 15 || id == 21)
            {
                aliasNames.Add("head");
                aliasNames.Add("head_bone");
                aliasNames.Add("Bip001 Head");
                aliasNames.Add("Bip01 Head");
                aliasNames.Add("Head");
            }
            else if (id == 19 || name.Equals("Bip01 R Foot", StringComparison.OrdinalIgnoreCase))
            {
                aliasNames.Add("R_Foot");
                aliasNames.Add("Foot_R");
            }
            else if (id == 20 || name.Equals("Bip01 L Foot", StringComparison.OrdinalIgnoreCase))
            {
                aliasNames.Add("L_Foot");
                aliasNames.Add("Foot_L");
            }
            else if (id == 6 || name.Equals("back", StringComparison.OrdinalIgnoreCase))
            {
                aliasNames.Add("Back");
                aliasNames.Add("B_Spine2");
            }
        }
    }

    /// <summary>
    /// Quản lý tra cứu vị trí các khớp xương / điểm gắn hiệu ứng (B_RH, B_LH, Spine1, Head, Foot...)
    /// Dựa theo DATA_CONVENTIONS.md (Mục 7) và Settings/N/PartSlot.csv
    /// </summary>
    public class PartSlotDatabase : MonoBehaviour
    {
        private static PartSlotDatabase instance;
        public static PartSlotDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PartSlotDatabase>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[PartSlotDatabase]");
                        instance = go.AddComponent<PartSlotDatabase>();
                        DontDestroyOnLoad(go);
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        private readonly Dictionary<int, PartSlotData> slotById = new Dictionary<int, PartSlotData>();
        private readonly Dictionary<string, PartSlotData> slotByName = new Dictionary<string, PartSlotData>(StringComparer.OrdinalIgnoreCase);
        
        // Cache Transform tìm được theo từng GameObject để tránh đệ quy tìm Transform liên tục (0 GC Alloc)
        private static readonly Dictionary<(int rootId, int slotId), Transform> transformCacheById = new Dictionary<(int, int), Transform>();
        private static readonly Dictionary<(int rootId, string slotName), Transform> transformCacheByName = new Dictionary<(int, string), Transform>();

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
            if (!isLoaded || slotById.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại PartSlot Database")]
        public void LoadDatabase()
        {
            slotById.Clear();
            slotByName.Clear();
            transformCacheById.Clear();
            transformCacheByName.Clear();

            string filePath = Path.Combine(Application.dataPath, "Settings", "N", "PartSlot.csv");
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

                isLoaded = true;
                Debug.Log($"✅ <color=cyan>[PartSlotDatabase]</color> Đã nạp thành công <b>{slotById.Count}</b> vị trí khớp xương từ PartSlot.csv!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PartSlotDatabase] ❌ Lỗi đọc PartSlot.csv: {ex.Message}");
            }
        }

        /// <summary>
        /// Tìm kiếm vị trí Transform của khớp xương (Slot) trên cơ thể nhân vật dựa theo SlotId (1: Tay phải, 2: Tay trái, 7: Ngực, 15: Đầu...)
        /// </summary>
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

            // Fallback an toàn nếu không tìm thấy xương
            transformCacheById[key] = characterRoot;
            return characterRoot;
        }

        /// <summary>
        /// Tìm kiếm Transform theo tên SlotName (vd: "B_RH", "B_LH", "Bip01 Spine1", "head"...)
        /// </summary>
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
                if (currentName.Equals(names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }
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

        /// <summary>
        /// Xác định chế độ xoay của hiệu ứng (FollowBoneFull, UprightBody, FlatGround, FixedWorld)
        /// theo chuẩn quy định tại DATA_CONVENTIONS.md Mục 11
        /// </summary>
        public static TopDownGame.Combat.VfxRotationMode GetSlotRotationMode(int slotId, bool isLockRotateFromCsv = false)
        {
            if (isLockRotateFromCsv) return TopDownGame.Combat.VfxRotationMode.FlatGround;

            switch (slotId)
            {
                // 1. Follow Bone Full (Xoay 100% theo xương: Tay, Vũ khí, Cánh, Lưng, Đầu 21, Thú cưỡi)
                case 1:   // B_RH
                case 2:   // B_LH
                case 3:   // B_Spine2
                case 6:   // back
                case 8:   // Bone001
                case 11:  // S_RH
                case 12:  // S_LH
                case 13:  // S_RH_01
                case 14:  // S_LH_01
                case 17:  // Bip01 R Hand
                case 18:  // Bip01 L Hand
                case 21:  // Bip001 Head
                case 152: // B_Hs
                case 153: // B_Hs001
                case 154: // B_Hs002
                case 155: // Bone033
                case 156: // Bone033(mirrored)
                    return TopDownGame.Combat.VfxRotationMode.FollowBoneFull;

                // 2. Flat Ground (Khóa phẳng Oxz mặt đất: Chân phải/trái)
                case 19:  // Bip01 R Foot
                case 20:  // Bip01 L Foot
                    return TopDownGame.Combat.VfxRotationMode.FlatGround;

                // 3. Head Dummy / Stun (Khóa cố định trục thế giới)
                case 15:  // S_Hat
                case 16:  // S_HAT_01
                    return TopDownGame.Combat.VfxRotationMode.FixedWorld;

                // 4. Upright Body (Khóa Pitch & Roll, giữ trục đứng: Spine1, Pelvis, Căn cốt)
                case 4:   // Bip001
                case 5:   // Bip01
                case 7:   // Bip01 Spine1
                case 22:  // Bip01 Pelvis
                    return TopDownGame.Combat.VfxRotationMode.UprightBody;

                default:
                    return slotId > 0 ? TopDownGame.Combat.VfxRotationMode.FollowBoneFull : TopDownGame.Combat.VfxRotationMode.UprightBody;
            }
        }
    }
}
