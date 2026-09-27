using System;
using UnityEngine;
using TopDownGame.Combat;

namespace TopDownGame.Stats
{
    /// <summary>
    /// Base class quản lý chỉ số sinh mệnh cho mọi thực thể (Player, Quái vật, Boss, NPC, Thùng gỗ, ...)
    /// </summary>
    public class EntityStats : MonoBehaviour, IDamageable
    {
        [Header("Health Resource")]
        [SerializeField] protected ResourceStat health = new ResourceStat();

        [Header("Offensive Stats (Chỉ số tấn công)")]
        [Tooltip("Sát thương vật lý cơ bản")]
        [SerializeField] protected float physicalDamage = 20f;

        [Tooltip("Sát thương phép cơ bản")]
        [SerializeField] protected float magicDamage = 10f;

        [Tooltip("Tốc độ đánh (%) cộng thêm (AttackSpeed trong NpcAttribute.csv)")]
        [SerializeField] protected float attackSpeed = 0f;

        public ResourceStat Health => health;
        public float PhysicalDamage => physicalDamage;
        public float MagicDamage => magicDamage;
        public float AttackSpeed => attackSpeed;
        public bool IsDead { get; protected set; }

        public void SetPhysicalDamage(float value) => physicalDamage = Mathf.Max(0f, value);
        public void SetMagicDamage(float value) => magicDamage = Mathf.Max(0f, value);
        public void SetAttackSpeed(float value) => attackSpeed = value;

        public event Action OnDeath;
        public event Action<float, Vector3, Vector3> OnDamaged; // amount, hitPoint, hitDirection

        protected virtual void Awake()
        {
            health.Initialize();
        }

        protected virtual void Update()
        {
            if (IsDead) return;
            health.Tick(Time.deltaTime);
        }

        public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead || amount <= 0f) return;

            health.Modify(-amount);
            OnDamaged?.Invoke(amount, hitPoint, hitDirection);

            if (health.CurrentValue <= 0f)
            {
                Die();
            }
        }

        public virtual void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            health.Modify(amount);
        }

        public virtual void Revive(float healthPercentage = 1.0f)
        {
            IsDead = false;
            health.Initialize();
            if (healthPercentage < 1.0f)
            {
                health.Modify(-health.MaxValue * (1.0f - healthPercentage));
            }
        }

        protected virtual void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
            // Debug.Log($"💀 <color=red>[DEATH]</color> {gameObject.name} đã bị hạ gục!");
        }
    }
}
