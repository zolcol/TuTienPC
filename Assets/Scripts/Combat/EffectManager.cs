using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Combat
{
    public class EffectManager : MonoBehaviour
    {
        private static EffectManager instance;
        public static EffectManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EffectManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[EffectManager]");
                        instance = go.AddComponent<EffectManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

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
            }
        }

        /// <summary>
        /// Kích hoạt hiệu ứng VFX tương ứng của kỹ năng theo vị trí người tung chiêu
        /// </summary>
        public GameObject PlaySkillEffect(SkillData skill, Transform attacker, float defaultDuration = 2.5f)
        {
            return PlaySkillEffect(skill, attacker, null, null, defaultDuration);
        }

        /// <summary>
        /// Kích hoạt hiệu ứng kỹ năng theo chuẩn StartPosType (Caster/Target/HitPoint) và khớp xương PartSlot
        /// </summary>
        public GameObject PlaySkillEffect(SkillData skill, Transform attacker, Transform target, Vector3? hitPoint, float defaultDuration = 2.5f)
        {
            if (skill == null || string.IsNullOrEmpty(skill.effectPath) || attacker == null) return null;

            Transform spawnRoot = attacker;
            if (skill.startPosType == VfxStartPosType.Target && target != null)
            {
                spawnRoot = target;
            }
            else if (skill.startPosType == VfxStartPosType.HitPoint && hitPoint.HasValue)
            {
                return SpawnEffect(skill.effectPath, hitPoint.Value, attacker.rotation, null, defaultDuration);
            }

            if (skill.slotId > 0)
            {
                // Tự động phân loại chế độ xoay (FollowBone, UprightBody, FlatGround) theo PartSlot và EffectRes
                return SpawnEffectAtSlot(skill.effectPath, spawnRoot, skill.slotId, defaultDuration, true);
            }

            return SpawnEffect(skill.effectPath, spawnRoot.position, spawnRoot.rotation, null, defaultDuration);
        }

        /// <summary>
        /// Sinh ra hiệu ứng gắn vào khớp xương cụ thể của nhân vật (1: Tay phải, 2: Tay trái, 7: Ngực, 15: Đầu, 19/20: Chân...)
        /// Tự động áp dụng 3 Chế độ Xoay (FollowBoneFull, UprightBody, FlatGround, FixedWorld) theo DATA_CONVENTIONS.md Mục 11 & 13
        /// </summary>
        public GameObject SpawnEffectAtSlot(string resourcePath, Transform characterRoot, int slotId, float autoDestroyTime = 2.5f, bool attachToBone = true)
        {
            if (string.IsNullOrEmpty(resourcePath) || characterRoot == null) return null;

            bool isLockRotate = TopDownGame.Data.EffectDatabase.IsLockRotate(resourcePath);
            VfxRotationMode mode = TopDownGame.Data.PartSlotDatabase.GetSlotRotationMode(slotId, isLockRotate);

            Transform boneSlot = null;
            if (mode != VfxRotationMode.FlatGround)
            {
                boneSlot = TopDownGame.Data.PartSlotDatabase.GetSlotTransform(characterRoot, slotId);
            }
            if (boneSlot == null) boneSlot = characterRoot;

            Vector3 spawnPos = (mode == VfxRotationMode.FlatGround) ? characterRoot.position : boneSlot.position;
            Quaternion spawnRot = (mode == VfxRotationMode.FlatGround) ? Quaternion.Euler(0f, characterRoot.eulerAngles.y, 0f) : boneSlot.rotation;

            GameObject effectInstance = SpawnEffect(resourcePath, spawnPos, spawnRot, null, autoDestroyTime);
            if (effectInstance == null) return null;

            if (attachToBone)
            {
                VfxLockRotation lockRot = effectInstance.AddComponent<VfxLockRotation>();
                lockRot.Initialize(characterRoot, mode == VfxRotationMode.FlatGround ? null : boneSlot, mode);
            }

            return effectInstance;
        }

        /// <summary>
        /// Sinh ra hiệu ứng bám theo chân mục tiêu (Mặt đất phẳng FlatGround theo characterRoot.position)
        /// Dùng cho Missile đài sen hồi máu (Chiêu 306), vòng sáng trận pháp đất, hoặc đạn/hiệu ứng bám mục tiêu không có Slot xương.
        /// Chuẩn hóa theo DATA_CONVENTIONS.md Mục 12 & 13.
        /// </summary>
        public GameObject SpawnEffectFollowTargetGround(string resourcePath, Transform characterRoot, float autoDestroyTime = 5.0f)
        {
            if (string.IsNullOrEmpty(resourcePath) || characterRoot == null) return null;

            Vector3 spawnPos = characterRoot.position;
            Quaternion spawnRot = Quaternion.Euler(0f, characterRoot.eulerAngles.y, 0f);

            GameObject effectInstance = SpawnEffect(resourcePath, spawnPos, spawnRot, null, autoDestroyTime);
            if (effectInstance == null) return null;

            VfxLockRotation lockRot = effectInstance.AddComponent<VfxLockRotation>();
            lockRot.Initialize(characterRoot, null, VfxRotationMode.FlatGround);

            return effectInstance;
        }

        /// <summary>
        /// Sinh ra hiệu ứng gắn theo tên khớp xương (vd: "B_RH", "B_LH", "Bip01 Spine1", "head"...)
        /// </summary>
        public GameObject SpawnEffectAtSlot(string resourcePath, Transform characterRoot, string slotName, float autoDestroyTime = 2.5f, bool attachToBone = true)
        {
            if (string.IsNullOrEmpty(resourcePath) || characterRoot == null) return null;

            Transform boneSlot = TopDownGame.Data.PartSlotDatabase.GetSlotTransform(characterRoot, slotName);
            if (boneSlot == null) boneSlot = characterRoot;

            bool isLockRotate = TopDownGame.Data.EffectDatabase.IsLockRotate(resourcePath);
            VfxRotationMode mode = isLockRotate ? VfxRotationMode.FlatGround : VfxRotationMode.FollowBoneFull;

            Vector3 spawnPos = boneSlot.position;
            Quaternion spawnRot = boneSlot.rotation;

            GameObject effectInstance = SpawnEffect(resourcePath, spawnPos, spawnRot, null, autoDestroyTime);
            if (effectInstance == null) return null;

            if (attachToBone)
            {
                VfxLockRotation lockRot = effectInstance.AddComponent<VfxLockRotation>();
                lockRot.Initialize(characterRoot, boneSlot, mode);
            }

            return effectInstance;
        }

        /// <summary>
        /// Sinh ra hiệu ứng từ đường dẫn prefab trong Resources
        /// </summary>
        public GameObject SpawnEffect(string resourcePath, Vector3 position, Quaternion rotation, Transform parent = null, float autoDestroyTime = 2.5f)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;

            GameObject prefab = LoadEffectPrefab(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[EffectManager] ⚠️ Không tìm thấy VFX prefab tại đường dẫn: Resources/{resourcePath}");
                return null;
            }

            GameObject effectInstance;
            if (parent != null)
            {
                effectInstance = Instantiate(prefab, parent);
                effectInstance.transform.localPosition = Vector3.zero;
                effectInstance.transform.localRotation = Quaternion.identity;

                // Tự động kích hoạt khóa xoay phương ngang nếu cột LockRotate trong EffectRes.csv = 1
                if (TopDownGame.Data.EffectDatabase.IsLockRotate(resourcePath))
                {
                    VfxLockRotation lockRot = effectInstance.AddComponent<VfxLockRotation>();
                    Transform root = parent.root != null ? parent.root : parent;
                    lockRot.Initialize(root, parent, VfxRotationMode.FlatGround);
                }
            }
            else
            {
                if (TopDownGame.Data.EffectDatabase.IsLockRotate(resourcePath))
                {
                    rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                }
                effectInstance = Instantiate(prefab, position, rotation, null);
            }

            if (autoDestroyTime > 0f)
            {
                Destroy(effectInstance, autoDestroyTime);
            }

            return effectInstance;
        }

        private GameObject LoadEffectPrefab(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (prefabCache.TryGetValue(path, out GameObject cached))
            {
                return cached;
            }

            string cleanPath = path.Replace("\\", "/").TrimStart('/');
            if (cleanPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(0, cleanPath.Length - 7);
            }

            // 1. Thử tải qua Resources.Load chuẩn
            GameObject prefab = Resources.Load<GameObject>(cleanPath);

#if UNITY_EDITOR
            // 2. Fallback linh hoạt trong Unity Editor tìm file trong Assets/resources/ hoặc toàn project
            if (prefab == null)
            {
                string directPath = $"Assets/resources/{cleanPath}.prefab";
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(directPath);

                if (prefab == null)
                {
                    string altDirect = $"Assets/{cleanPath}.prefab";
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(altDirect);
                }

                if (prefab == null)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(cleanPath);
                    string[] guids = UnityEditor.AssetDatabase.FindAssets($"{fileName} t:Prefab");
                    foreach (var guid in guids)
                    {
                        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (System.IO.Path.GetFileNameWithoutExtension(assetPath).Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            if (prefab != null) break;
                        }
                    }
                }
            }
#endif

            if (prefab != null)
            {
                prefabCache[path] = prefab;
            }
            return prefab;
        }
    }
}
