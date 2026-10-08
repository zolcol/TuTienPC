using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Combat
{
    public class PooledVfx : MonoBehaviour
    {
        public string PoolKey;
        public Coroutine AutoRecycleCoroutine;
        private VfxLockRotation lockRotationComponent;
        private ParticleSystem[] cachedParticleSystems;
        private TrailRenderer[] cachedTrailRenderers;
        private Renderer[] cachedRenderers;

        public void CacheComponents()
        {
            lockRotationComponent = GetComponent<VfxLockRotation>();
            cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
            cachedTrailRenderers = GetComponentsInChildren<TrailRenderer>(true);
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public void ResetAndPlay()
        {
            if (cachedParticleSystems != null)
            {
                for (int i = 0; i < cachedParticleSystems.Length; i++)
                {
                    if (cachedParticleSystems[i] != null)
                    {
                        cachedParticleSystems[i].Clear();
                        cachedParticleSystems[i].Play();
                    }
                }
            }
            if (cachedTrailRenderers != null)
            {
                for (int i = 0; i < cachedTrailRenderers.Length; i++)
                {
                    if (cachedTrailRenderers[i] != null)
                    {
                        cachedTrailRenderers[i].Clear();
                    }
                }
            }
            if (cachedRenderers != null)
            {
                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    if (cachedRenderers[i] != null)
                    {
                        cachedRenderers[i].enabled = true;
                    }
                }
            }
        }

        public void StopEffects()
        {
            if (cachedParticleSystems != null)
            {
                for (int i = 0; i < cachedParticleSystems.Length; i++)
                {
                    if (cachedParticleSystems[i] != null)
                    {
                        cachedParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }
            if (cachedTrailRenderers != null)
            {
                for (int i = 0; i < cachedTrailRenderers.Length; i++)
                {
                    if (cachedTrailRenderers[i] != null)
                    {
                        cachedTrailRenderers[i].Clear();
                    }
                }
            }
        }
    }

    public class EffectManager : MonoBehaviour
    {
        private static EffectManager instance;
        private static bool isApplicationQuitting = false;

        public static bool HasInstance => instance != null && !isApplicationQuitting;

        public static EffectManager Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    instance = FindObjectOfType<EffectManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[EffectManager]");
                        instance = go.AddComponent<EffectManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, string> keyCache = new Dictionary<string, string>();
        private readonly Dictionary<string, Queue<GameObject>> vfxPools = new Dictionary<string, Queue<GameObject>>(System.StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            instance = null;
            isApplicationQuitting = false;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
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
            if (skill.startPosType == VfxStartPosType.Target)
            {
                if (target != null)
                {
                    spawnRoot = target;
                }
                else if (hitPoint.HasValue)
                {
                    Vector3 snappedHitPoint = CombatFormula.SnapToGround(hitPoint.Value, CombatFormula.GROUND_VFX_Y_OFFSET);
                    return SpawnEffect(skill.effectPath, snappedHitPoint, attacker.rotation, null, defaultDuration);
                }
            }
            else if (skill.startPosType == VfxStartPosType.HitPoint && hitPoint.HasValue)
            {
                Vector3 snappedHitPoint = CombatFormula.SnapToGround(hitPoint.Value, CombatFormula.GROUND_VFX_Y_OFFSET);
                return SpawnEffect(skill.effectPath, snappedHitPoint, attacker.rotation, null, defaultDuration);
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

            Vector3 spawnPos = (mode == VfxRotationMode.FlatGround) ? CombatFormula.SnapToGround(characterRoot.position, CombatFormula.GROUND_VFX_Y_OFFSET) : boneSlot.position;
            Quaternion spawnRot = (mode == VfxRotationMode.FlatGround) ? Quaternion.Euler(0f, characterRoot.eulerAngles.y, 0f) : boneSlot.rotation;

            GameObject effectInstance = SpawnEffect(resourcePath, spawnPos, spawnRot, null, autoDestroyTime);
            if (effectInstance == null) return null;

            if (attachToBone)
            {
                VfxLockRotation lockRot = effectInstance.GetComponent<VfxLockRotation>() ?? effectInstance.AddComponent<VfxLockRotation>();
                lockRot.enabled = true;
                lockRot.Initialize(characterRoot, mode == VfxRotationMode.FlatGround ? null : boneSlot, mode);
            }

            return effectInstance;
        }

        /// <summary>
        /// Sinh ra hiệu ứng bám theo chân mục tiêu (Mặt đất phẳng FlatGround theo characterRoot.position raycast xuống sàn + 0.02m)
        /// Dùng cho Missile đài sen hồi máu (Chiêu 306), vòng sáng trận pháp đất, hoặc đạn/hiệu ứng bám mục tiêu không có Slot xương.
        /// Chuẩn hóa theo DATA_CONVENTIONS.md Mục 12 & 13.
        /// </summary>
        public GameObject SpawnEffectFollowTargetGround(string resourcePath, Transform characterRoot, float autoDestroyTime = 5.0f)
        {
            if (string.IsNullOrEmpty(resourcePath) || characterRoot == null) return null;

            Vector3 spawnPos = CombatFormula.SnapToGround(characterRoot.position, CombatFormula.GROUND_VFX_Y_OFFSET);
            Quaternion spawnRot = Quaternion.Euler(0f, characterRoot.eulerAngles.y, 0f);

            GameObject effectInstance = SpawnEffect(resourcePath, spawnPos, spawnRot, null, autoDestroyTime);
            if (effectInstance == null) return null;

            VfxLockRotation lockRot = effectInstance.GetComponent<VfxLockRotation>() ?? effectInstance.AddComponent<VfxLockRotation>();
            lockRot.enabled = true;
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
                VfxLockRotation lockRot = effectInstance.GetComponent<VfxLockRotation>() ?? effectInstance.AddComponent<VfxLockRotation>();
                lockRot.enabled = true;
                lockRot.Initialize(characterRoot, boneSlot, mode);
            }

            return effectInstance;
        }

        /// <summary>
        /// Sinh ra hiệu ứng từ đường dẫn prefab trong Resources có hỗ trợ Object Pooling
        /// </summary>
        public GameObject SpawnEffect(string resourcePath, Vector3 position, Quaternion rotation, Transform parent = null, float autoDestroyTime = 2.5f)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;

            if (!keyCache.TryGetValue(resourcePath, out string cleanKey))
            {
                cleanKey = resourcePath.Replace("\\", "/").Trim().ToLowerInvariant();
                keyCache[resourcePath] = cleanKey;
            }
            GameObject effectInstance = null;
            PooledVfx pooled = null;

            if (vfxPools.TryGetValue(cleanKey, out Queue<GameObject> poolQueue))
            {
                while (poolQueue.Count > 0)
                {
                    GameObject candidate = poolQueue.Dequeue();
                    if (candidate != null)
                    {
                        effectInstance = candidate;
                        pooled = effectInstance.GetComponent<PooledVfx>();
                        break;
                    }
                }
            }
            else
            {
                vfxPools[cleanKey] = new Queue<GameObject>();
            }

            if (effectInstance == null)
            {
                GameObject prefab = LoadEffectPrefab(resourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[EffectManager] ⚠️ Không tìm thấy VFX prefab tại đường dẫn: Resources/{resourcePath}");
                    return null;
                }

                effectInstance = Instantiate(prefab, transform);
                pooled = effectInstance.AddComponent<PooledVfx>();
                pooled.PoolKey = cleanKey;
                pooled.CacheComponents();
            }

            // Thiết lập vị trí / cha
            if (parent != null)
            {
                effectInstance.transform.SetParent(parent);
                effectInstance.transform.localPosition = Vector3.zero;
                effectInstance.transform.localRotation = Quaternion.identity;

                if (TopDownGame.Data.EffectDatabase.IsLockRotate(resourcePath))
                {
                    VfxLockRotation lockRot = effectInstance.GetComponent<VfxLockRotation>() ?? effectInstance.AddComponent<VfxLockRotation>();
                    lockRot.enabled = true;
                    Transform root = parent.root != null ? parent.root : parent;
                    lockRot.Initialize(root, parent, VfxRotationMode.FlatGround);
                }
            }
            else
            {
                effectInstance.transform.SetParent(null);
                if (TopDownGame.Data.EffectDatabase.IsLockRotate(resourcePath))
                {
                    rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                }
                effectInstance.transform.position = position;
                effectInstance.transform.rotation = rotation;
            }

            effectInstance.SetActive(true);
            pooled.ResetAndPlay();

            // Lập lịch tự động thu hồi về Pool thay vì Destroy
            if (pooled.AutoRecycleCoroutine != null)
            {
                StopCoroutine(pooled.AutoRecycleCoroutine);
                pooled.AutoRecycleCoroutine = null;
            }

            if (autoDestroyTime > 0f)
            {
                pooled.AutoRecycleCoroutine = StartCoroutine(AutoRecycleRoutine(pooled, autoDestroyTime));
            }

            return effectInstance;
        }

        private IEnumerator AutoRecycleRoutine(PooledVfx pooled, float delay)
        {
            float timer = delay;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            if (pooled != null && pooled.gameObject != null && pooled.gameObject.activeInHierarchy)
            {
                RecycleEffect(pooled.gameObject);
            }
        }

        /// <summary>
        /// Thu hồi hiệu ứng VFX về Object Pool (0 GC Alloc, 0 Destroy)
        /// </summary>
        public void RecycleEffect(GameObject effectInstance)
        {
            if (effectInstance == null) return;
            if (isApplicationQuitting || !Application.isPlaying) return;

            if (effectInstance.TryGetComponent<PooledVfx>(out PooledVfx pooled))
            {
                if (pooled.AutoRecycleCoroutine != null)
                {
                    StopCoroutine(pooled.AutoRecycleCoroutine);
                    pooled.AutoRecycleCoroutine = null;
                }

                VfxLockRotation lockRot = effectInstance.GetComponent<VfxLockRotation>();
                if (lockRot != null)
                {
                    lockRot.enabled = false;
                }

                pooled.StopEffects();

                if (this != null && gameObject != null && effectInstance.transform.parent != transform)
                {
                    try
                    {
                        effectInstance.transform.SetParent(transform);
                    }
                    catch (System.Exception)
                    {
                        // Bỏ qua nếu parent đang trong chu trình active/deactive của Unity
                    }
                }
                effectInstance.SetActive(false);

                if (!string.IsNullOrEmpty(pooled.PoolKey))
                {
                    if (!vfxPools.TryGetValue(pooled.PoolKey, out Queue<GameObject> poolQueue))
                    {
                        poolQueue = new Queue<GameObject>();
                        vfxPools[pooled.PoolKey] = poolQueue;
                    }
                    poolQueue.Enqueue(effectInstance);
                }
            }
            else
            {
                Destroy(effectInstance);
            }
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

            if (prefab != null)
            {
                prefabCache[path] = prefab;
            }
            return prefab;
        }
    }
}
