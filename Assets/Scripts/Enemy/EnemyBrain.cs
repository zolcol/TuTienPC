using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Enemy
{
    [DisallowMultipleComponent]
    public class EnemyBrain : MonoBehaviour
    {
        [Header("=== COMBAT & SKILLS ===")]
        [Tooltip("Layer nhận sát thương (vd: Player / Default)")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Tooltip("Danh sách các ID kỹ năng của quái trong Skills.csv")]
        [SerializeField] private List<int> attackSkillIds = new List<int>() { 101 };

        [Header("=== DEBUG GIZMOS ===")]
        [SerializeField] private bool showHitGizmos = true;
        [SerializeField] private float gizmoDisplayDuration = 0.25f;

        public LayerMask TargetLayer { get => targetLayer; set => targetLayer = value; }
        public List<int> AttackSkillIds => attackSkillIds;

        private readonly Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();
        private readonly List<int> cooldownKeysBuffer = new List<int>(8);
        private readonly List<SkillData> readyAttacksBuffer = new List<SkillData>(4);

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

        private EnemyStats stats;

        private void Awake()
        {
            stats = GetComponent<EnemyStats>() ?? GetComponentInParent<EnemyStats>();
        }

        private void Update()
        {
            UpdateCooldowns(Time.deltaTime);
            if (gizmoTimer > 0f)
            {
                gizmoTimer -= Time.deltaTime;
            }
        }

        public void SetSkillList(List<int> skills)
        {
            if (skills != null && skills.Count > 0)
            {
                attackSkillIds.Clear();
                for (int i = 0; i < skills.Count; i++)
                {
                    int id = skills[i];
                    if (id > 0 && !attackSkillIds.Contains(id))
                    {
                        attackSkillIds.Add(id);
                    }
                }
            }
        }

        public void UpdateCooldowns(float dt)
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
                SkillData skill = SkillDatabase.GetBaseSkill(id);
                if (skill != null && !IsOnCooldown(skill.id))
                {
                    // Bỏ qua các kỹ năng tự thân / bị động / buff khi đang tìm chiêu tấn công mục tiêu
                    if (skill.targetSelf || skill.relation == SkillRelation.Self)
                        continue;

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

        public float GetMinAttackRange(float fallbackRange = 3f)
        {
            float minR = float.MaxValue;
            if (attackSkillIds != null && attackSkillIds.Count > 0)
            {
                for (int i = 0; i < attackSkillIds.Count; i++)
                {
                    var sk = SkillDatabase.GetBaseSkill(attackSkillIds[i]);
                    if (sk != null && !sk.targetSelf && sk.relation != SkillRelation.Self && sk.range > 0f && sk.range < minR)
                    {
                        minR = sk.range;
                    }
                }
            }
            return minR < float.MaxValue ? (minR + 0.35f) : (fallbackRange + 0.35f);
        }

        public void ExecuteSkillDamage(SkillData skill, Transform target)
        {
            if (skill == null) return;
            if (stats == null) stats = GetComponent<EnemyStats>() ?? GetComponentInParent<EnemyStats>();

            int monsterLv = stats != null ? stats.MonsterLevel : 1;
            SkillDamageResolver.CastDamage(transform, stats, skill, targetLayer, target, default, false, monsterLv);

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

        private void OnDrawGizmos()
        {
            if (!showHitGizmos || gizmoTimer <= 0f) return;
            SkillDamageResolver.DrawGizmo(lastGizmo.type, lastGizmo.origin, lastGizmo.forward, lastGizmo.range, lastGizmo.fanAngle, lastGizmo.boxWidth);
        }
    }
}
