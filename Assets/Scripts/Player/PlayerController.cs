using System;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Input;
using TopDownGame.Skills;
using TopDownGame.StateMachine;
using TopDownGame.Stats;

namespace TopDownGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [Header("=== MOVEMENT SETTINGS ===")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSmoothTime = 0.08f;
        [Tooltip("Độ mượt/gia tốc khi xoay người đổi hướng lúc đang đánh (0.12 - 0.18s giúp xoay đầm tay, có quán tính, không giật ngoắt)")]
        [SerializeField] private float attackRotationSmoothTime = 0.14f;
        [SerializeField] private float gravity = -9.81f;

        [Header("=== COMBAT & TARGETING ===")]
        [Tooltip("Layer nhận sát thương của mục tiêu (vd: Enemy hoặc Default)")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Tooltip("ID của đòn đánh thường đầu tiên trong Skills.csv (vd: 301)")]
        [SerializeField] private int defaultNormalAttackId = 301;

        [Header("=== FACTION & SKILL SLOTS (Q - E - R) ===")]
        [Tooltip("Môn phái của nhân vật (1: Thiên Vương, 2: Nga Mi, 3: Đào Hoa, 4: Tiêu Dao, ...)")]
        [SerializeField] private int factionId = 2;

        [Tooltip("ID của Model trong NpcRes.csv (vd: 1 cho Thiên Vương Nam, 2 cho Nga Mi, 3 cho Đào Hoa)")]
        [SerializeField] private int npcResId = 2;

        [Tooltip("Kỹ năng 1: Phím Q (vd: 306 - Nga Mi Skill 1 / Tụ Hàng Phổ Độ)")]
        [SerializeField] private int skillSlotQ_Id = 306;

        [Tooltip("Kỹ năng 2: Phím E (vd: 308 - Nga Mi Skill 2 / Bạch Lộ Ngưng Sương)")]
        [SerializeField] private int skillSlotE_Id = 308;

        [Tooltip("Kỹ năng 3: Phím R (vd: 346 - Nga Mi Nộ / Băng Phách Hồng Liên Kiếp)")]
        [SerializeField] private int skillSlotR_Id = 346;

        [Header("=== CHEAT & TESTING ===")]
        [Tooltip("Bỏ qua thời gian hồi chiêu (No Cooldown / NoCD) khi test")]
        [SerializeField] private bool noCooldown = false;
        [Tooltip("Bỏ qua tiêu hao Mana / Nội lực khi test")]
        [SerializeField] private bool noManaCost = false;

        [Header("=== DEBUG & GIZMOS (Tùy chọn) ===")]
        [Tooltip("Hiển thị vùng quét tia / quạt / vòng tròn trong Scene View khi tung đòn")]
        [SerializeField] private bool showHitGizmos = true;
        [Tooltip("Thời gian lưu vệt Gizmo (giây)")]
        [SerializeField] private float gizmoDisplayDuration = 0.25f;
        [Tooltip("Trạng thái hiện tại của Player (Chỉ xem)")]
        [SerializeField] private string currentStateDisplay;
        [Tooltip("Đang dùng tay cầm Gamepad (Chỉ xem)")]
        [SerializeField] private bool usingGamepadDisplay;

        // State Machine
        public TopDownGame.StateMachine.StateMachine StateMachine { get; private set; }
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }

        // Internal References
        private PlayerInputReader inputReader;
        private LegacyAnimationController animationController;
        private PlayerStats stats;
        private UnityEngine.Camera mainCamera;

        // Components & Getters
        public CharacterController CharacterController { get; private set; }
        public PlayerInputReader InputReader => inputReader;
        public LegacyAnimationController AnimationController => animationController;
        public PlayerStats Stats => stats;
        public float MoveSpeed => moveSpeed;
        public float AttackRotationSmoothTime => attackRotationSmoothTime;

        // Skills Getters from Database
        public SkillData DefaultNormalAttack => SkillDatabase.GetSkill(defaultNormalAttackId);
        public SkillData SkillSlotQ => SkillDatabase.GetSkill(skillSlotQ_Id);
        public SkillData SkillSlotE => SkillDatabase.GetSkill(skillSlotE_Id);
        public SkillData SkillSlotR => SkillDatabase.GetSkill(skillSlotR_Id);

        public int SkillSlotQ_Id => skillSlotQ_Id;
        public int SkillSlotE_Id => skillSlotE_Id;
        public int SkillSlotR_Id => skillSlotR_Id;
        public int NpcResId => npcResId;

        public bool NoCooldown { get => noCooldown; set => noCooldown = value; }
        public bool NoManaCost { get => noManaCost; set => noManaCost = value; }

        private bool isGrounded;
        private float turnSmoothVelocity;
        private Vector3 moveDirection;
        private Vector3 verticalVelocity;

        // Quản lý Cooldown theo Skill ID
        private readonly Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();

        // Debug Gizmo Cache
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
            CharacterController = GetComponent<CharacterController>();
            inputReader = GetComponent<PlayerInputReader>() ?? GetComponentInChildren<PlayerInputReader>();
            animationController = GetComponent<LegacyAnimationController>() ?? GetComponentInChildren<LegacyAnimationController>();
            stats = GetComponent<PlayerStats>() ?? GetComponentInChildren<PlayerStats>();
            mainCamera = UnityEngine.Camera.main;

            SkillDatabase.Instance.EnsureLoaded();
            TopDownGame.Data.FactionSkillDatabase.Instance.EnsureLoaded();

            // Chỉ tự động nạp từ FactionSkill nếu các ô kỹ năng chưa được người dùng thiết lập trong Inspector (ID <= 0)
            if (factionId > 0)
            {
                ApplyFactionSkills(factionId, false);
            }

            // Khởi tạo State Machine
            StateMachine = new TopDownGame.StateMachine.StateMachine();
            IdleState = new PlayerIdleState(this, StateMachine);
            MoveState = new PlayerMoveState(this, StateMachine);
            AttackState = new PlayerAttackState(this, StateMachine);
        }

        [ContextMenu("Nạp Kỹ Năng Theo Môn Phái")]
        public void ApplyFactionSkills()
        {
            ApplyFactionSkills(factionId, true);
        }

        public void ApplyFactionSkills(int targetFaction, bool overwriteExisting = false)
        {
            this.factionId = targetFaction;
            var list = TopDownGame.Data.FactionSkillDatabase.GetSkillsByFaction(targetFaction);
            if (list == null || list.Count == 0) return;

            foreach (var fSkill in list)
            {
                if (fSkill.btnName.Equals("Attack", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (overwriteExisting || defaultNormalAttackId <= 0) defaultNormalAttackId = fSkill.skillId;
                }
                else if (fSkill.btnName.Equals("Skill1", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (overwriteExisting || skillSlotQ_Id <= 0) skillSlotQ_Id = fSkill.skillId;
                }
                else if (fSkill.btnName.Equals("Skill2", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (overwriteExisting || skillSlotE_Id <= 0) skillSlotE_Id = fSkill.skillId;
                }
                else if (fSkill.btnName.Equals("Skill3", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Skill3 là chiêu hỗ trợ/hồi phục (như 306 Từ Hàng Phổ Độ). Nếu Q chưa gán thì ưu tiên nạp vào Q
                    if (overwriteExisting || skillSlotQ_Id <= 0) skillSlotQ_Id = fSkill.skillId;
                }
                else if (fSkill.btnName.Equals("Skill5", System.StringComparison.OrdinalIgnoreCase) || fSkill.isAnger)
                {
                    if (overwriteExisting || skillSlotR_Id <= 0) skillSlotR_Id = fSkill.skillId;
                }
            }
        }

        private void Start()
        {
            StateMachine.Initialize(IdleState);
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

            if (inputReader != null)
            {
                usingGamepadDisplay = inputReader.IsUsingGamepad;
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

        public bool IsOnCooldown(int skillId)
        {
            if (noCooldown) return false;
            return skillId > 0 && cooldownTimers.TryGetValue(skillId, out float timeRemaining) && timeRemaining > 0f;
        }

        public float GetRemainingCooldown(int skillId)
        {
            if (noCooldown || skillId <= 0) return 0f;
            return cooldownTimers.TryGetValue(skillId, out float timeRemaining) ? Mathf.Max(0f, timeRemaining) : 0f;
        }

        public void StartCooldown(int skillId, float duration)
        {
            if (noCooldown || skillId <= 0 || duration <= 0f) return;
            cooldownTimers[skillId] = duration;
        }

        private void HandleMovementAndGravity()
        {
            isGrounded = CharacterController.isGrounded;

            if (isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }

            Vector3 finalMove = (moveDirection + verticalVelocity) * Time.deltaTime;
            CharacterController.Move(finalMove);
            moveDirection = Vector3.zero;
        }

        public Vector3 GetInputVector()
        {
            if (inputReader == null) return Vector3.zero;

            Vector2 rawInput = inputReader.MoveInput;
            if (rawInput.sqrMagnitude < 0.001f) return Vector3.zero;

            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }

            if (mainCamera != null)
            {
                Vector3 camForward = mainCamera.transform.forward;
                Vector3 camRight = mainCamera.transform.right;

                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                return (camForward * rawInput.y + camRight * rawInput.x).normalized;
            }

            return new Vector3(rawInput.x, 0f, rawInput.y);
        }

        public bool IsAttackPressed()
        {
            return inputReader != null && (inputReader.AttackTriggered || inputReader.IsAttackHeld);
        }

        public bool CheckAndTriggerSkills()
        {
            if (inputReader == null) return false;

            // 1. Phím Q
            if (inputReader.Skill2Triggered && skillSlotQ_Id > 0)
            {
                SkillData skill = SkillSlotQ;
                if (CanExecuteSkill(skill))
                {
                    ExecuteSkill(skill);
                    return true;
                }
            }

            // 2. Phím E
            if (inputReader.Skill1Triggered && skillSlotE_Id > 0)
            {
                SkillData skill = SkillSlotE;
                if (CanExecuteSkill(skill))
                {
                    ExecuteSkill(skill);
                    return true;
                }
            }

            // 3. Phím R
            if (inputReader.Skill3Triggered && skillSlotR_Id > 0)
            {
                SkillData skill = SkillSlotR;
                if (CanExecuteSkill(skill))
                {
                    ExecuteSkill(skill);
                    return true;
                }
            }

            return false;
        }

        public bool CanExecuteSkill(SkillData skill)
        {
            if (skill == null) return false;
            if (IsOnCooldown(skill.id)) return false;
            if (!noManaCost && stats != null && skill.manaCost > 0f && !stats.HasEnoughMana(skill.manaCost)) return false;
            return true;
        }

        private void ExecuteSkill(SkillData skill)
        {
            if (!noManaCost && stats != null && skill.manaCost > 0f)
            {
                stats.ConsumeMana(skill.manaCost);
            }
            Debug.Log($"[PlayerController] ⚡ Thi triển kỹ năng: [ID {skill.id}] <b>{skill.name}</b> (Quan hệ: {skill.relation}, IsHeal: {skill.IsHeal})");
            ExecuteAction(skill);
            if (!noCooldown)
            {
                StartCooldown(skill.id, skill.cooldown);
            }
        }

        public void Move(Vector3 direction)
        {
            moveDirection = direction * moveSpeed;
        }

        public void MoveWithSpeed(Vector3 direction, float speed)
        {
            moveDirection = direction * speed;
        }

        public void RotateTowards(Vector3 direction)
        {
            RotateTowards(direction, rotationSmoothTime);
        }

        public void RotateTowards(Vector3 direction, float smoothTime)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, smoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }
        }

        public void ResetTurnVelocity()
        {
            turnSmoothVelocity = 0f;
        }

        public void StartNormalAttack()
        {
            SkillData normalAttack = DefaultNormalAttack;
            if (normalAttack != null)
            {
                ExecuteAction(normalAttack);
            }
            else
            {
                Debug.LogWarning($"[PlayerController] Không tìm thấy Skill ID {defaultNormalAttackId} trong Skills.csv!");
            }
        }

        public void ExecuteAction(SkillData skill)
        {
            if (skill == null) return;

            AttackState.SetSkill(skill);

            if (StateMachine.CurrentState == AttackState)
            {
                AttackState.Enter();
            }
            else
            {
                StateMachine.ChangeState(AttackState);
            }
        }

        /// <summary>
        /// Kích hoạt quét sát thương qua SkillDamageResolver tập trung
        /// </summary>
        public void ExecuteSkillDamage(SkillData skill)
        {
            if (skill == null) return;

            SkillDamageResolver.CastDamage(transform, stats, skill, targetLayer);

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

        public void PlaySkillCastEffect(SkillData skill)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.effectPath))
            {
                TopDownGame.Combat.EffectManager.Instance.PlaySkillEffect(skill, transform);
            }
        }

        public void OnHitTriggered(SkillData skill)
        {
            if (skill != null && !skill.HasProjectile)
            {
                TopDownGame.Combat.EffectManager.Instance.PlaySkillEffect(skill, transform);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showHitGizmos || gizmoTimer <= 0f) return;
            SkillDamageResolver.DrawGizmo(lastGizmo.type, lastGizmo.origin, lastGizmo.forward, lastGizmo.range, lastGizmo.fanAngle, lastGizmo.boxWidth);
        }
    }
}
