using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Data;
using TopDownGame.NPC;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TopDownGame.Enemy
{
    /// <summary>
    /// Điểm sinh quái vật (Enemy Spawn Point) với cấu hình tham số hoàn chỉnh:
    /// - ID quái vật (npcTemplateId)
    /// - Cấp độ quái (monsterLevel)
    /// - Số lượng quái tối đa (maxMonsterCount)
    /// - Bán kính khu vực sinh quái (spawnRadius)
    /// - Thời gian hồi sinh sau khi quái chết (respawnDelay)
    /// - Tự động quản lý vòng đời quái, zero GC vòng lặp, gizmos trực quan.
    /// </summary>
    [SelectionBase]
    public class EnemySpawnPoint : MonoBehaviour
    {
        [Header("=== CẤU HÌNH QUÁI VẬT ===")]
        [Tooltip("ID mẫu NPC / Quái vật trong NpcTemplate.csv")]
        [SerializeField] private int npcTemplateId = 72;

        [Tooltip("Cấp độ của quái khi sinh ra")]
        [Range(1, 200)]
        [SerializeField] private int monsterLevel = 1;

        [Header("=== CẤU HÌNH BÃI SPAWN ===")]
        [Tooltip("Số lượng quái tối đa duy trì tại bãi này")]
        [Range(1, 50)]
        [SerializeField] private int maxMonsterCount = 3;

        [Tooltip("Bán kính phạm vi sinh quái quanh tâm (mét)")]
        [Range(0.5f, 50f)]
        [SerializeField] private float spawnRadius = 6.0f;

        [Tooltip("Thời gian hồi sinh từng con sau khi quái chết (giây)")]
        [SerializeField] private float respawnDelay = 8.0f;

        [Tooltip("Khoảng thời gian giãn cách giữa các lần sinh quái (giây)")]
        [SerializeField] private float spawnInterval = 0.5f;

        [Tooltip("Tự động kích hoạt sinh quái khi bắt đầu game")]
        [SerializeField] private bool autoSpawnOnStart = true;

        [Tooltip("Tự động bắn Raycast bám xuống mặt đất/Terrain")]
        [SerializeField] private bool snapToGround = true;

        [Header("=== GIZMOS & HIỂN THỊ ===")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.6f);

        // Danh sách quản lý các con quái đang sống
        private readonly List<EnemyController> activeEnemies = new List<EnemyController>();

        // Hàng đợi đếm thời gian hồi sinh
        private readonly List<float> respawnTimers = new List<float>();

        public int NpcTemplateId { get => npcTemplateId; set => npcTemplateId = value; }
        public int MonsterLevel { get => monsterLevel; set => monsterLevel = Mathf.Max(1, value); }
        public int MaxMonsterCount { get => maxMonsterCount; set => maxMonsterCount = Mathf.Max(1, value); }
        public float SpawnRadius { get => spawnRadius; set => spawnRadius = Mathf.Max(0.5f, value); }
        public float RespawnDelay { get => respawnDelay; set => respawnDelay = Mathf.Max(0.5f, value); }
        public int ActiveCount => activeEnemies.Count;

        private void Start()
        {
            NpcTemplateDatabase.Instance.EnsureLoaded();
            if (autoSpawnOnStart)
            {
                StartCoroutine(InitialSpawnRoutine());
            }
        }

        private IEnumerator InitialSpawnRoutine()
        {
            for (int i = 0; i < maxMonsterCount; i++)
            {
                SpawnSingleEnemy();
                if (spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }

        private void Update()
        {
            // 1. Dọn dẹp quái đã chết hoặc bị Destroy
            CleanupDeadEnemies();

            // 2. Xử lý bộ đếm hồi sinh
            HandleRespawnTimers();
        }

        private void CleanupDeadEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = activeEnemies[i];
                if (enemy == null || (enemy.Stats != null && enemy.Stats.IsDead))
                {
                    activeEnemies.RemoveAt(i);
                    respawnTimers.Add(respawnDelay);
                }
            }
        }

        private void HandleRespawnTimers()
        {
            if (respawnTimers.Count == 0) return;

            while (activeEnemies.Count + respawnTimers.Count > maxMonsterCount && respawnTimers.Count > 0)
            {
                respawnTimers.RemoveAt(respawnTimers.Count - 1);
            }

            for (int i = respawnTimers.Count - 1; i >= 0; i--)
            {
                respawnTimers[i] -= Time.deltaTime;
                if (respawnTimers[i] <= 0f)
                {
                    respawnTimers.RemoveAt(i);
                    if (activeEnemies.Count < maxMonsterCount)
                    {
                        SpawnSingleEnemy();
                    }
                }
            }
        }

        /// <summary>
        /// Sinh 1 con quái vật tại vị trí ngẫu nhiên trong bán kính SpawnRadius
        /// </summary>
        public EnemyController SpawnSingleEnemy()
        {
            if (activeEnemies.Count >= maxMonsterCount) return null;

            Vector3 spawnPos = GetRandomSpawnPosition();
            EnemyController enemy = CreateEnemyInstance(npcTemplateId, monsterLevel, spawnPos, transform);

            if (enemy != null)
            {
                activeEnemies.Add(enemy);
            }

            return enemy;
        }

        /// <summary>
        /// Lập tức sinh đủ số lượng quái
        /// </summary>
        [ContextMenu("Spawn Now")]
        public void SpawnAllImmediate()
        {
            int need = maxMonsterCount - activeEnemies.Count;
            for (int i = 0; i < need; i++)
            {
                SpawnSingleEnemy();
            }
        }

        /// <summary>
        /// Xóa toàn bộ quái hiện có của điểm spawn này
        /// </summary>
        [ContextMenu("Clear All Spawned")]
        public void ClearAllSpawned()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    if (Application.isPlaying)
                        Destroy(activeEnemies[i].gameObject);
                    else
                        DestroyImmediate(activeEnemies[i].gameObject);
                }
            }
            activeEnemies.Clear();
            respawnTimers.Clear();
        }

        /// <summary>
        /// Lấy tọa độ ngẫu nhiên phẳng trong bán kính spawn, có raycast mặt đất
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 targetPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (snapToGround)
            {
                Vector3 rayStart = targetPos + Vector3.up * 20f;
                int enemyLayer = LayerMask.NameToLayer(CombatLayersAndTags.LayerEnemy);
                int mask = enemyLayer >= 0 ? ~(1 << enemyLayer) : ~0;

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, mask))
                {
                    targetPos.y = hit.point.y;
                }
            }

            return targetPos;
        }

        /// <summary>
        /// Factory Method: Tạo và cấu hình hoàn chỉnh một GameObject Enemy từ NpcTemplateData
        /// </summary>
        public static EnemyController CreateEnemyInstance(int templateId, int level, Vector3 position, Transform parent = null)
        {
            NpcTemplateDatabase.Instance.EnsureLoaded();
            NpcTemplateData template = NpcTemplateDatabase.GetTemplate(templateId);
            if (template == null)
            {
                Debug.LogWarning($"[EnemySpawnPoint] ⚠️ Không tìm thấy NpcTemplate ID: {templateId}");
                return null;
            }

            // 1. Root GameObject
            string goName = $"[NPC_{template.id}] {template.name} (Lv.{level})";
            GameObject enemyGO = new GameObject(goName);
            enemyGO.transform.position = position;
            if (parent != null)
            {
                enemyGO.transform.SetParent(parent);
            }

            // Layer & Tag
            int enemyLayer = LayerMask.NameToLayer(CombatLayersAndTags.LayerEnemy);
            if (enemyLayer == -1) enemyLayer = GameConstants.Layers.DefaultEnemyLayerIndex;

            // 2. Nạp Model Prefab con trước để Animation Components sẵn sàng
            if (!string.IsNullOrEmpty(template.prefab))
            {
                GameObject modelPrefab = NpcTemplateDatabase.LoadPrefab(template.prefab);
                if (modelPrefab != null)
                {
                    GameObject modelInst = Instantiate(modelPrefab, enemyGO.transform);
                    modelInst.name = modelPrefab.name;
                    modelInst.transform.localPosition = Vector3.zero;
                    modelInst.transform.localRotation = Quaternion.identity;
                }
            }

            // 3. CharacterController
            var resData = template.GetRes();
            float height = (resData != null && resData.height > 0f) ? resData.height : 1.8f;
            float radius = (resData != null && resData.width > 0f) ? resData.width : 0.5f;

            CharacterController cc = enemyGO.AddComponent<CharacterController>();
            cc.height = height;
            cc.radius = radius;
            cc.center = new Vector3(0f, height * 0.5f, 0f);

            // 4. EnemyStats
            EnemyStats stats = enemyGO.AddComponent<EnemyStats>();
            stats.MonsterLevel = level;

            // 5. EnemyPerception & Brain
            EnemyPerception perception = enemyGO.AddComponent<EnemyPerception>();
            perception.SpawnPosition = position;
            perception.SetRanges(template.visionRadius, template.activeRadius, 3f);

            EnemyBrain brain = enemyGO.AddComponent<EnemyBrain>();

            // 6. EnemyController & Animation Controller
            EnemyController enemyCtrl = enemyGO.AddComponent<EnemyController>();
            enemyCtrl.Initialize(templateId, level, position);

            // 7. Gán Tag & Layer đệ quy
            SetTagAndLayerRecursively(enemyGO, CombatLayersAndTags.TagEnemy, enemyLayer);

            return enemyCtrl;
        }

        private static void SetTagAndLayerRecursively(GameObject obj, string tag, int layer)
        {
            if (obj == null) return;
            try { if (!string.IsNullOrEmpty(tag)) obj.tag = tag; } catch (Exception) { }
            if (layer >= 0) obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetTagAndLayerRecursively(child.gameObject, tag, layer);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.9f);
            Gizmos.DrawSphere(transform.position, 0.35f);

            if (Application.isPlaying && activeEnemies != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    if (activeEnemies[i] != null)
                    {
                        Gizmos.DrawLine(transform.position, activeEnemies[i].transform.position);
                    }
                }
            }

#if UNITY_EDITOR
            NpcTemplateDatabase.Instance.EnsureLoaded();
            var template = NpcTemplateDatabase.GetTemplate(npcTemplateId);
            string templateName = template != null ? template.name : $"ID:{npcTemplateId}";
            string info = $"[SPAWN POINT]\n{templateName} (Lv.{monsterLevel})\nSố lượng: {(Application.isPlaying ? activeEnemies.Count : 0)}/{maxMonsterCount}\nTầm: {spawnRadius:F1}m";
            
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.yellow;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;

            Handles.Label(transform.position + Vector3.up * 1.5f, info, labelStyle);
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
