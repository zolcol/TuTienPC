using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.UI
{
    /// <summary>
    /// Trình quản lý Floating Text / Số nhảy sát thương trung tâm.
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
        [SerializeField] private int initialPoolSize = 32;

        private readonly Queue<FloatingTextItem> availablePool = new Queue<FloatingTextItem>();
        private Transform poolContainer;
        private Transform cachedCameraTransform;

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
        /// Spawn một số nảy sát thương cơ bản hoặc chí mạng
        /// </summary>
        public void SpawnDamage(float amount, Vector3 worldPosition, bool isPlayer = false, bool isCrit = false)
        {
            if (amount <= 0f) return;

            FloatingTextType type;
            if (isPlayer)
            {
                type = FloatingTextType.PlayerDamaged;
            }
            else if (isCrit)
            {
                type = FloatingTextType.CritDamage;
            }
            else
            {
                type = FloatingTextType.NormalDamage;
            }

            int rounded = Mathf.RoundToInt(amount);
            Spawn(type, rounded.ToString(), worldPosition);
        }

        /// <summary>
        /// Spawn số nhảy hồi máu (+HP)
        /// </summary>
        public void SpawnHeal(float amount, Vector3 worldPosition)
        {
            if (amount <= 0f) return;
            int rounded = Mathf.RoundToInt(amount);
            Spawn(FloatingTextType.Heal, rounded.ToString(), worldPosition);
        }

        /// <summary>
        /// Spawn số nhảy hồi năng lượng (+MP)
        /// </summary>
        public void SpawnMana(float amount, Vector3 worldPosition)
        {
            if (amount <= 0f) return;
            int rounded = Mathf.RoundToInt(amount);
            Spawn(FloatingTextType.ManaRestored, rounded.ToString(), worldPosition);
        }

        /// <summary>
        /// Spawn chữ MISS
        /// </summary>
        public void SpawnMiss(Vector3 worldPosition)
        {
            Spawn(FloatingTextType.Miss, "MISS", worldPosition);
        }

        /// <summary>
        /// Spawn điểm kinh nghiệm (+EXP)
        /// </summary>
        public void SpawnExp(int amount, Vector3 worldPosition)
        {
            if (amount <= 0) return;
            Spawn(FloatingTextType.ExpGain, amount.ToString(), worldPosition);
        }

        /// <summary>
        /// Spawn hiệu ứng LEVEL UP!
        /// </summary>
        public void SpawnLevelUp(Vector3 worldPosition)
        {
            Spawn(FloatingTextType.LevelUp, "LEVEL UP!", worldPosition);
        }

        /// <summary>
        /// Spawn theo enum FloatingTextType
        /// </summary>
        public void Spawn(FloatingTextType type, string content, Vector3 worldPosition)
        {
            FloatingTextResData config = GameDatabase.FloatingTexts.Get(type);
            SpawnInternal(config, content, worldPosition);
        }

        /// <summary>
        /// Spawn theo tên định danh typeName (chuẩn module mở rộng từ CSV)
        /// </summary>
        public void Spawn(string typeName, string content, Vector3 worldPosition)
        {
            FloatingTextResData config = GameDatabase.FloatingTexts.Get(typeName);
            SpawnInternal(config, content, worldPosition);
        }

        private void SpawnInternal(FloatingTextResData config, string content, Vector3 worldPosition)
        {
            if (config == null)
            {
                config = GameDatabase.FloatingTexts.Get(FloatingTextType.NormalDamage);
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
