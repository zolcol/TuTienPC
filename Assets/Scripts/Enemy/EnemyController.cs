using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.NPC;
using TopDownGame.Skills;
using TopDownGame.StateMachine;

namespace TopDownGame.Enemy
{
    [RequireComponent(typeof(CharacterController), typeof(EnemyPerception), typeof(EnemyBrain))]
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyController : MonoBehaviour
    {
        [Header("=== NPC TEMPLATE ===")]
        [SerializeField] private int npcTemplateId = 101;

        [Header("=== MOVEMENT SETTINGS ===")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private string currentStateDisplay;

        public TopDownGame.StateMachine.StateMachine StateMachine { get; private set; }
        public EnemyIdleState IdleState { get; private set; }
        public EnemyChaseState ChaseState { get; private set; }
        public EnemyAttackState AttackState { get; private set; }
        public EnemyHurtState HurtState { get; private set; }
        public EnemyDeadState DeadState { get; private set; }

        public EnemyPerception Perception { get; private set; }
        public EnemyBrain Brain { get; private set; }
        public EnemyStats Stats { get; private set; }
        public CharacterController CharacterController { get; private set; }
        public LegacyAnimationController AnimationController { get; private set; }

        public Transform Target => Perception != null ? Perception.Target : null;
        public float DetectionRange => Perception != null ? Perception.DetectionRange : 10f;
        public float ActiveRadius => Perception != null ? Perception.ActiveRadius : 15f;
        public float AttackRange => Perception != null ? Perception.AttackRange : 3f;
        public Vector3 SpawnPosition => Perception != null ? Perception.SpawnPosition : transform.position;
        public float DistanceFromSpawn => Perception != null ? Perception.GetDistanceToSpawn() : 0f;
        public float MoveSpeed => moveSpeed;
        public float WalkSpeed => walkSpeed;

        public int NpcResId => (npcTemplateId > 0 && NpcTemplateDatabase.GetTemplate(npcTemplateId) != null) ? NpcTemplateDatabase.GetTemplate(npcTemplateId).npcResId : 0;

        private Vector3 verticalVelocity, moveDirection;
        private float turnSmoothVelocity;

        private void Awake()
        {
            Perception = GetComponent<EnemyPerception>() ?? gameObject.AddComponent<EnemyPerception>();
            Brain = GetComponent<EnemyBrain>() ?? gameObject.AddComponent<EnemyBrain>();
            Stats = GetComponent<EnemyStats>() ?? gameObject.AddComponent<EnemyStats>();
            CharacterController = GetComponent<CharacterController>();

            NpcTemplateDatabase.Instance.EnsureLoaded();
            SkillDatabase.Instance.EnsureLoaded();
            ApplyTemplateData();
            EnsureAnimationController();

            StateMachine = new TopDownGame.StateMachine.StateMachine();
            IdleState = new EnemyIdleState(this, StateMachine);
            ChaseState = new EnemyChaseState(this, StateMachine);
            AttackState = new EnemyAttackState(this, StateMachine);
            HurtState = new EnemyHurtState(this, StateMachine);
            DeadState = new EnemyDeadState(this, StateMachine);
        }

        private void Start()
        {
            EnsureAnimationController();
            TryFindTarget();
            if (Stats != null)
            {
                Stats.OnDeath += HandleDeath;
                Stats.OnDamaged += HandleDamaged;
            }
            StateMachine.Initialize(IdleState);
        }

        private void OnDestroy()
        {
            if (Stats != null)
            {
                Stats.OnDeath -= HandleDeath;
                Stats.OnDamaged -= HandleDamaged;
            }
        }

        private void Update()
        {
            StateMachine.Update();
            HandleMovementAndGravity();
            if (StateMachine.CurrentState != null) currentStateDisplay = StateMachine.CurrentState.GetType().Name;
        }

        private void FixedUpdate() => StateMachine.PhysicsUpdate();

        private void HandleMovementAndGravity()
        {
            if (CharacterController == null || !CharacterController.enabled || !CharacterController.gameObject.activeInHierarchy) return;
            verticalVelocity.y = CharacterController.isGrounded && verticalVelocity.y < 0f ? -2f : verticalVelocity.y + gravity * Time.deltaTime;
            CharacterController.Move((moveDirection * moveSpeed + verticalVelocity) * Time.deltaTime);
            moveDirection = Vector3.zero;
        }

        public void MoveTowardsTarget()
        {
            if (Target == null || !CharacterController.enabled) return;
            Vector3 dir = Target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) moveDirection = dir.normalized;
        }

        public void MoveTowardsSpawn()
        {
            if (CharacterController == null || !CharacterController.enabled) return;
            Vector3 dir = SpawnPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) moveDirection = dir.normalized * (moveSpeed > 0f ? (walkSpeed / moveSpeed) : 0.5f);
        }

