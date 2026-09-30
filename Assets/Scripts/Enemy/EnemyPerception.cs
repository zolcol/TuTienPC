using UnityEngine;
using TopDownGame.Combat;

namespace TopDownGame.Enemy
{
    [DisallowMultipleComponent]
    public class EnemyPerception : MonoBehaviour
    {
        [Header("=== TARGET & RANGES ===")]
        [Tooltip("Mục tiêu quái vật rượt đuổi (để trống sẽ tự động tìm Player theo Tag)")]
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

        private void Awake()
        {
            SpawnPosition = transform.position;
        }

        public void SetRanges(float visionRadius, float leashRadius, float atkRange)
        {
            detectionRange = visionRadius;
            activeRadius = leashRadius;
            if (atkRange > 0f) attackRange = atkRange;
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
