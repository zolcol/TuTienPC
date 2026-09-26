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
        [Tooltip("Ã„ÂÃ¡Â»â„¢ mÃ†Â°Ã¡Â»Â£t/gia tÃ¡Â»â€˜c khi xoay ngÃ†Â°Ã¡Â»Âi Ã„â€˜Ã¡Â»â€¢i hÃ†Â°Ã¡Â»â€ºng lÃƒÂºc Ã„â€˜ang Ã„â€˜ÃƒÂ¡nh (0.12 - 0.18s giÃƒÂºp xoay Ã„â€˜Ã¡ÂºÂ§m tay, cÃƒÂ³ quÃƒÂ¡n tÃƒÂ­nh, khÃƒÂ´ng giÃ¡ÂºÂ­t ngoÃ¡ÂºÂ¯t)")]
        [SerializeField] private float attackRotationSmoothTime = 0.14f;
        [SerializeField] private float gravity = -9.81f;

        [Header("=== COMBAT & TARGETING ===")]
        [Tooltip("Layer nhÃ¡ÂºÂ­n sÃƒÂ¡t thÃ†Â°Ã†Â¡ng cÃ¡Â»Â§a mÃ¡Â»Â¥c tiÃƒÂªu (vd: Enemy hoÃ¡ÂºÂ·c Default)")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Tooltip("ID cÃ¡Â»Â§a Ã„â€˜ÃƒÂ²n Ã„â€˜ÃƒÂ¡nh thÃ†Â°Ã¡Â»Âng Ã„â€˜Ã¡ÂºÂ§u tiÃƒÂªn trong Skills.csv (vd: 301)")]
        [SerializeField] private int defaultNormalAttackId = 301;

        [Header("=== FACTION & SKILL SLOTS (Q - E - R) ===")]
        [Tooltip("MÃƒÂ´n phÃƒÂ¡i cÃ¡Â»Â§a nhÃƒÂ¢n vÃ¡ÂºÂ­t (1: ThiÃƒÂªn VÃ†Â°Ã†Â¡ng, 2: Nga Mi, 3: Ã„ÂÃƒÂ o Hoa, 4: TiÃƒÂªu Dao, ...)")]
        [SerializeField] private int factionId = 2;

        [Tooltip("ID cÃ¡Â»Â§a Model trong NpcRes.csv (vd: 1 cho ThiÃƒÂªn VÃ†Â°Ã†Â¡ng Nam, 2 cho Nga Mi, 3 cho Ã„ÂÃƒÂ o Hoa)")]
        [SerializeField] private int npcResId = 2;

        [Tooltip("KÃ¡Â»Â¹ nÃ„Æ’ng 1: PhÃƒÂ­m Q (vd: 306 - Nga Mi Skill 1 / TÃ¡Â»Â¥ HÃƒÂ ng PhÃ¡Â»â€¢ Ã„ÂÃ¡Â»â„¢)")]
        [SerializeField] private int skillSlotQ_Id = 306;

        [Tooltip("KÃ¡Â»Â¹ nÃ„Æ’ng 2: PhÃƒÂ­m E (vd: 308 - Nga Mi Skill 2 / BÃ¡ÂºÂ¡ch LÃ¡Â»â„¢ NgÃ†Â°ng SÃ†Â°Ã†Â¡ng)")]
        [SerializeField] private int skillSlotE_Id = 308;

        [Tooltip("KÃ¡Â»Â¹ nÃ„Æ’ng 3: PhÃƒÂ­m R (vd: 346 - Nga Mi NÃ¡Â»â„¢ / BÃ„Æ’ng PhÃƒÂ¡ch HÃ¡Â»â€œng LiÃƒÂªn KiÃ¡ÂºÂ¿p)")]
        [SerializeField] private int skillSlotR_Id = 346;

        [Header("=== CHEAT & TESTING ===")]
        [Tooltip("BÃ¡Â»Â qua thÃ¡Â»Âi gian hÃ¡Â»â€œi chiÃƒÂªu (No Cooldown / NoCD) khi test")]
        [SerializeField] private bool noCooldown = false;
        [Tooltip("BÃ¡Â»Â qua tiÃƒÂªu hao Mana / NÃ¡Â»â„¢i lÃ¡Â»Â±c khi test")]
        [SerializeField] private bool noManaCost = false;

        [Header("=== DEBUG & GIZMOS (TÃƒÂ¹y chÃ¡Â»Ân) ===")]
        [Tooltip("HiÃ¡Â»Æ’n thÃ¡Â»â€¹ vÃƒÂ¹ng quÃƒÂ©t tia / quÃ¡ÂºÂ¡t / vÃƒÂ²ng trÃƒÂ²n trong Scene View khi tung Ã„â€˜ÃƒÂ²n")]
        [SerializeField] private bool showHitGizmos = true;
        [Tooltip("ThÃ¡Â»Âi gian lÃ†Â°u vÃ¡Â»â€¡t Gizmo (giÃƒÂ¢y)")]
        [SerializeField] private float gizmoDisplayDuration = 0.25f;
        [Tooltip("TrÃ¡ÂºÂ¡ng thÃƒÂ¡i hiÃ¡Â»â€¡n tÃ¡ÂºÂ¡i cÃ¡Â»Â§a Player (ChÃ¡Â»â€° xem)")]
        [SerializeField] private string currentStateDisplay;
        [Tooltip("Ã„Âang dÃƒÂ¹ng tay cÃ¡ÂºÂ§m Gamepad (ChÃ¡Â»â€° xem)")]
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

        // --- SKILL SELECTOR (AIMING) ---
        private SkillData aimingSkill;
        private GameObject currentIndicator;
        private Vector3 aimGroundPosition;

        // QuÃ¡ÂºÂ£n lÃƒÂ½ Cooldown theo Skill ID
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

            // ChÃ¡Â»â€° tÃ¡Â»Â± Ã„â€˜Ã¡Â»â„¢ng nÃ¡ÂºÂ¡p tÃ¡Â»Â« FactionSkill nÃ¡ÂºÂ¿u cÃƒÂ¡c ÃƒÂ´ kÃ¡Â»Â¹ nÃ„Æ’ng chÃ†Â°a Ã„â€˜Ã†Â°Ã¡Â»Â£c ngÃ†Â°Ã¡Â»Âi dÃƒÂ¹ng thiÃ¡ÂºÂ¿t lÃ¡ÂºÂ­p trong Inspector (ID <= 0)
            if (factionId > 0)
            {
                ApplyFactionSkills(factionId, false);
            }

            // KhÃ¡Â»Å¸i tÃ¡ÂºÂ¡o State Machine
            StateMachine = new TopDownGame.StateMachine.StateMachine();
            IdleState = new PlayerIdleState(this, StateMachine);
            MoveState = new PlayerMoveState(this, StateMachine);
            AttackState = new PlayerAttackState(this, StateMachine);
        }

        [ContextMenu("NÃ¡ÂºÂ¡p KÃ¡Â»Â¹ NÃ„Æ’ng Theo MÃƒÂ´n PhÃƒÂ¡i")]
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
                    // Skill3 lÃƒÂ  chiÃƒÂªu hÃ¡Â»â€” trÃ¡Â»Â£/hÃ¡Â»â€œi phÃ¡Â»Â¥c (nhÃ†Â° 306 TÃ¡Â»Â« HÃƒÂ ng PhÃ¡Â»â€¢ Ã„ÂÃ¡Â»â„¢). NÃ¡ÂºÂ¿u Q chÃ†Â°a gÃƒÂ¡n thÃƒÂ¬ Ã†Â°u tiÃƒÂªn nÃ¡ÂºÂ¡p vÃƒÂ o Q
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
            UpdateSkillAiming();
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

            if (HandleSkillInput(inputReader.Skill2Triggered, inputReader.Skill2Held, inputReader.Skill2Released, SkillSlotQ)) return true;
            if (HandleSkillInput(inputReader.Skill1Triggered, inputReader.Skill1Held, inputReader.Skill1Released, SkillSlotE)) return true;
            if (HandleSkillInput(inputReader.Skill3Triggered, inputReader.Skill3Held, inputReader.Skill3Released, SkillSlotR)) return true;

            return false;
        }

        private bool HandleSkillInput(bool triggered, bool held, bool released, SkillData skill)
        {
            if (skill == null || !CanExecuteSkill(skill)) 
            {
                if (aimingSkill == skill) CancelAiming();
                return false;
            }

            if (skill.selectorType == SkillSelectorType.None)
            {
                // Quick Cast
                if (triggered)
                {
                    CancelAiming();
                    return ExecuteSkill(skill);
                }
            }
            else
            {
                // Aiming Cast
                if (triggered)
                {
                    StartAiming(skill);
                }
                else if (released && aimingSkill == skill)
                {
                    CancelAiming();
                    return ExecuteSkill(skill);
                }
                else if (!held && aimingSkill == skill)
                {
                    CancelAiming();
                }
            }

            return false;
        }

        private void StartAiming(SkillData skill)
        {
            if (aimingSkill != skill)
            {
                CancelAiming();
            }
            aimingSkill = skill;
            
            int resId = 0;
            switch(skill.selectorType)
            {
                case SkillSelectorType.SmartcastCircleAOE:
                    if (skill.IsHeal) resId = IndicatorVfxResID.SelectedAllyAOE;
                    else if (skill.startPosType == VfxStartPosType.Target) resId = IndicatorVfxResID.SelectedEnemyAOE; // Lock Target
                    else resId = IndicatorVfxResID.SelectedEnemyAOE; // Smartcast
                    break;
                case SkillSelectorType.DirectionalArrow:
                    resId = IndicatorVfxResID.DirectionArrow;
                    break;
            }
            
            if (resId > 0)
            {
                string path = TopDownGame.Data.EffectDatabase.GetEffectPath(resId);
                if (!string.IsNullOrEmpty(path))
                {
                    currentIndicator = TopDownGame.Combat.EffectManager.Instance.SpawnEffect(path, transform.position, transform.rotation, null, 99f);
                }
            }
        }

        public void CancelAiming()
        {
            aimingSkill = null;
            if (currentIndicator != null)
            {
                Destroy(currentIndicator);
                currentIndicator = null;
            }
        }

        private void UpdateSkillAiming()
        {
            if (aimingSkill == null || currentIndicator == null) return;

            bool usingMouse = inputReader != null && !inputReader.IsUsingGamepad;
            float maxRange = aimingSkill.selectorRange > 0f ? aimingSkill.selectorRange : aimingSkill.range;
            
            Vector3 targetGroundPos = transform.position + transform.forward * maxRange;
            Vector3 aimDir = transform.forward;

            if (usingMouse)
            {
                if (UnityEngine.InputSystem.Mouse.current != null && mainCamera != null)
                {
                    Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
                    Plane groundPlane = new Plane(Vector3.up, transform.position);
                    if (groundPlane.Raycast(ray, out float enter))
                    {
                        Vector3 hitPoint = ray.GetPoint(enter);
                        Vector3 offset = hitPoint - transform.position;
                        if (offset.magnitude > maxRange)
                        {
                            targetGroundPos = transform.position + offset.normalized * maxRange;
                        }
                        else
                        {
                            targetGroundPos = hitPoint;
                        }
                        aimDir = (targetGroundPos - transform.position).normalized;
                    }
                }
            }
            else
            {
                Vector3 inputDir = GetInputVector();
                if (inputDir.sqrMagnitude > 0.01f)
                {
                    aimDir = inputDir;
                    targetGroundPos = transform.position + aimDir * maxRange;
                }
            }

            if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;

            if (aimingSkill.selectorType == SkillSelectorType.DirectionalArrow)
            {
                currentIndicator.transform.position = transform.position;
                currentIndicator.transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
            }
            else if (aimingSkill.selectorType == SkillSelectorType.SmartcastCircleAOE)
            {
                aimGroundPosition = targetGroundPos;
                currentIndicator.transform.position = targetGroundPos;
            }
        }

        public bool CanExecuteSkill(SkillData skill)
        {
            if (skill == null) return false;
            if (IsOnCooldown(skill.id)) return false;
            if (!noManaCost && stats != null && skill.manaCost > 0f && !stats.HasEnoughMana(skill.manaCost)) return false;
            return true;
        }

        private bool ExecuteSkill(SkillData skill)
        {
            if (!ExecuteAction(skill)) return false;

            if (!noManaCost && stats != null && skill.manaCost > 0f)
            {
                stats.ConsumeMana(skill.manaCost);
            }
            if (!noCooldown)
            {
                StartCooldown(skill.id, skill.cooldown);
            }
            return true;
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

        private Transform currentLockTarget;
        private Vector3 currentTargetPoint;

        public bool StartNormalAttack()
        {
            SkillData normalAttack = DefaultNormalAttack;
            if (normalAttack != null)
            {
                return ExecuteAction(normalAttack);
            }
            else
            {
                Debug.LogWarning($"[PlayerController] KhÃ´ng tÃ¬m th?y Skill ID {defaultNormalAttackId} trong Skills.csv!");
                return false;
            }
        }

        private bool AimSkill(SkillData skill)
        {
            if (skill == null) return false;
            
            bool usingMouse = inputReader != null && !inputReader.IsUsingGamepad;
            currentLockTarget = null;
            currentTargetPoint = transform.position + transform.forward * (skill.range > 0 ? skill.range : 5f);

            if (skill.targetSelf || skill.relation == SkillRelation.Self)
            {
                currentTargetPoint = transform.position;
                return true; 
            }

            bool isTargetLockSkill = skill.startPosType == TopDownGame.Skills.VfxStartPosType.Target && skill.relation == TopDownGame.Skills.SkillRelation.Enemy;

            if (usingMouse)
            {
                if (UnityEngine.InputSystem.Mouse.current == null || mainCamera == null) return true;
                
                Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
                
                if (isTargetLockSkill)
                {
                    Transform foundTarget = null;
                    
                    // Su dung SphereCastAll tao hinh tru ban kinh 1.5f (Soft targeting)
                    RaycastHit[] hits = Physics.SphereCastAll(ray, 1.5f, 100f, targetLayer);
                    if (hits.Length > 0)
                    {
                        float minDistanceToRay = float.MaxValue;
                        foreach (var hit in hits)
                        {
                            Vector3 enemyPos = hit.collider.transform.position;
                            float distToRay = Vector3.Cross(ray.direction, enemyPos - ray.origin).magnitude;
                            
                            if (distToRay < minDistanceToRay)
                            {
                                minDistanceToRay = distToRay;
                                foundTarget = hit.collider.transform;
                            }
                        }
                    }

                    if (foundTarget != null)
                    {
                        float distance = Vector3.Distance(transform.position, foundTarget.position);
                        if (distance <= skill.range)
                        {
                            currentLockTarget = foundTarget;
                            currentTargetPoint = currentLockTarget.position;
                            RotateTowardsInstantly(currentLockTarget.position - transform.position);
                            return true;
                        }
                    }

                    // Nếu không hover trúng quái bằng chuột, tự động tìm mục tiêu trước mặt (Soft Auto-Lock)
                    Transform fallbackTarget = FindTargetInFront(skill.range);
                    if (fallbackTarget != null)
                    {
                        currentLockTarget = fallbackTarget;
                        currentTargetPoint = currentLockTarget.position;
                        RotateTowardsInstantly(currentLockTarget.position - transform.position);
                        return true;
                    }
                    else
                    {
                        return false; 
                    }
                }

                Plane groundPlane = new Plane(Vector3.up, transform.position);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    currentTargetPoint = ray.GetPoint(enter);
                    RotateTowardsInstantly(currentTargetPoint - transform.position);
                }
            }
            else
            {
                if (isTargetLockSkill)
                {
                    Transform bestTarget = FindTargetInFront(skill.range);
                    if (bestTarget != null)
                    {
                        currentLockTarget = bestTarget;
                        currentTargetPoint = currentLockTarget.position;
                        RotateTowardsInstantly(currentLockTarget.position - transform.position);
                        return true;
                    }
                    else
                    {
                        return false; // KhÃƒÂ´ng cÃƒÂ³ ai Ã„â€˜Ã¡Â»Æ’ Ã„â€˜ÃƒÂ¡nh
                    }
                }

                // Gamepad: GiÃ¡Â»Â¯ nguyÃƒÂªn hÃ†Â°Ã¡Â»â€ºng quay hiÃ¡Â»â€¡n tÃ¡ÂºÂ¡i hoÃ¡ÂºÂ·c hÃ†Â°Ã¡Â»â€ºng input (nÃ¡ÂºÂ¿u Ã„â€˜ang Ã„â€˜Ã¡ÂºÂ©y cÃ¡ÂºÂ§n)
                Vector3 inputVec = GetInputVector();
                if (inputVec.sqrMagnitude > 0.01f)
                {
                    RotateTowardsInstantly(inputVec);
                    currentTargetPoint = transform.position + inputVec.normalized * (skill.range > 0 ? skill.range : 5f);
                }
            }

            return true;
        }

        private Transform FindTargetInFront(float range)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, targetLayer);
            Transform best = null;
            float minDot = 0.3f; // KhoÃ¡ÂºÂ£ng 70 Ã„â€˜Ã¡Â»â„¢ nÃƒÂ³n phÃƒÂ­a trÃ†Â°Ã¡Â»â€ºc
            float minDst = float.MaxValue;

            foreach (var h in hits)
            {
                Vector3 dir = (h.transform.position - transform.position);
                dir.y = 0;
                float dst = dir.magnitude;
                if (dst > 0.01f)
                {
                    dir /= dst;
                    float dot = Vector3.Dot(transform.forward, dir);
                    if (dot > minDot && dst < minDst)
                    {
                        minDst = dst;
                        best = h.transform;
                    }
                }
                else
                {
                    return h.transform; // Ã„ÂÃ¡Â»Â©ng sÃƒÂ¡t cÃ¡ÂºÂ¡nh nhau
                }
            }
            return best;
        }

        private void RotateTowardsInstantly(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
        }

        public bool ExecuteAction(SkillData skill)
        {
            if (skill == null) return false;

            if (!AimSkill(skill)) 
            {
                return false; 
            }

            AttackState.SetSkill(skill);

            if (StateMachine.CurrentState == AttackState)
            {
                AttackState.Enter();
            }
            else
            {
                StateMachine.ChangeState(AttackState);
            }
            return true;
        }

        public void ExecuteSkillDamage(SkillData skill)
        {
            if (skill == null) return;

            SkillDamageResolver.CastDamage(transform, stats, skill, targetLayer, currentLockTarget, currentTargetPoint);

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






