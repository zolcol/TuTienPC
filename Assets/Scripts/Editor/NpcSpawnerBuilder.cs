#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using TopDownGame.Enemy;
using TopDownGame.NPC;

namespace TopDownGame.Editor
{
    public class NpcSpawnerBuilder : EditorWindow
    {
        private int selectedNpcId = 72; // Hắc Ám Thỏ hoặc Sói
        private string searchFilter = "";
        private Vector2 scrollPos;

        [MenuItem("Tools/TopDownGame/Spawn NPC from Template", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<NpcSpawnerBuilder>("NPC Spawner");
            window.minSize = new Vector2(480, 520);
        }

        private void OnGUI()
        {
            GUILayout.Label("👾 Tạo Quái / NPC từ Database Chuẩn (Settings/GameData)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Công cụ tự động nạp Template từ Settings/GameData/NPC/NpcTemplate.csv, liên kết NpcRes (Model, Kích thước) và NpcAttribute (Máu, Công Level 1).", MessageType.Info);

            EditorGUILayout.Space(10);
            selectedNpcId = EditorGUILayout.IntField("Nhập NPC ID:", selectedNpcId);

            NpcTemplateDatabase.Instance.EnsureLoaded();
            var template = NpcTemplateDatabase.GetTemplate(selectedNpcId);

            if (template != null)
            {
                var attrib = template.GetAttribute();
                var res = template.GetRes();

                string statsInfo = attrib != null 
                    ? $"Máu: {attrib.maxLife:F0} | Công: {attrib.AverageAttack:F0} | Tốc độ: {template.runSpeed:F1}m/s | Tầm nhìn: {template.visionRadius:F1}m | Leash: {template.activeRadius:F1}m" 
                    : $"Mặc định (100 HP / 20 ATK) | Tầm nhìn: {template.visionRadius:F1}m | Leash: {template.activeRadius:F1}m";
                string sizeInfo = res != null 
                    ? $"Cao: {res.height:F1}m | Rộng: {res.width:F1}m" 
                    : "1.8m x 0.5m";

                EditorGUILayout.HelpBox(
                    $"• Tên: {template.name}\n" +
                    $"• Loại: {template.kind} | Phe (Camp): {template.camp}\n" +
                    $"• Chỉ số (Level 1): {statsInfo}\n" +
                    $"• Kích thước: {sizeInfo}\n" +
                    $"• Skills: [{string.Join(", ", template.GetSkillList())}]\n" +
                    $"• Model: {template.prefab}", 
                    MessageType.None
                );
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ Không tìm thấy ID này trong NpcTemplate.csv!", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🚀 TẠO 1 NPC ĐƠN LẺ", GUILayout.Height(35)))
            {
                SpawnNpcInScene(selectedNpcId);
            }
            if (GUILayout.Button("🎯 TẠO BÃI QUÁI (SPAWN POINT)", GUILayout.Height(35)))
            {
                CreateSpawnPointInScene(selectedNpcId);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);
            GUILayout.Label("Danh sách mẫu trong Database:", EditorStyles.boldLabel);
            searchFilter = EditorGUILayout.TextField("🔍 Tìm kiếm tên / ID:", searchFilter);

            var all = NpcTemplateDatabase.GetAllTemplates();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(220));
            int count = 0;
            foreach (var kvp in all)
            {
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    bool matchId = kvp.Key.ToString().Contains(searchFilter);
                    bool matchName = kvp.Value.name.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!matchId && !matchName) continue;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"#{kvp.Key}: {kvp.Value.name} ({kvp.Value.kind})", GUILayout.Width(280));
                if (GUILayout.Button("Chọn", GUILayout.Width(50)))
                {
                    selectedNpcId = kvp.Key;
                    GUI.FocusControl(null);
                }
                if (GUILayout.Button("Spawn", GUILayout.Width(60)))
                {
                    SpawnNpcInScene(kvp.Key);
                }
                EditorGUILayout.EndHorizontal();

