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
        [Tooltip("Bán kính phát hiện người chơi (quái bắt đầu rượt đuổi)")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("Khoảng cách tiếp cận để tung đòn đánh")]
        [SerializeField] private float attackRange = 3f;

        [Header("=== MOVEMENT SETTINGS ===")]
        [SerializeField] private float moveSpeed = 3.5f;
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
        public float AttackRange => attackRange;
        public float MoveSpeed => moveSpeed;
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

            // 3. Tầm nhìn & Tốc độ di chuyển
            moveSpeed = template.runSpeed > 0f ? template.runSpeed : 3.5f;
            detectionRange = template.visionRadius > 0f ? template.visionRadius : 10f;

            // 4. Nạp danh sách kỹ năng từ bảng
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

            EnsureAnimationController();
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
            if (!characterController.enabled) return;

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

            var keys = new List<int>(cooldownTimers.Keys);
            foreach (var id in keys)
            {
                if (cooldownTimers[id] > 0f)
                {
                    cooldownTimers[id] -= Time.deltaTime;
                    if (cooldownTimers[id] <= 0f)
                    {
                        cooldownTimers[id] = 0f;
                    }
                }
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

        public SkillData GetReadyAttack()
        {
            if (attackSkillIds == null || attackSkillIds.Count == 0) return null;

            List<SkillData> readyAttacks = new List<SkillData>();
            foreach (var id in attackSkillIds)
            {
                SkillData skill = SkillDatabase.GetSkill(id);
                if (skill != null && !IsOnCooldown(skill.id))
                {
                    readyAttacks.Add(skill);
                }
            }

            if (readyAttacks.Count == 0) return null;

            int randomIndex = Random.Range(0, readyAttacks.Count);
            return readyAttacks[randomIndex];
        }

        public void TryFindTarget()
        {
            if (target != null) return;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        public float GetDistanceToTarget()
        {
            if (target == null) return float.MaxValue;
            return Vector3.Distance(transform.position, target.position);
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

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

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
