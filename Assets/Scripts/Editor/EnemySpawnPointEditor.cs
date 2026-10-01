#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TopDownGame.Enemy;
using TopDownGame.NPC;

namespace TopDownGame.Editor
{
    [CustomEditor(typeof(EnemySpawnPoint))]
    public class EnemySpawnPointEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EnemySpawnPoint spawner = (EnemySpawnPoint)target;

            EditorGUILayout.LabelField("👾 Điểm Sinh Quái Vật (Spawn Point)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Quản lý sinh quái và hồi sinh tự động dựa trên NpcTemplate.csv.", MessageType.Info);
            EditorGUILayout.Space(5);

            SerializedProperty npcIdProp = serializedObject.FindProperty("npcTemplateId");
            SerializedProperty levelProp = serializedObject.FindProperty("monsterLevel");
            SerializedProperty maxCountProp = serializedObject.FindProperty("maxMonsterCount");
            SerializedProperty radiusProp = serializedObject.FindProperty("spawnRadius");
            SerializedProperty respawnProp = serializedObject.FindProperty("respawnDelay");
            SerializedProperty intervalProp = serializedObject.FindProperty("spawnInterval");
            SerializedProperty autoSpawnProp = serializedObject.FindProperty("autoSpawnOnStart");
            SerializedProperty snapGroundProp = serializedObject.FindProperty("snapToGround");
            SerializedProperty showGizmosProp = serializedObject.FindProperty("showGizmos");
            SerializedProperty gizmoColorProp = serializedObject.FindProperty("gizmoColor");

            EditorGUILayout.PropertyField(npcIdProp, new GUIContent("NPC Template ID:"));
            EditorGUILayout.PropertyField(levelProp, new GUIContent("Monster Level:"));

            // Hiển thị thông tin quái từ Database
            NpcTemplateDatabase.Instance.EnsureLoaded();
            var template = NpcTemplateDatabase.GetTemplate(npcIdProp.intValue);
            if (template != null)
            {
                var attrib = template.GetAttribute();
                int lv = Mathf.Max(1, levelProp.intValue);
                string hpText = attrib != null ? $"{attrib.GetMaxLife(lv):F0}" : "100";
                string atkText = attrib != null ? $"{attrib.GetAverageAttack(lv):F0}" : "20";

                EditorGUILayout.HelpBox(
                    $"• Tên: {template.name}\n" +
                    $"• Cấp độ: Lv.{lv} | Máu: {hpText} | Công: {atkText}\n" +
                    $"• Tốc độ: {template.runSpeed:F1} m/s | Prefab: {template.prefab}",
                    MessageType.None
                );
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ Không tìm thấy Template ID này trong NpcTemplate.csv!", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("⚙️ Cấu Hình Bãi Quái", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxCountProp, new GUIContent("Số Lượng Quái:"));
            EditorGUILayout.PropertyField(radiusProp, new GUIContent("Bán Kính Spawn (m):"));
            EditorGUILayout.PropertyField(respawnProp, new GUIContent("Thời Gian Hồi Sinh (s):"));
            EditorGUILayout.PropertyField(intervalProp, new GUIContent("Khoảng Cách Spawn (s):"));
            EditorGUILayout.PropertyField(autoSpawnProp, new GUIContent("Tự Sinh Khi Bắt Đầu:"));
            EditorGUILayout.PropertyField(snapGroundProp, new GUIContent("Dò Mặt Đất (Snap):"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎨 Hiển Thị Gizmos", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showGizmosProp, new GUIContent("Bật Gizmos:"));
            EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Màu Vòng Tròn:"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🚀 Sinh Quái Ngay (Spawn)", GUILayout.Height(30)))
            {
                spawner.SpawnAllImmediate();
            }
            if (GUILayout.Button("🗑️ Xóa Toàn Bộ Quái (Clear)", GUILayout.Height(30)))
            {
                spawner.ClearAllSpawned();
            }
            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("GameObject/TopDown RPG/Enemy Spawn Point", false, 10)]
        public static void CreateSpawnPointGameObject(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("EnemySpawnPoint");
            var spawner = go.AddComponent<EnemySpawnPoint>();
            spawner.NpcTemplateId = 72; // Mặc định
            spawner.MonsterLevel = 1;
            spawner.MaxMonsterCount = 3;
            spawner.SpawnRadius = 6.0f;

            if (SceneView.lastActiveSceneView != null)
            {
                Vector3 spawnPos = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
                spawnPos.y = 0f;
                go.transform.position = spawnPos;
            }

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Enemy Spawn Point");
            Selection.activeObject = go;
        }
    }
}
#endif
