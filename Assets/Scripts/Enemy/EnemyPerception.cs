using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Data;
using TopDownGame.Stats;

namespace TopDownGame.Enemy
{
    [DisallowMultipleComponent]
    public class EnemyPerception : MonoBehaviour
    {
        [Header("=== TARGET & RANGES ===")]
        [Tooltip("Mục tiêu quái vật đang nhắm tới")]
        [SerializeField] private Transform target;
        [Tooltip("Bán kính phát hiện người chơi / VisionRadius")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("Tầm truy đuổi tối đa / ActiveRadius / Leash Range")]
        [SerializeField] private float activeRadius = 15f;
        [Tooltip("Khoảng cách tiếp cận để tung đòn đánh / AttackRadius")]
        [SerializeField] private float attackRange = 3f;

        [Header("=== GIZMOS ===")]
        [SerializeField] private bool showRangeGizmos = true;

        public Transform Target { get => target; set => target = value; }
        public float DetectionRange { get => detectionRange; set => detectionRange = value; }
        public float ActiveRadius { get => activeRadius; set => activeRadius = value; }
        public float AttackRange { get => attackRange; set => attackRange = value; }
        public Vector3 SpawnPosition { get; set; }
        public NpcAiData AiData { get; private set; }
        public Transform Attacker { get; private set; }

        private float breathTimer;
        private float targetLockTimer;

        private static readonly Collider[] scanBuffer = new Collider[32];
        private static readonly List<Transform> candidateBuffer = new List<Transform>(16);

        private void Awake()
        {
            SpawnPosition = transform.position;
            AiData = NpcAiData.CreateDefaultActive();
            breathTimer = Random.Range(0f, AiData.breathTimeSec);
        }

        public void SetAiData(NpcAiData data)
        {
            if (data != null)
            {
                AiData = data;
                breathTimer = Random.Range(0f, AiData.breathTimeSec);
            }
        }

        public void SetRanges(float visionRadius, float leashRadius, float atkRange)
        {
            detectionRange = visionRadius;
            activeRadius = leashRadius;
            if (atkRange > 0f) attackRange = atkRange;
        }

        public void NotifyDamaged(Transform attacker)
        {
            if (attacker == null || attacker == transform) return;
            Attacker = attacker;

            // Nếu quái có cơ chế phản đòn (StrikeBack) và chưa có mục tiêu hoặc hết thời gian khóa mục tiêu
            if (AiData != null && AiData.canStrikeBack)
            {
                if (target == null || targetLockTimer <= 0f)
                {
                    target = attacker;
                    targetLockTimer = AiData.lockDuration;
                }
            }
        }

        public void ClearTarget()
        {
            target = null;
            targetLockTimer = 0f;
        }

        public void ClearAttacker()
        {
            Attacker = null;
        }

        /// <summary>
        /// Cập nhật nhịp thở AI (Tick Rate) và đánh giá chọn mục tiêu theo DATA_CONVENTIONS_V2.md (Mục 21)
        /// </summary>
        public void TickPerception(float dt)
        {
            if (targetLockTimer > 0f)
            {
                targetLockTimer -= dt;
            }

            // Kiểm tra tính hợp lệ của mục tiêu hiện tại
            if (target != null)
            {
                if (!IsTargetValid(target))
                {
                    ClearTarget();
                }
            }

            breathTimer -= dt;
            if (breathTimer <= 0f)
            {
                breathTimer = AiData != null ? AiData.breathTimeSec : 1.0f;
                EvaluateTarget();
            }
        }

        private bool IsTargetValid(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy) return false;

            var stats = t.GetComponent<EntityStats>() ?? t.GetComponentInParent<EntityStats>();
            if (stats != null && stats.IsDead) return false;

            // Nếu khoảng cách đến Spawn hoặc cự ly vượt quá Leash Range (ActiveRadius)
            float distToSpawn = GetDistanceToSpawn();
            if (distToSpawn > activeRadius) return false;

            float distToTarget = Vector3.Distance(transform.position, t.position);
            if (distToTarget > activeRadius * 1.25f) return false;

            return true;
        }

