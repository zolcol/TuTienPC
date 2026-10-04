using System;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.UI;

namespace TopDownGame.Stats
{
    /// <summary>
    /// Base class quản lý chỉ số sinh mệnh cho mọi thực thể (Player, Quái vật, Boss, NPC, Thùng gỗ, ...)
    /// </summary>
    public class EntityStats : MonoBehaviour, IDamageable
    {
        [Header("Vitals / Resources (Tài nguyên sinh tồn)")]
        [SerializeField] protected ResourceStat health = new ResourceStat();
        [SerializeField] protected ResourceStat mana = new ResourceStat();

        [Header("Offensive Stats (Chỉ số tấn công)")]
        [Tooltip("Sát thương vật lý cơ bản")]
        [SerializeField] protected float physicalDamage = 20f;

        [Tooltip("Sát thương phép cơ bản")]
        [SerializeField] protected float magicDamage = 10f;

        [Tooltip("Tốc độ đánh (%) cộng thêm")]
        [SerializeField] protected float attackSpeed = 0f;

        [Tooltip("Tỉ lệ chí mạng (%)")]
        [SerializeField] protected float critRate = 5f;

        [Tooltip("Sát thương chí mạng (%)")]
        [SerializeField] protected float critDamage = 150f;

        [Header("Defensive Stats (Chỉ số phòng ngự)")]
        [Tooltip("Giáp vật lý (giảm sát thương vật lý)")]
        [SerializeField] protected float armor = 0f;

        [Tooltip("Kháng phép (giảm sát thương phép)")]
        [SerializeField] protected float magicResist = 0f;

        [Header("Mobility (Di chuyển)")]
        [Tooltip("Tốc độ di chuyển cơ bản")]
        [SerializeField] protected float moveSpeed = 5f;

        public ResourceStat Health => health;
        public ResourceStat Mana => mana;
        public float PhysicalDamage => physicalDamage;
        public float MagicDamage => magicDamage;
        public float AttackSpeed => attackSpeed;
        public float CritRate => critRate;
        public float CritDamage => critDamage;
        public float Armor => armor;
        public float MagicResist => magicResist;
        public float MoveSpeed => moveSpeed;
        public bool IsDead { get; protected set; }
        public bool IsInvulnerable { get; set; }
        public Transform LastAttacker { get; protected set; }

        public void SetPhysicalDamage(float value) => physicalDamage = Mathf.Max(0f, value);
        public void SetMagicDamage(float value) => magicDamage = Mathf.Max(0f, value);
        public void SetAttackSpeed(float value) => attackSpeed = value;
        public void SetCritRate(float value) => critRate = Mathf.Clamp(value, 0f, 100f);
        public void SetCritDamage(float value) => critDamage = Mathf.Max(100f, value);
        public void SetArmor(float value) => armor = Mathf.Max(0f, value);
        public void SetMagicResist(float value) => magicResist = Mathf.Max(0f, value);
        public void SetMoveSpeed(float value) => moveSpeed = Mathf.Max(0f, value);

        public void ApplyStatsFromData(TopDownGame.Data.NpcStatData statData, int level = 1)
        {
            if (statData == null) return;
            health.SetMaxValue(statData.GetMaxHp(level), true);
            health.SetRegenRate(statData.GetHpRegen(level));

            mana.SetMaxValue(statData.GetMaxMp(level), true);
            mana.SetRegenRate(statData.GetMpRegen(level));

            SetPhysicalDamage(statData.GetPhysicalDamage(level));
            SetMagicDamage(statData.GetMagicDamage(level));
            SetArmor(statData.GetArmor(level));
            SetMagicResist(statData.GetMagicResist(level));
            SetAttackSpeed(statData.GetAttackSpeed(level));
            SetCritRate(statData.GetCritChance(level));
            SetCritDamage(statData.GetCritMultiplier(level));
        }

        public bool HasEnoughMana(float amount) => mana.CurrentValue >= amount;
        public bool ConsumeMana(float amount) => mana.Consume(amount);
        public void RestoreMana(float amount) => mana.Modify(amount);

        public event Action OnDeath;
        public event Action<float, Vector3, Vector3> OnDamaged; // amount, hitPoint, hitDirection

        protected virtual void Awake()
        {
            health.Initialize();
            mana.Initialize();
        }

        protected virtual void Update()
        {
            if (IsDead) return;
            health.Tick(Time.deltaTime);
            mana.Tick(Time.deltaTime);
        }

        public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            TakeDamage(amount, hitPoint, hitDirection, null, false);
        }

        public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, Transform attacker)
        {
            TakeDamage(amount, hitPoint, hitDirection, attacker, false);
        }

        public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, Transform attacker, bool isCrit)
        {
            if (IsDead || IsInvulnerable || amount <= 0f) return;

            if (attacker != null)
            {
                LastAttacker = attacker;
            }

            health.Modify(-amount);
            OnDamaged?.Invoke(amount, hitPoint, hitDirection);

            Vector3 spawnPos = (hitPoint != Vector3.zero) ? hitPoint : (transform.position + Vector3.up * 1.5f);
            FloatingTextManager.Instance.SpawnDamage(amount, spawnPos, this is PlayerStats, isCrit);

            if (health.CurrentValue <= 0f)
            {
                Die();
            }
        }

        public virtual void ResetToFullHealth()
        {
            if (IsDead) return;
            health.ResetToMax();
        }

        public virtual void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            health.Modify(amount);
            FloatingTextManager.Instance.SpawnHeal(amount, transform.position + Vector3.up * 1.5f);
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