                count++;
                if (count >= 100)
                {
                    GUILayout.Label("... Còn nhiều kết quả nữa, vui lòng gõ tìm kiếm cụ thể ...");
                    break;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public static GameObject SpawnNpcInScene(int npcId)
        {
            NpcTemplateDatabase.Instance.EnsureLoaded();
            var template = NpcTemplateDatabase.GetTemplate(npcId);
            if (template == null)
            {
                EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy NPC ID {npcId} trong Database!", "OK");
                return null;
            }

            // 1. Tạo GameObject Cha (Root Container)
            string rootName = $"[NPC_{template.id}] {template.name}";
            GameObject rootGO = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(rootGO, "Create NPC from Template");

            // Xác định Layer Enemy (Layer 6)
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer == -1) enemyLayer = 6;

            // Đặt vị trí trước Scene View Camera nếu có
            if (SceneView.lastActiveSceneView != null)
            {
                Vector3 spawnPos = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 4f;
                spawnPos.y = 0f;
                rootGO.transform.position = spawnPos;
            }

            // 2. Thêm CharacterController với kích thước từ NpcRes
            var resData = template.GetRes();
            float height = (resData != null && resData.height > 0f) ? resData.height : 1.8f;
            float radius = (resData != null && resData.width > 0f) ? resData.width : 0.5f;

            CharacterController cc = rootGO.AddComponent<CharacterController>();
            cc.height = height;
            cc.radius = radius;
            cc.center = new Vector3(0f, height * 0.5f, 0f);

            // 3. Thêm EnemyStats & EnemyController
            EnemyStats stats = rootGO.AddComponent<EnemyStats>();
            var attrib = template.GetAttribute();
            if (attrib != null)
            {
                stats.Health.SetMaxValue(attrib.maxLife, true);
                stats.SetPhysicalDamage(attrib.AverageAttack);
            }

            EnemyController enemyCtrl = rootGO.AddComponent<EnemyController>();

            // Gán Template ID và Tốc độ di chuyển qua SerializedObject
            SerializedObject ctrlSO = new SerializedObject(enemyCtrl);
            ctrlSO.FindProperty("npcTemplateId").intValue = template.id;

            float defaultSpeed = (template.runSpeed > 0f) ? template.runSpeed : 5.0f;
            var moveSpeedProp = ctrlSO.FindProperty("moveSpeed");
            if (moveSpeedProp != null)
            {
                moveSpeedProp.floatValue = defaultSpeed;
            }
            ctrlSO.ApplyModifiedProperties();

            // 4. Nạp Model Prefab từ bảng làm GameObject Con
            if (!string.IsNullOrEmpty(template.prefab))
            {
                GameObject modelPrefab = NpcTemplateDatabase.LoadPrefab(template.prefab);
                if (modelPrefab != null)
                {
                    GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, rootGO.transform);
                    modelInstance.name = modelPrefab.name;
                    modelInstance.transform.localPosition = Vector3.zero;
                    modelInstance.transform.localRotation = Quaternion.identity;
                    Undo.RegisterCreatedObjectUndo(modelInstance, "Create NPC Model Child");
                }
                else
                {
                    Debug.LogWarning($"[NpcSpawner] ⚠️ Không tìm thấy prefab model tại: {template.prefab}");
                }
            }

            // 5. Gán Tag và Layer "Enemy" đệ quy cho cả Root và toàn bộ Model/Xương/Colliders con
            SetTagAndLayerRecursively(rootGO, "Enemy", enemyLayer);

            enemyCtrl.ApplyTemplateData();
            enemyCtrl.EnsureAnimationController();

            EditorUtility.SetDirty(enemyCtrl);
            EditorUtility.SetDirty(rootGO);

            Selection.activeGameObject = rootGO;
            EditorGUIUtility.PingObject(rootGO);

            Debug.Log($"✅ Đã tạo thành công NPC: <b>{rootName}</b> với model <i>{template.prefab}</i> | Máu: {(attrib != null ? attrib.maxLife : 100)} | Tốc độ: {defaultSpeed:F1} m/s | Layer: Enemy ({enemyLayer}) | Tag: Enemy!");
            return rootGO;
        }

        public static GameObject CreateSpawnPointInScene(int npcId, int level = 1, int maxCount = 3, float radius = 6.0f)
        {
            NpcTemplateDatabase.Instance.EnsureLoaded();
            var template = NpcTemplateDatabase.GetTemplate(npcId);
            string name = template != null ? template.name : $"NPC_{npcId}";

            GameObject spGO = new GameObject($"SpawnPoint_{name}");
            Undo.RegisterCreatedObjectUndo(spGO, "Create Spawn Point");

            if (SceneView.lastActiveSceneView != null)
            {
                Vector3 spawnPos = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
                spawnPos.y = 0f;
                spGO.transform.position = spawnPos;
            }

            var spawner = spGO.AddComponent<EnemySpawnPoint>();
            spawner.NpcTemplateId = npcId;
            spawner.MonsterLevel = level;
            spawner.MaxMonsterCount = maxCount;
            spawner.SpawnRadius = radius;

            EditorUtility.SetDirty(spGO);
            Selection.activeGameObject = spGO;
            EditorGUIUtility.PingObject(spGO);

            Debug.Log($"🎯 Đã tạo thành công Bãi Quái: <b>{spGO.name}</b> (Template #{npcId}, Lv.{level}, Max: {maxCount}, Radius: {radius}m)!");
            return spGO;
        }

        private static void SetTagAndLayerRecursively(GameObject obj, string tag, int layer)
        {
            if (obj == null) return;
            try
            {
                if (!string.IsNullOrEmpty(tag)) obj.tag = tag;
            }
            catch (Exception) { }

            if (layer >= 0) obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetTagAndLayerRecursively(child.gameObject, tag, layer);
            }
        }
    }
}
#endif