        public void RotateTowardsTarget() => RotateTowards(Target != null ? Target.position - transform.position : Vector3.zero);
        public void RotateTowardsSpawn() => RotateTowards(SpawnPosition - transform.position);

        private void RotateTowards(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }
        }

        private void HandleDeath() => StateMachine.ChangeState(DeadState);

        private void HandleDamaged(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (Stats == null || Stats.IsDead || StateMachine.CurrentState == DeadState) return;
            StateMachine.ChangeState(HurtState);
        }

        public void TryFindTarget() => Perception?.TryFindTarget();
        public float GetDistanceToTarget() => Perception != null ? Perception.GetDistanceToTarget() : float.MaxValue;
        public float GetDistanceToSpawn() => Perception != null ? Perception.GetDistanceToSpawn() : 0f;
        public SkillData GetReadyAttack(float dist = -1f) => Brain != null ? Brain.GetReadyAttack(dist) : null;
        public float GetMinAttackRange() => Brain != null ? Brain.GetMinAttackRange(AttackRange) : AttackRange;
        public void StartCooldown(int skillId, float duration) => Brain?.StartCooldown(skillId, duration);
        public bool IsOnCooldown(int skillId) => Brain != null && Brain.IsOnCooldown(skillId);
        public void ExecuteSkillDamage(SkillData skill) => Brain?.ExecuteSkillDamage(skill, Target);

        public void EnsureAnimationController()
        {
            if (AnimationController == null) AnimationController = GetComponentInChildren<LegacyAnimationController>();
            if (AnimationController == null)
            {
                Animation anim = GetComponentInChildren<Animation>();
                AnimationController = anim != null ? anim.gameObject.AddComponent<LegacyAnimationController>() : gameObject.AddComponent<LegacyAnimationController>();
            }
            if (AnimationController != null) AnimationController.AutoFindAnimationComponents();
        }

        [ContextMenu("Áp dụng NpcTemplate (Nạp Model & Skills)")]
        public void ApplyTemplateData()
        {
            if (npcTemplateId <= 0) return;
            NpcTemplateDatabase.Instance.EnsureLoaded();
            NpcTemplateData template = NpcTemplateDatabase.GetTemplate(npcTemplateId);
            if (template == null) return;

            var attrib = template.GetAttribute();
            if (attrib != null && Stats != null)
            {
                Stats.Health.SetMaxValue(attrib.maxLife, true);
                Stats.SetPhysicalDamage(attrib.AverageAttack);
                Stats.SetMagicDamage(attrib.woodDamage + attrib.waterDamage + attrib.fireDamage + attrib.earthDamage + attrib.metalDamage);
            }

            var resData = template.GetRes();
            if (resData != null && CharacterController != null)
            {
                CharacterController.height = Mathf.Max(1.0f, resData.height);
                CharacterController.radius = Mathf.Max(0.2f, resData.width);
                CharacterController.center = new Vector3(0f, CharacterController.height * 0.5f, 0f);
            }

            moveSpeed = template.runSpeed > 0f ? template.runSpeed : 5.0f;
            walkSpeed = template.walkSpeed > 0f ? template.walkSpeed : (moveSpeed * 0.5f);

            var skillList = template.GetSkillList();
            float firstSkillRange = (skillList.Count > 0 && SkillDatabase.GetSkill(skillList[0]) != null) ? SkillDatabase.GetSkill(skillList[0]).range : 3f;
            if (Perception != null) Perception.SetRanges(template.visionRadius, template.activeRadius, firstSkillRange);
            if (Brain != null && skillList.Count > 0) Brain.SetSkillList(skillList);

            if (!string.IsNullOrEmpty(template.prefab))
            {
                bool hasChild = false;
                foreach (Transform child in transform)
                {
                    if (child.GetComponentInChildren<LegacyAnimationController>() != null || child.GetComponentInChildren<Renderer>() != null) { hasChild = true; break; }
                }
                if (!hasChild)
                {
                    GameObject modelPrefab = NpcTemplateDatabase.LoadPrefab(template.prefab);
                    if (modelPrefab != null)
                    {
                        GameObject modelInst = Instantiate(modelPrefab, transform);
                        modelInst.name = modelPrefab.name;
                        modelInst.transform.localPosition = Vector3.zero;
                        modelInst.transform.localRotation = Quaternion.identity;
                    }
                }
            }

            int enemyLayer = LayerMask.NameToLayer(CombatLayersAndTags.LayerEnemy);
            if (enemyLayer != -1) SetLayerRecursively(gameObject, enemyLayer);
            SetTagRecursively(gameObject, CombatLayersAndTags.TagEnemy);
            EnsureAnimationController();
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null || layer < 0) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void SetTagRecursively(GameObject obj, string tag)
        {
            if (obj == null || string.IsNullOrEmpty(tag)) return;
            try { obj.tag = tag; } catch (System.Exception) { }
            foreach (Transform child in obj.transform) SetTagRecursively(child.gameObject, tag);
        }
    }
}