        private void EvaluateTarget()
        {
            // Nếu đang khóa mục tiêu và mục tiêu vẫn hợp lệ thì giữ nguyên
            if (target != null && targetLockTimer > 0f && IsTargetValid(target))
            {
                return;
            }

            // 1. Nếu là quái bị động (Attack == 0), chỉ chọn mục tiêu khi bị đánh (StrikeBack)
            if (AiData != null && !AiData.isAggressive)
            {
                if (AiData.canStrikeBack && Attacker != null && IsTargetValid(Attacker))
                {
                    target = Attacker;
                    targetLockTimer = AiData.lockDuration;
                }
                else
                {
                    target = null;
                }
                return;
            }

            // 2. Quái chủ động (Attack == 1): Quét tìm mục tiêu trong VisionRadius
            candidateBuffer.Clear();
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 1.0f, detectionRange, scanBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = scanBuffer[i];
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;

                // Chỉ nhắm vào kẻ địch đối địch (Player / LayerPlayer / TagPlayer)
                if (col.CompareTag(CombatLayersAndTags.TagPlayer) || col.GetComponentInParent<TopDownGame.Player.PlayerController>() != null)
                {
                    Transform rootTransform = col.transform.root;
                    if (!candidateBuffer.Contains(rootTransform) && IsTargetValid(rootTransform))
                    {
                        candidateBuffer.Add(rootTransform);
                    }
                }
            }

            if (candidateBuffer.Count == 0)
            {
                // Nếu không thấy ai trong tầm nhìn, nhưng có Attacker từ xa đánh trúng
                if (AiData != null && AiData.canStrikeBack && Attacker != null && IsTargetValid(Attacker))
                {
                    target = Attacker;
                    targetLockTimer = AiData.lockDuration;
                }
                else
                {
                    target = null;
                }
                return;
            }

            // 3. Lọc mục tiêu theo SelectTarget enum
            AiTargetSelectType selectType = AiData != null ? AiData.selectTarget : AiTargetSelectType.StrikeBack;
            Transform selected = null;

            switch (selectType)
            {
                case AiTargetSelectType.StrikeBack:
                    if (Attacker != null && candidateBuffer.Contains(Attacker))
                    {
                        selected = Attacker;
                    }
                    else
                    {
                        selected = GetNearestTarget(candidateBuffer);
                    }
                    break;

                case AiTargetSelectType.Nearest:
                    selected = GetNearestTarget(candidateBuffer);
                    break;

                case AiTargetSelectType.Poorest:
                    selected = GetPoorestTarget(candidateBuffer);
                    break;

                case AiTargetSelectType.Richest:
                    selected = GetRichestTarget(candidateBuffer);
                    break;

                case AiTargetSelectType.Random:
                    selected = candidateBuffer[Random.Range(0, candidateBuffer.Count)];
                    break;

                case AiTargetSelectType.Player:
                    selected = GetNearestTarget(candidateBuffer);
                    break;

                default:
                    selected = GetNearestTarget(candidateBuffer);
                    break;
            }

            if (selected != null)
            {
                target = selected;
                targetLockTimer = AiData != null ? AiData.lockDuration : 4.0f;
            }
        }

        private Transform GetNearestTarget(List<Transform> list)
        {
            Transform best = null;
            float minDst = float.MaxValue;
            Vector3 myPos = transform.position;

            for (int i = 0; i < list.Count; i++)
            {
                Transform t = list[i];
                if (t == null) continue;
                Vector3 diff = t.position - myPos;
                diff.y = 0f;
                float d = diff.sqrMagnitude;
                if (d < minDst)
                {
                    minDst = d;
                    best = t;
                }
            }
            return best;
        }

        private Transform GetPoorestTarget(List<Transform> list)
        {
            Transform best = null;
            float minHpRatio = float.MaxValue;

            for (int i = 0; i < list.Count; i++)
            {
                Transform t = list[i];
                if (t == null) continue;
                var s = t.GetComponent<EntityStats>() ?? t.GetComponentInParent<EntityStats>();
                if (s != null && s.Health.MaxValue > 0)
                {
                    float ratio = s.Health.CurrentValue / s.Health.MaxValue;
                    if (ratio < minHpRatio)
                    {
                        minHpRatio = ratio;
                        best = t;
                    }
                }
            }
            return best ?? GetNearestTarget(list);
        }

        private Transform GetRichestTarget(List<Transform> list)
        {
            Transform best = null;
            float maxHpRatio = -1f;

            for (int i = 0; i < list.Count; i++)
            {
                Transform t = list[i];
                if (t == null) continue;
                var s = t.GetComponent<EntityStats>() ?? t.GetComponentInParent<EntityStats>();
                if (s != null && s.Health.MaxValue > 0)
                {
                    float ratio = s.Health.CurrentValue / s.Health.MaxValue;
                    if (ratio > maxHpRatio)
                    {
                        maxHpRatio = ratio;
                        best = t;
                    }
                }
            }
            return best ?? GetNearestTarget(list);
        }

        public void TryFindTarget()
        {
            EvaluateTarget();
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
    }
}
