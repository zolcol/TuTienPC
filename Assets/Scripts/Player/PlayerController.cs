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
        [Tooltip("Äá»™ mÆ°á»£t/gia tá»‘c khi xoay ngÆ°á»i Ä‘á»•i hÆ°á»›ng lÃºc Ä‘ang Ä‘Ã¡nh (0.12 - 0.18s giÃºp xoay Ä‘áº§m tay, cÃ³ quÃ¡n tÃ­nh, khÃ´ng giáº­t ngoáº¯t)")]
        [SerializeField] private float attackRotationSmoothTime = 0.14f;
        [SerializeField] private float gravity = -9.81f;

        [Header("=== COMBAT & TARGETING ===")]
        [Tooltip("Layer nháº­n sÃ¡t thÆ°Æ¡ng cá»§a má»¥c tiÃªu (vd: Enemy hoáº·c Default)")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Tooltip("ID cá»§a Ä‘Ã²n Ä‘Ã¡nh thÆ°á»ng Ä‘áº§u tiÃªn trong Skills.csv (vd: 301)")]
        [SerializeField] private int defaultNormalAttackId = 301;

        [Header("=== FACTION & SKILL SLOTS (Q - E - R) ===")]
        [Tooltip("MÃ´n phÃ¡i cá»§a nhÃ¢n váº­t (1: ThiÃªn VÆ°Æ¡ng, 2: Nga Mi, 3: ÄÃ o Hoa, 4: TiÃªu Dao, ...)")]
        [SerializeField] private int factionId = 2;

        [Tooltip("ID cá»§a Model trong NpcRes.csv (vd: 1 cho ThiÃªn VÆ°Æ¡ng Nam, 2 cho Nga Mi, 3 cho ÄÃ o Hoa)")]
        [SerializeField] private int npcResId = 2;

        [Tooltip("Ká»¹ nÄƒng 1: PhÃ­m Q (vd: 306 - Nga Mi Skill 1 / Tá»¥ HÃ ng Phá»• Äá»™)")]
        [SerializeField] private int skillSlotQ_Id = 306;

        [Tooltip("Ká»¹ nÄƒng 2: PhÃ­m E (vd: 308 - Nga Mi Skill 2 / Báº¡ch Lá»™ NgÆ°ng SÆ°Æ¡ng)")]
        [SerializeField] private int skillSlotE_Id = 308;

        [Tooltip("Ká»¹ nÄƒng 3: PhÃ­m R (vd: 346 - Nga Mi Ná»™ / BÄƒng PhÃ¡ch Há»“ng LiÃªn Kiáº¿p)")]
        [SerializeField] private int skillSlotR_Id = 346;

        [Header("=== CHEAT & TESTING ===")]
        [Tooltip("Bá» qua thá»i gian há»“i chiÃªu (No Cooldown / NoCD) khi test")]
        [SerializeField] private bool noCooldown = false;
        [Tooltip("Bá» qua tiÃªu hao Mana / Ná»™i lá»±c khi test")]
        [SerializeField] private bool noManaCost = false;

        [Header("=== DEBUG & GIZMOS (TÃ¹y chá»n) ===")]
        [Tooltip("Hiá»ƒn thá»‹ vÃ¹ng quÃ©t tia / quáº¡t / vÃ²ng trÃ²n trong Scene View khi tung Ä‘Ã²n")]
        [SerializeField] private bool showHitGizmos = true;
        [Tooltip("Thá»i gian lÆ°u vá»‡t Gizmo (giÃ¢y)")]
        [SerializeField] private float gizmoDisplayDuration = 0.25f;
        [Tooltip("Tráº¡ng thÃ¡i hiá»‡n táº¡i cá»§a Player (Chá»‰ xem)")]
        [SerializeField] private string currentStateDisplay;
        [Tooltip("Äang dÃ¹ng tay cáº§m Gamepad (Chá»‰ xem)")]
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

        // Quáº£n lÃ½ Cooldown theo Skill ID
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

            // Chá»‰ tá»± Ä‘á»™ng náº¡p tá»« FactionSkill náº¿u cÃ¡c Ã´ ká»¹ nÄƒng chÆ°a Ä‘Æ°á»£c ngÆ°á»i dÃ¹ng thiáº¿t láº­p trong Inspector (ID <= 0)
            if (factionId > 0)
            {
                ApplyFactionSkills(factionId, false);
            }

            // Khá»Ÿi táº¡o State Machine
            StateMachine = new TopDownGame.StateMachine.StateMachine();
            IdleState = new PlayerIdleState(this, StateMachine);
            MoveState = new PlayerMoveState(this, StateMachine);
            AttackState = new PlayerAttackState(this, StateMachine);
        }

        [ContextMenu("Náº¡p Ká»¹ NÄƒng Theo MÃ´n PhÃ¡i")]
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
                    // Skill3 lÃ  chiÃªu há»— trá»£/há»“i phá»¥c (nhÆ° 306 Tá»« HÃ ng Phá»• Äá»™). Náº¿u Q chÆ°a gÃ¡n thÃ¬ Æ°u tiÃªn náº¡p vÃ o Q
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

            if (inputReader.Skill2Triggered && skillSlotQ_Id > 0)
            {
                SkillData skill = SkillSlotQ;
                if (CanExecuteSkill(skill) && ExecuteSkill(skill)) return true;
            }
            if (inputReader.Skill1Triggered && skillSlotE_Id > 0)
            {
                SkillData skill = SkillSlotE;
                if (CanExecuteSkill(skill) && ExecuteSkill(skill)) return true;
            }
            if (inputReader.Skill3Triggered && skillSlotR_Id > 0)
            {
                SkillData skill = SkillSlotR;
                if (CanExecuteSkill(skill) && ExecuteSkill(skill)) return true;
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
                Debug.LogWarning($"[PlayerController] Không tìm th?y Skill ID {defaultNormalAttackId} trong Skills.csv!");
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
                        else
                        {
                            return false; 
                        }
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
                        return false; // KhÃ´ng cÃ³ ai Ä‘á»ƒ Ä‘Ã¡nh
                    }
                }

                // Gamepad: Giá»¯ nguyÃªn hÆ°á»›ng quay hiá»‡n táº¡i hoáº·c hÆ°á»›ng input (náº¿u Ä‘ang Ä‘áº©y cáº§n)
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
            float minDot = 0.3f; // Khoáº£ng 70 Ä‘á»™ nÃ³n phÃ­a trÆ°á»›c
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
                    return h.transform; // Äá»©ng sÃ¡t cáº¡nh nhau
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





