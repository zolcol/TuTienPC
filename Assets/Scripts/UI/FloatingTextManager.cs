using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.UI
{
    public static class NumberStringCache
    {
        private static readonly string[] cache = new string[10000];
        public static string Get(int num)
        {
            if (num >= 0 && num < 10000)
            {
                if (cache[num] == null) cache[num] = num.ToString();
                return cache[num];
            }
            return num.ToString();
        }
    }

    /// <summary>
    /// Trình quản lý Floating Text / Số nhảy sát thương và hiệu ứng chiến đấu trung tâm.
    /// Sử dụng Object Pool tái sử dụng 100%, tuân thủ nguyên tắc Zero-GC trong combat loop.
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        private static FloatingTextManager instance;
        public static FloatingTextManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<FloatingTextManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[FloatingTextManager]");
                        instance = go.AddComponent<FloatingTextManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        [Header("Pool Configuration")]
        [SerializeField] private int initialPoolSize = 100;

        private readonly Queue<FloatingTextItem> availablePool = new Queue<FloatingTextItem>();
        private Transform poolContainer;
        private Transform cachedCameraTransform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            instance = null;
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
                InitializePool();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializePool()
        {
            if (poolContainer == null)
            {
                GameObject containerGo = new GameObject("PoolContainer");
                containerGo.transform.SetParent(transform);
                poolContainer = containerGo.transform;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewItem();
            }
        }

        private FloatingTextItem CreateNewItem()
        {
            GameObject itemGo = new GameObject($"FloatingTextItem_{availablePool.Count + 1}");
            itemGo.transform.SetParent(poolContainer);
            FloatingTextItem item = itemGo.AddComponent<FloatingTextItem>();
            itemGo.SetActive(false);

            availablePool.Enqueue(item);
            return item;
        }

        private Transform GetCameraTransform()
        {
            if (cachedCameraTransform == null)
            {
                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    cachedCameraTransform = mainCam.transform;
                }
            }
            return cachedCameraTransform;
        }

        public void Recycle(FloatingTextItem item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(poolContainer);
            availablePool.Enqueue(item);
        }

        // ==================== CÁC HÀM SPAWN TIỆN ÍCH ====================

        /// <summary>
        /// Spawn một số nảy sát thương (Đòn thường hoặc Bạo kích hoặc Bị đánh)
        /// </summary>
        public void SpawnDamage(float amount, Vector3 worldPosition, bool isPlayer = false, bool isCrit = false)
        {
            if (amount <= 0f) return;

            FlyCharType type;
            if (isPlayer)
            {
                type = isCrit ? FlyCharType.HurtDeadly : FlyCharType.HurtNormal;
            }
            else
            {
                type = isCrit ? FlyCharType.HitDeadly : FlyCharType.HitNormal;
            }

            int rounded = Mathf.RoundToInt(amount);
            Spawn(type, NumberStringCache.Get(rounded), worldPosition);
        }

        /// <summary>
        /// Spawn số nhảy hồi phục (+HP)
        /// </summary>
        public void SpawnHeal(float amount, Vector3 worldPosition)
        {
            if (amount <= 0f) return;
            int rounded = Mathf.RoundToInt(amount);
            Spawn(FlyCharType.Treatment, NumberStringCache.Get(rounded), worldPosition);
        }

        /// <summary>
        /// Spawn số nhảy hồi năng lượng (+MP)
        /// </summary>
        public void SpawnMana(float amount, Vector3 worldPosition)
        {
            if (amount <= 0f) return;
            int rounded = Mathf.RoundToInt(amount);
            Spawn(FlyCharType.Treatment, NumberStringCache.Get(rounded), worldPosition);
        }

        /// <summary>
        /// Spawn chữ Đánh trượt / Né đòn
        /// </summary>
        public void SpawnMiss(Vector3 worldPosition, bool isPlayer = false)
        {
            if (isPlayer)
            {
                Spawn(FlyCharType.HurtMiss, "NÉ ĐÒN", worldPosition);
            }
            else
            {
                Spawn(FlyCharType.HitMiss, "MISS", worldPosition);
            }
        }

        /// <summary>
        /// Spawn chữ Né đòn
        /// </summary>
        public void SpawnDodge(Vector3 worldPosition)
        {
            Spawn(FlyCharType.HurtMiss, "NÉ ĐÒN", worldPosition);
        }

        /// <summary>
        /// Spawn điểm kinh nghiệm (+EXP)
        /// </summary>
        public void SpawnExp(int amount, Vector3 worldPosition)
        {
            if (amount <= 0) return;
            Spawn(FlyCharType.AddExp, NumberStringCache.Get(amount), worldPosition);
        }

        /// <summary>
        /// Spawn hiệu ứng LEVEL UP!
        /// </summary>
        public void SpawnLevelUp(Vector3 worldPosition)
        {
            Spawn(FlyCharType.AddExp, "LEVEL UP!", worldPosition);
        }

        /// <summary>
        /// Spawn theo enum FlyCharType
        /// </summary>
        public void Spawn(FlyCharType type, string content, Vector3 worldPosition)
        {
            FlyCharResData config = GameDatabase.FlyChars.Get(type);
            SpawnInternal(config, content, worldPosition);
        }

        /// <summary>
        /// Spawn theo tên định danh typeName từ CSV
        /// </summary>
        public void Spawn(string typeName, string content, Vector3 worldPosition)
        {
            FlyCharResData config = GameDatabase.FlyChars.Get(typeName);
            SpawnInternal(config, content, worldPosition);
        }

        private void SpawnInternal(FlyCharResData config, string content, Vector3 worldPosition)
        {
            if (config == null)
            {
                config = GameDatabase.FlyChars.Get(FlyCharType.HitNormal);
            }

            FloatingTextItem item;
            if (availablePool.Count > 0)
            {
                item = availablePool.Dequeue();
            }
            else
            {
                item = CreateNewItem();
                availablePool.Dequeue();
            }

            item.transform.SetParent(null);
            item.Initialize(config, content, worldPosition, GetCameraTransform(), Recycle);
        }
    }
}
