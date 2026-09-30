using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.NPC;
using TopDownGame.Skills;
using TopDownGame.StateMachine;

namespace TopDownGame.Enemy
{
    [RequireComponent(typeof(CharacterController))]
    public class EnemyController : MonoBehaviour
    {
        [Header("=== NPC TEMPLATE (Bảng NpcTemplate.csv) ===")]
        [Tooltip("ID của NPC trong bảng NpcTemplate.csv (vd: 101, 102). Nếu > 0 sẽ nạp Skill và Model tương ứng")]
        [SerializeField] private int npcTemplateId = 101;

        [Header("=== AI PERCEPTION (Tầm nhìn & Tầm đánh) ===")]
        [Tooltip("Mục tiêu quái vật rượt đuổi (để trống sẽ tự động tìm Player theo Tag)")]
        [SerializeField] private Transform target;
        [Tooltip("Bán kính phát hiện người chơi / VisionRadius (quái bắt đầu rượt đuổi)")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("Tầm truy đuổi tối đa / ActiveRadius / Leash Range trước khi từ bỏ")]
        [SerializeField] private float activeRadius = 15f;
        [Tooltip("Khoảng cách tiếp cận để tung đòn đánh / AttackRadius")]
        [SerializeField] private float attackRange = 3f;

        [Header("=== MOVEMENT SETTINGS ===")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [SerializeField] private float gravity = -9.81f;

        [Header("=== COMBAT & SKILLS ===")]
        [Tooltip("Layer nhận sát thương (vd: Player / Default)")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Tooltip("Danh sách các ID kỹ năng của quái trong Skills.csv (vd: 101 cho NPC 012, 102 cho NPC 015)")]
        [SerializeField] private List<int> attackSkillIds = new List<int>() { 101 };

        [Header("=== DEBUG & GIZMOS (Tùy chọn) ===")]
        [Tooltip("Hiển thị vòng tròn tầm nhìn và tầm đánh trong Scene View")]
        [SerializeField] private bool showRangeGizmos = true;
        [Tooltip("Hiển thị vệt tia sát thương khi quái tung chiêu")]
        [SerializeField] private bool showHitGizmos = true;
        [SerializeField] private float gizmoDisplayDuration = 0.25f;
        [Tooltip("Trạng thái hiện tại của State Machine (Chỉ xem)")]
        [SerializeField] private string currentStateDisplay;

        // References
        private CharacterController characterController;
        private LegacyAnimationController animationController;
        private EnemyStats stats;

        // State Machine
        public TopDownGame.StateMachine.StateMachine StateMachine { get; private set; }
        public EnemyIdleState IdleState { get; private set; }
        public EnemyChaseState ChaseState { get; private set; }
        public EnemyAttackState AttackState { get; private set; }
        public EnemyDeadState DeadState { get; private set; }

        // Getters
        public Transform Target => target;
        public float DetectionRange => detectionRange;
        public float ActiveRadius => activeRadius;
        public float AttackRange => attackRange;
        public float MoveSpeed => moveSpeed;
        public float WalkSpeed => walkSpeed;
        public Vector3 SpawnPosition { get; private set; }
        public float DistanceFromSpawn => Vector3.Distance(transform.position, SpawnPosition);
        public CharacterController CharacterController => characterController;
        public LegacyAnimationController AnimationController => animationController;
        public EnemyStats Stats => stats;
        
        public int NpcResId 
        {
            get 
            {
                if (npcTemplateId > 0)
                {
                    var template = TopDownGame.NPC.NpcTemplateDatabase.GetTemplate(npcTemplateId);
                    if (template != null) return template.npcResId;
                }
                return 0;
            }
        }

        private Vector3 verticalVelocity;
        private Vector3 moveDirection;
        private float turnSmoothVelocity;
        private bool isGrounded;
        private readonly Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();
        private readonly List<int> cooldownKeysBuffer = new List<int>(8);
        private readonly List<SkillData> readyAttacksBuffer = new List<SkillData>(4);

        // Debug Gizmo
        private struct GizmoDrawInfo
        {
            public SkillType type;
            public Vector3 origin;
            public Vector3 forward;
            public float range;
            public float fanAngle;
            public float boxWidth;
        }
        private GizmoDrawInfo lastGizmo;
        private float gizmoTimer;

        private void Awake()
        {
            SpawnPosition = transform.position;
            characterController = GetComponent<CharacterController>();
            stats = GetComponent<EnemyStats>() ?? gameObject.AddComponent<EnemyStats>();

            NpcTemplateDatabase.Instance.EnsureLoaded();
            SkillDatabase.Instance.EnsureLoaded();

            ApplyTemplateData();

            EnsureAnimationController();

            // Khởi tạo State Machine
            StateMachine = new TopDownGame.StateMachine.StateMachine();
            IdleState = new EnemyIdleState(this, StateMachine);
            ChaseState = new EnemyChaseState(this, StateMachine);
            AttackState = new EnemyAttackState(this, StateMachine);
            DeadState = new EnemyDeadState(this, StateMachine);
        }

        public void EnsureAnimationController()
        {
            if (animationController == null)
            {
                animationController = GetComponentInChildren<LegacyAnimationController>();
            }

            if (animationController == null)
            {
                // Tìm Animation component có sẵn trên model con
                Animation anim = GetComponentInChildren<Animation>();
                if (anim != null)
                {
                    animationController = anim.gameObject.AddComponent<LegacyAnimationController>();
                }
                else
                {
                    animationController = gameObject.AddComponent<LegacyAnimationController>();
                }
            }

            if (animationController != null)
            {
                animationController.AutoFindAnimationComponents();
            }
        }

        [ContextMenu("Áp dụng NpcTemplate (Nạp Model & Skills)")]
        public void ApplyTemplateData()
        {
            if (npcTemplateId <= 0) return;

            NpcTemplateDatabase.Instance.EnsureLoaded();
            NpcTemplateData template = NpcTemplateDatabase.GetTemplate(npcTemplateId);
            if (template == null) return;

            // 1. Áp dụng chỉ số Máu & Tấn công từ NpcAttribute (Level 1)
            var attrib = template.GetAttribute();
            if (attrib != null && stats != null)
            {
                stats.Health.SetMaxValue(attrib.maxLife, true);
                stats.SetPhysicalDamage(attrib.AverageAttack);
                float totalMagic = attrib.woodDamage + attrib.waterDamage + attrib.fireDamage + attrib.earthDamage + attrib.metalDamage;
                stats.SetMagicDamage(totalMagic);
            }
            else if (stats != null)
            {
                stats.Health.SetMaxValue(100f, true);
                stats.SetPhysicalDamage(20f);
            }

            // 2. Áp dụng Kích thước va chạm CharacterController từ NpcRes
            var resData = template.GetRes();
            if (resData != null && characterController != null)
            {
                characterController.height = Mathf.Max(1.0f, resData.height);
                characterController.radius = Mathf.Max(0.2f, resData.width);
                characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);
            }

            // 3. Tầm nhìn (VisionRadius), Tầm hoạt động (ActiveRadius / Leash) & Tốc độ di chuyển đọc trực tiếp từ Database
            moveSpeed = template.runSpeed > 0f ? template.runSpeed : 5.0f;
            walkSpeed = template.walkSpeed > 0f ? template.walkSpeed : (moveSpeed * 0.5f);
            detectionRange = template.visionRadius;
            activeRadius = template.activeRadius;

            // 4. Nạp danh sách kỹ năng từ bảng và cập nhật AttackRadius
            var skillsFromTable = template.GetSkillList();
            if (skillsFromTable.Count > 0)
            {
                attackSkillIds = skillsFromTable;
                var firstSkill = SkillDatabase.GetSkill(attackSkillIds[0]);
                if (firstSkill != null && firstSkill.range > 0f)
                {
                    attackRange = firstSkill.range;
                }
            }

            // 5. Nếu chưa có model con, nạp prefab từ bảng làm GameObject con
            if (!string.IsNullOrEmpty(template.prefab))
            {
                bool hasChildModel = false;
                foreach (Transform child in transform)
                {
                    if (child.GetComponentInChildren<LegacyAnimationController>() != null || child.GetComponentInChildren<Renderer>() != null)
                    {
                        hasChildModel = true;
                        break;
                    }
                }

                if (!hasChildModel)
                {
                    GameObject modelPrefab = NpcTemplateDatabase.LoadPrefab(template.prefab);
                    if (modelPrefab != null)
                    {
                        GameObject modelInstance = Instantiate(modelPrefab, transform);
                        modelInstance.name = modelPrefab.name;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;

                        // Đảm bảo model con có LegacyAnimationController
                        animationController = modelInstance.GetComponentInChildren<LegacyAnimationController>();
                        if (animationController == null)
                        {
                            Animation anim = modelInstance.GetComponentInChildren<Animation>();
                            if (anim != null)
                            {
                                animationController = anim.gameObject.AddComponent<LegacyAnimationController>();
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemyController] ⚠️ Không tìm thấy prefab model tại: {template.prefab}");
                    }
                }
            }

            // 6. Gán chuẩn Tag và Layer "Enemy" cho Root và toàn bộ GameObject con
            int enemyLayer = LayerMask.NameToLayer(CombatLayersAndTags.LayerEnemy);
            if (enemyLayer != -1)
            {
                SetLayerRecursively(gameObject, enemyLayer);
            }
            SetTagRecursively(gameObject, CombatLayersAndTags.TagEnemy);

            EnsureAnimationController();
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null || layer < 0) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void SetTagRecursively(GameObject obj, string tag)
        {
            if (obj == null || string.IsNullOrEmpty(tag)) return;
            try
            {
                obj.tag = tag;
            }
            catch (System.Exception) { }

            foreach (Transform child in obj.transform)
            {
                SetTagRecursively(child.gameObject, tag);
            }
        }

        private void Start()
        {
            EnsureAnimationController();
            TryFindTarget();
            stats.OnDeath += HandleDeath;
            StateMachine.Initialize(IdleState);
        }

        private void OnDestroy()
        {
            if (stats != null)
            {
                stats.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            UpdateCooldowns();
            StateMachine.Update();
            HandleMovementAndGravity();

            if (StateMachine.CurrentState != null)
            {
                currentStateDisplay = StateMachine.CurrentState.GetType().Name;
            }

            if (gizmoTimer > 0f)
            {
                gizmoTimer -= Time.deltaTime;
            }
        }

        private void FixedUpdate()
        {
            StateMachine.PhysicsUpdate();
        }

        private void HandleMovementAndGravity()
        {
            if (characterController == null || !characterController.enabled || !characterController.gameObject.activeInHierarchy) return;

            isGrounded = characterController.isGrounded;

            if (isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }

            Vector3 finalMove = (moveDirection * moveSpeed + verticalVelocity) * Time.deltaTime;
            characterController.Move(finalMove);
            moveDirection = Vector3.zero;
        }

        public void MoveTowardsTarget()
        {
            if (target == null || !characterController.enabled) return;

            Vector3 direction = (target.position - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                moveDirection = direction.normalized;
            }
        }

        private void HandleDeath()
        {
            StateMachine.ChangeState(DeadState);
        }

        private void UpdateCooldowns()
        {
            if (cooldownTimers.Count == 0) return;

            cooldownKeysBuffer.Clear();
            foreach (var kvp in cooldownTimers)
            {
                if (kvp.Value > 0f)
                {
                    cooldownKeysBuffer.Add(kvp.Key);
                }
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < cooldownKeysBuffer.Count; i++)
            {
                int id = cooldownKeysBuffer[i];
                float rem = cooldownTimers[id] - dt;
                cooldownTimers[id] = rem > 0f ? rem : 0f;
            }
        }

        public void StartCooldown(int skillId, float duration)
        {
            if (skillId <= 0 || duration <= 0f) return;
            cooldownTimers[skillId] = duration;
        }

        public bool IsOnCooldown(int skillId)
        {
            return skillId > 0 && cooldownTimers.TryGetValue(skillId, out float timeRemaining) && timeRemaining > 0f;
        }

        public SkillData GetReadyAttack(float distanceToTarget = -1f)
        {
            if (attackSkillIds == null || attackSkillIds.Count == 0) return null;

            readyAttacksBuffer.Clear();
            for (int i = 0; i < attackSkillIds.Count; i++)
            {
                int id = attackSkillIds[i];
                SkillData skill = SkillDatabase.GetSkill(id);
                if (skill != null && !IsOnCooldown(skill.id))
                {
                    // Buffer nhỏ 0.35m để dung hòa bán kính Collider va chạm giữa Enemy và Player
                    float effectiveSkillRange = (skill.range > 0f ? skill.range : 2.0f) + 0.35f;
                    if (distanceToTarget < 0f || distanceToTarget <= effectiveSkillRange)
                    {
                        readyAttacksBuffer.Add(skill);
                    }
                }
            }

            if (readyAttacksBuffer.Count == 0) return null;

            int randomIndex = Random.Range(0, readyAttacksBuffer.Count);
            return readyAttacksBuffer[randomIndex];
        }

        public float GetMinAttackRange()
        {
            float minR = float.MaxValue;
            if (attackSkillIds != null && attackSkillIds.Count > 0)
            {
                for (int i = 0; i < attackSkillIds.Count; i++)
                {
                    var sk = SkillDatabase.GetSkill(attackSkillIds[i]);
                    if (sk != null && sk.range > 0f && sk.range < minR)
                    {
                        minR = sk.range;
                    }
                }
            }
            return minR < float.MaxValue ? (minR + 0.35f) : (attackRange + 0.35f);
        }

        public void TryFindTarget()
        {
            if (target != null) return;

            GameObject playerObj = GameObject.FindGameObjectWithTag(CombatLayersAndTags.TagPlayer);
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        /// <summary>
        /// Khoảng cách phẳng trên mặt phẳng OXZ đến mục tiêu (tránh sai lệch trục Y do chiều cao model)
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (target == null) return float.MaxValue;
            Vector3 diff = target.position - transform.position;
            diff.y = 0f;
            return diff.magnitude;
        }

        /// <summary>
        /// Khoảng cách phẳng trên mặt phẳng OXZ đến điểm xuất phát
        /// </summary>
        public float GetDistanceToSpawn()
        {
            Vector3 diff = SpawnPosition - transform.position;
            diff.y = 0f;
            return diff.magnitude;
        }

        public void MoveTowardsSpawn()
        {
            if (characterController == null || !characterController.enabled) return;

            Vector3 direction = (SpawnPosition - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                float speedRatio = moveSpeed > 0f ? (walkSpeed / moveSpeed) : 0.5f;
                moveDirection = direction.normalized * speedRatio;
            }
        }

        public void RotateTowardsSpawn()
        {
            Vector3 direction = (SpawnPosition - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }
        }

        public void RotateTowardsTarget()
        {
            if (target == null) return;

            Vector3 direction = (target.position - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }
        }

        /// <summary>
        /// Kích hoạt quét sát thương qua SkillDamageResolver tập trung
        /// </summary>
        public void ExecuteSkillDamage(SkillData skill)
        {
            if (skill == null) return;

            SkillDamageResolver.CastDamage(transform, stats, skill, targetLayer, target);

            if (showHitGizmos)
            {
                lastGizmo = new GizmoDrawInfo
                {
                    type = skill.skillType,
                    origin = transform.position,
                    forward = transform.forward,
                    range = skill.range,
                    fanAngle = skill.fanAngle,
                    boxWidth = skill.boxWidth
                };
                gizmoTimer = gizmoDisplayDuration;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showRangeGizmos) return;

            // VisionRadius (Tầm nhìn phát hiện người chơi)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // ActiveRadius (Leash Range - Tầm hoạt động tối đa từ điểm spawn)
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.75f);
            Vector3 spawnCenter = Application.isPlaying ? SpawnPosition : transform.position;
            Gizmos.DrawWireSphere(spawnCenter, activeRadius);

            // AttackRadius (Tầm tấn công của chiêu thức)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private void OnDrawGizmos()
        {
            if (!showHitGizmos || gizmoTimer <= 0f) return;
            SkillDamageResolver.DrawGizmo(lastGizmo.type, lastGizmo.origin, lastGizmo.forward, lastGizmo.range, lastGizmo.fanAngle, lastGizmo.boxWidth);
        }
    }
}
