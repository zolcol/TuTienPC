using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Data;
using TopDownGame.Stats;
using TopDownGame.Audio;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Điều khiển đường bay, phát hiện va chạm và kích nổ đạn đạo (Missile / Projectile)
    /// Chuẩn hóa theo DATA_CONVENTIONS.md (Mục 0 & Mục 3) và Missile.csv
    /// </summary>
    public class ProjectileController : MonoBehaviour
    {
        private static readonly RaycastHit[] raycastBuffer = new RaycastHit[16];
        private static readonly Collider[] overlapBuffer = new Collider[32];
        private static readonly Collider[] bounceBuffer = new Collider[32];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            System.Array.Clear(raycastBuffer, 0, raycastBuffer.Length);
            System.Array.Clear(overlapBuffer, 0, overlapBuffer.Length);
            System.Array.Clear(bounceBuffer, 0, bounceBuffer.Length);
        }

        [Header("Runtime Info")]
        [SerializeField] private int skillId;
        [SerializeField] private int missileId;
        [SerializeField] private float speed;
        [SerializeField] private float collisionRadius;
        [SerializeField] private float damage;

        public float Speed => speed;
        public float CollisionRadius => collisionRadius;
        public float HitHeightOffset => collisionRadius > 0f ? collisionRadius : 0.6f;

        private Transform caster;
        private EntityStats casterStats;
        private SkillData skillData;
        private MissileData missileData;
        private LayerMask targetLayer;
        private Transform homingTarget;

        private Vector3 moveDirection;
        private float distanceTraveled;
        private float maxDistance;
        private float lifeTimer;
        private float flyTimer;
        private bool isInitialized = false;
        private bool isDestroying = false;
        private GameObject flyingEffectInstance;
        private readonly Dictionary<IDamageable, float> lastHitTimes = new Dictionary<IDamageable, float>();
        private readonly Dictionary<IDamageable, int> targetHitCounts = new Dictionary<IDamageable, int>();
        private IDamageable lastHitEntity = null;
        private int bouncesRemaining = 0;
        private float bounceRadius = 10f;
        private int maxHitsPerTarget = 0;

        #region Lifecycle & Initialization

        public void Launch(
            Transform caster,
            EntityStats casterStats,
            SkillData skill,
            MissileData missile,
            Vector3 startPos,
            Vector3 direction,
            LayerMask targetLayer,
            Transform explicitTarget = null)
        {
            this.caster = caster;
            this.casterStats = casterStats;
            this.skillData = skill;
            this.missileData = missile;
            this.targetLayer = targetLayer;

            this.skillId = skill != null ? skill.id : 0;
            this.missileId = missile != null ? missile.missileId : 0;
            this.damage = (skill != null && skill.IsHeal) 
                ? skill.CalculateHeal(casterStats) 
                : (skill != null ? skill.CalculateDamage(casterStats) : 20f);

            this.moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : (caster != null ? caster.forward : Vector3.forward);
            this.speed = missile != null ? missile.SpeedInUnitsPerSec : 10f;
            this.collisionRadius = missile != null ? missile.CollisionRadius : 0.6f;
            this.lifeTimer = missile != null ? missile.LifeTimeInSeconds : 2.5f;
            this.maxDistance = (skill != null && skill.range > 0f) 
                ? skill.range 
                : (this.speed > 0f ? (this.speed * this.lifeTimer) : 15f);
            this.distanceTraveled = 0f;
            this.flyTimer = 0f;

            // Reset lịch sử va chạm cho lượt bắn mới
            this.lastHitTimes.Clear();
            this.targetHitCounts.Clear();

            // Xác định chế độ bám mục tiêu (Homing / Bounce)
            bool isBounceMissile = skill != null && skill.missileForm == 4;
            bool isHomingMissile = (missile != null && (missile.moveKind == MissileMoveKind.HomingTracking || (int)missile.moveKind == 2 || missile.isFollowTarget));
            this.homingTarget = (isBounceMissile || isHomingMissile) ? explicitTarget : null;

            // Thiết lập nảy bật liên hoàn (MissileForm = 4)
            this.bouncesRemaining = (isBounceMissile && skill.skillParam1 > 0) ? (int)skill.skillParam1 : 0;
            this.bounceRadius = (isBounceMissile && skill.skillParam3 > 0) ? (skill.skillParam3 / 100f) : 10f;
            this.maxHitsPerTarget = (isBounceMissile && skill.skillParam4 > 0) ? (int)skill.skillParam4 : 0;
            this.lastHitEntity = null;

            transform.position = startPos;
            if (this.moveDirection.sqrMagnitude > 0.001f)
            {
                this.moveDirection.y = 0f;
                this.moveDirection.Normalize();
                transform.rotation = Quaternion.LookRotation(this.moveDirection);
            }

            // Nạp hiệu ứng và âm thanh bay
            string flyEffect = missile != null ? missile.FlyEffectPath : "";
            if (!string.IsNullOrEmpty(flyEffect))
            {
                flyingEffectInstance = EffectManager.Instance.SpawnEffect(flyEffect, transform.position, transform.rotation, transform, 0f);
            }

            if (missile != null && missile.flySoundID > 0)
            {
                SoundManager.Instance.PlaySound(missile.flySoundID, transform);
            }

            isInitialized = true;
            isDestroying = false;
        }

        private void Update()
        {
            if (!isInitialized || isDestroying) return;

            float dt = Time.deltaTime;
            lifeTimer -= dt;
            if (lifeTimer <= 0f)
            {
                ExplodeAndDestroy(transform.position, false);
                return;
            }

            // Kích hoạt Sub-Skill theo nhịp bay
            UpdateFlySkill(dt);

            // Cập nhật gia tốc & bẻ lái Homing
            UpdateTrajectory(dt);

            // Tính toán bước di chuyển & kiểm tra va chạm
            float stepDistance = speed * dt;
            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + moveDirection * stepDistance;

            if (CheckCollision(currentPos, nextPos, stepDistance))
            {
                return;
            }

            // Di chuyển tiếp nếu chưa chạm đích
            if (speed > 0f)
            {
                transform.position = nextPos;
                distanceTraveled += stepDistance;

                if (distanceTraveled >= maxDistance)
                {
                    ExplodeAndDestroy(nextPos, false);
                }
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (flyingEffectInstance != null)
            {
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.RecycleEffect(flyingEffectInstance);
                }
                flyingEffectInstance = null;
            }
            isInitialized = false;
            isDestroying = false;
        }

        #endregion

        #region Trajectory & Movement

        private void UpdateTrajectory(float dt)
        {
            // Bẻ lái bám mục tiêu (Homing / Target tracking hoặc Nảy đạn liên hoàn)
            bool isHoming = (missileData != null && (missileData.moveKind == MissileMoveKind.HomingTracking || (int)missileData.moveKind == 2 || missileData.isFollowTarget))
                || (skillData != null && skillData.missileForm == 4);

            if (isHoming && homingTarget != null)
            {
                var targetStats = homingTarget.GetComponentInParent<EntityStats>();
                if (targetStats != null && targetStats.IsDead)
                {
                    if (skillData != null && skillData.missileForm == 4)
                    {
                        // Mục tiêu đang bay tới đã chết -> Tìm ngay mục tiêu kế tiếp hoặc nổ
                        Transform newTarget = FindNextBounceTarget(transform.position, lastHitEntity, null);
                        if (newTarget != null)
                        {
                            homingTarget = newTarget;
                        }
                        else
                        {
                            ExplodeAndDestroy(transform.position, false);
                            return;
                        }
                    }
                    else
                    {
                        // Đạn Homing thường: Mục tiêu đã chết -> Bỏ khóa mục tiêu, bay tiếp thẳng theo quán tính
                        homingTarget = null;
                    }
                }
                else
                {
                    Vector3 toTarget = homingTarget.position - transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        float turnSpeed = (skillData != null && skillData.missileForm == 4) ? 25f : 8f;
                        moveDirection = Vector3.RotateTowards(moveDirection, toTarget.normalized, turnSpeed * dt, 0f);
                        if (moveDirection.sqrMagnitude > 0.001f)
                        {
                            transform.rotation = Quaternion.LookRotation(moveDirection);
                        }
                    }
                }
            }

            // Tăng tốc đạn bay nếu có cấu hình gia tốc
            if (missileData != null && missileData.acceSpeed > 0f)
            {
                speed += missileData.AccelerationInUnitsPerSec2 * dt;
            }
        }

        private void UpdateFlySkill(float dt)
        {
            if (skillData == null || !skillData.HasFlySkill || skillData.flyEventInterval <= 0) return;

            flyTimer += dt;
            float intervalSec = skillData.FlyEventIntervalInSeconds;
            if (intervalSec > 0f && flyTimer >= intervalSec)
            {
                flyTimer -= intervalSec;
                TriggerFlySkill();
            }
        }

        #endregion

        #region Collision & Hit Resolution

        private bool IsMatchingHomingTarget(Collider col)
        {
            if (homingTarget == null) return true;
            if (col.transform == homingTarget || col.transform.IsChildOf(homingTarget) || homingTarget.IsChildOf(col.transform))
            {
                return true;
            }

            var targetStats = homingTarget.GetComponentInParent<EntityStats>();
            if (targetStats != null)
            {
                var colStats = col.GetComponentInParent<EntityStats>();
                if (colStats != null && colStats == targetStats)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckCollision(Vector3 fromPos, Vector3 toPos, float stepDistance)
        {
            Vector3 castOrigin = fromPos;
            Vector3 targetCenter = toPos;

            // 1. Quét hình cầu dọc đường bay (SphereCast)
            if (stepDistance > 0.001f)
            {
                int hitCount = Physics.SphereCastNonAlloc(castOrigin, collisionRadius, moveDirection, raycastBuffer, stepDistance, targetLayer);
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = raycastBuffer[i];
                    if (hit.collider == null) continue;

                    Vector3 hitPoint = hit.point != Vector3.zero ? hit.point : CombatFormula.GetSafeClosestPoint(hit.collider, castOrigin);
                    if (hitPoint == Vector3.zero) hitPoint = targetCenter;

                    if (HandleHitTarget(hit.collider, hitPoint, fromPos))
                    {
                        return true;
                    }
                }
            }

            // 2. Quét điểm tiếp xúc tại đích (OverlapSphere fallback)
            int overlapCount = Physics.OverlapSphereNonAlloc(targetCenter, collisionRadius, overlapBuffer, targetLayer);
            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null) continue;

                Vector3 hitPoint = CombatFormula.GetSafeClosestPoint(col, targetCenter);
                if (hitPoint == Vector3.zero) hitPoint = targetCenter;

                if (HandleHitTarget(col, hitPoint, fromPos))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Xử lý trúng mục tiêu, nảy đạn liên hoàn hoặc tự hủy đạn
        /// </summary>
        private bool HandleHitTarget(Collider col, Vector3 hitPoint, Vector3 currentPos)
        {
            if (col == null || (caster != null && (col.gameObject == caster.gameObject || col.transform.IsChildOf(caster)))) return false;

            // Nếu là đạn Homing/Bounce và đã khóa mục tiêu cụ thể, chỉ nhận va chạm đúng mục tiêu đó
            bool isHomingMode = (missileData != null && (missileData.moveKind == MissileMoveKind.HomingTracking || (int)missileData.moveKind == 2 || missileData.isFollowTarget))
                || (skillData != null && skillData.missileForm == 4);
            if (isHomingMode && homingTarget != null && !IsMatchingHomingTarget(col))
            {
                return false;
            }

            IDamageable hitDmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (hitDmg == null) return false;
            if (hitDmg is EntityStats stats && stats.IsDead) return false;

            // Đối với đạn nảy: không va chạm lặp lại với mục tiêu vừa nảy ra
            if (skillData != null && skillData.missileForm == 4 && hitDmg == lastHitEntity)
            {
                return false;
            }

            // Kiểm tra giới hạn số lần đánh lên mục tiêu này
            if (skillData != null && skillData.missileForm == 4 && maxHitsPerTarget > 0)
            {
                if (targetHitCounts.TryGetValue(hitDmg, out int hitCount) && hitCount >= maxHitsPerTarget)
                {
                    return false;
                }
            }

            if (!ProcessHit(col, hitPoint, moveDirection)) return false;

            targetHitCounts[hitDmg] = targetHitCounts.TryGetValue(hitDmg, out int count) ? count + 1 : 1;
            lastHitEntity = hitDmg;

            // Cơ chế nảy đạn liên hoàn (Chain Bouncing - MissileForm = 4)
            if (skillData != null && skillData.missileForm == 4)
            {
                if (bouncesRemaining > 0)
                {
                    Transform nextTarget = FindNextBounceTarget(transform.position, hitDmg, col);
                    if (nextTarget != null)
                    {
                        bouncesRemaining--;
                        homingTarget = nextTarget;
                        Vector3 toNext = nextTarget.position - transform.position;
                        toNext.y = 0f;
                        if (toNext.sqrMagnitude > 0.001f)
                        {
                            moveDirection = toNext.normalized;
                            transform.rotation = Quaternion.LookRotation(moveDirection);
                        }
                        distanceTraveled = 0f;
                        lifeTimer = Mathf.Max(lifeTimer, missileData != null ? missileData.LifeTimeInSeconds : 2.5f);
                        return true; // Đã xử lý va chạm và chuyển hướng nảy thành công
                    }
                }

                // Hết số lần nảy hoặc không tìm thấy mục tiêu nảy hợp lệ trong tầm -> Biến mất
                ExplodeAndDestroy(hitPoint, true);
                return true;
            }

            // Tự hủy khi trúng đích nếu cấu hình VanishOnHit (đạn thông thường)
            if (missileData != null && missileData.ShouldVanishOnHit)
            {
                ExplodeAndDestroy(hitPoint, true);
                return true;
            }

            return false;
        }

        private bool ProcessHit(Collider col, Vector3 hitPoint, Vector3 hitDir)
        {
            if (col == null || (caster != null && col.gameObject == caster.gameObject)) return false;

            float repeatInterval = (missileData != null && missileData.canRepeatDmg && missileData.dmgInterval > 0f)
                ? (missileData.dmgInterval / 15.0f)
                : 0f;

            bool isHitSuccess = false;

            if (skillData != null && skillData.IsHeal)
            {
                isHitSuccess = TryApplyHeal(col, repeatInterval);
            }
            else
            {
                var hitTracker = (skillData != null && skillData.missileForm == 4) ? null : lastHitTimes;
                isHitSuccess = SkillDamageResolver.ApplyDamage(col, damage, hitPoint, hitDir, caster, hitTracker, repeatInterval);
            }

            if (isHitSuccess)
            {
                PlayHitFeedback(hitPoint, col);
                return true;
            }

            return false;
        }

        private bool TryApplyHeal(Collider col, float repeatInterval)
        {
            bool isCasterPlayer = caster != null && (caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
            bool isTargetPlayer = col.CompareTag(CombatLayersAndTags.TagPlayer) || col.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;

            if (isCasterPlayer != isTargetPlayer) return false;

            var targetStats = col.GetComponent<EntityStats>() ?? col.GetComponentInParent<EntityStats>();
            if (targetStats == null || targetStats.IsDead) return false;

            if (skillData == null || skillData.missileForm != 4)
            {
                if (lastHitTimes.TryGetValue(targetStats, out float lastTime))
                {
                    if (repeatInterval <= 0f || (Time.time - lastTime) < repeatInterval)
                    {
                        return false;
                    }
                }
                lastHitTimes[targetStats] = Time.time;
            }

            targetStats.Heal(damage);
            return true;
        }

        private Transform FindNextBounceTarget(Vector3 origin, IDamageable currentHitDamageable, Collider currentHitCol)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, bounceRadius, bounceBuffer, targetLayer);
            Transform best = null;
            int bestHitCount = int.MaxValue;
            float closestDist = float.MaxValue;
            bool isCasterPlayer = caster != null && (caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
            bool isHeal = skillData != null && skillData.IsHeal;

            for (int i = 0; i < count; i++)
            {
                Collider col = bounceBuffer[i];
                if (col == null || col == currentHitCol) continue;
                if (caster != null && (col.gameObject == caster.gameObject || col.transform.IsChildOf(caster))) continue;

                var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (dmg == null || dmg == currentHitDamageable) continue;
                if (dmg is EntityStats stats && stats.IsDead) continue;

                if (dmg is Component comp)
                {
                    bool isTargetPlayer = comp.CompareTag(CombatLayersAndTags.TagPlayer) || comp.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;
                    if (isHeal)
                    {
                        if (isCasterPlayer != isTargetPlayer) continue;
                    }
                    else
                    {
                        if (isCasterPlayer == isTargetPlayer) continue;
                    }

                    int hits = targetHitCounts.TryGetValue(dmg, out int h) ? h : 0;
                    if (maxHitsPerTarget > 0 && hits >= maxHitsPerTarget) continue;

                    float dist = Vector3.Distance(origin, comp.transform.position);

                    if (best == null || hits < bestHitCount || (hits == bestHitCount && dist < closestDist))
                    {
                        best = comp.transform;
                        bestHitCount = hits;
                        closestDist = dist;
                    }
                }
            }

            return best;
        }

        #endregion

        #region Sub-Skills & Feedback

        private void PlayHitFeedback(Vector3 hitPoint, Collider hitCol)
        {
            // VFX trúng đích
            if (missileData != null && !string.IsNullOrEmpty(missileData.HitEffectPath))
            {
                EffectManager.Instance.SpawnEffect(missileData.HitEffectPath, hitPoint, Quaternion.identity, null, 2.0f);
            }

            // Âm thanh trúng đích
            if (missileData != null && missileData.collSoundID > 0)
            {
                SoundManager.Instance.PlaySoundAtPosition(missileData.collSoundID, hitPoint);
            }

            // Kích hoạt chiêu phụ trúng đích (HitSkillID)
            if (skillData != null && skillData.HasHitSkill)
            {
                SkillData hitSkill = SkillDatabase.GetSkill(skillData.hitSkillId);
                if (hitSkill != null && hitSkill.id != skillData.id)
                {
                    if (hitSkill.HasSound && SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlaySkillSound(hitSkill, hitCol.transform);
                    }
                    SkillDamageResolver.CastDamage(caster, casterStats, hitSkill, targetLayer, hitCol.transform, hitPoint);
                }
            }
        }

        private void TriggerFlySkill()
        {
            if (skillData == null || skillData.flySkillId <= 0) return;
            SkillData flySkill = SkillDatabase.GetSkill(skillData.flySkillId);
            if (flySkill == null || flySkill.id == skillData.id) return;

            Transform target = homingTarget != null ? homingTarget : null;
            if (flySkill.HasSound && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySkillSound(flySkill, target != null ? target : transform);
            }
            Vector3 targetPt = target != null ? target.position : transform.position;
            SkillDamageResolver.CastDamage(caster, casterStats, flySkill, targetLayer, target, targetPt);
        }

        private void TriggerVanishedSkill(Vector3 explosionPos)
        {
            if (skillData == null || !skillData.HasVanishedSkill) return;
            SkillData vanishSkill = SkillDatabase.GetSkill(skillData.vanishedSkillId);
            if (vanishSkill == null || vanishSkill.id == skillData.id) return;

            if (vanishSkill.HasSound && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySoundAtPosition(vanishSkill.playsound, explosionPos);
            }
            SkillDamageResolver.CastDamage(caster, casterStats, vanishSkill, targetLayer, homingTarget, explosionPos);
        }

        #endregion

        #region Explosion & Recycle

        private void ExplodeAndDestroy(Vector3 explosionPos, bool hasHitTarget = true)
        {
            if (!isInitialized || isDestroying) return;
            isDestroying = true;
            isInitialized = false;

            // Kích hoạt chiêu phụ khi đạn tan biến
            TriggerVanishedSkill(explosionPos);

            // VFX tan biến khi hết hạn bay
            if (!hasHitTarget && missileData != null && !string.IsNullOrEmpty(missileData.VanishEffectPath))
            {
                EffectManager.Instance.SpawnEffect(missileData.VanishEffectPath, explosionPos, Quaternion.identity, null, 1.5f);
            }

            float delaySec = (missileData != null && missileData.delayDeleteFrame > 0f)
                ? (missileData.delayDeleteFrame / 15.0f)
                : 0f;

            if (delaySec > 0.01f)
            {
                if (flyingEffectInstance != null)
                {
                    var particles = flyingEffectInstance.GetComponentsInChildren<ParticleSystem>();
                    for (int i = 0; i < particles.Length; i++)
                    {
                        particles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
                StartCoroutine(RecycleAfterDelay(delaySec));
            }
            else
            {
                RecycleImmediately();
            }
        }

        private System.Collections.IEnumerator RecycleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            RecycleImmediately();
        }

        private void RecycleImmediately()
        {
            if (flyingEffectInstance != null)
            {
                EffectManager.Instance.RecycleEffect(flyingEffectInstance);
                flyingEffectInstance = null;
            }

            isInitialized = false;
            isDestroying = false;
            caster = null;
            casterStats = null;
            skillData = null;
            missileData = null;
            homingTarget = null;
            lastHitEntity = null;
            flyTimer = 0f;
            lastHitTimes.Clear();
            targetHitCounts.Clear();
            bouncesRemaining = 0;
            maxHitsPerTarget = 0;

            ProjectilePool.Instance.Release(this);
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (!isInitialized) return;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Vector3 center = transform.position;
            Gizmos.DrawWireSphere(center, collisionRadius);
            Gizmos.DrawLine(center, center + moveDirection * 1.5f);
        }
    }
}
