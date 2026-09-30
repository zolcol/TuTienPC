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
        private static readonly Collider[] overlapBuffer = new Collider[16];

        [Header("Runtime Info")]
        [SerializeField] private int skillId;
        [SerializeField] private int missileId;
        [SerializeField] private float speed;
        [SerializeField] private float collisionRadius;
        [SerializeField] private float damage;

        public float Speed => speed;
        public float CollisionRadius => collisionRadius;

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
        private GameObject flyingEffectInstance;
        private readonly Dictionary<IDamageable, float> lastHitTimes = new Dictionary<IDamageable, float>();
        private int bouncesRemaining = 0;
        private float bounceRadius = 10f;
        private bool isDestroying = false;

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
            this.homingTarget = explicitTarget;

            this.skillId = skill != null ? skill.id : 0;
            this.missileId = missile != null ? missile.missileId : 0;
            this.damage = (skill != null && skill.IsHeal) ? skill.CalculateHeal(casterStats) : (skill != null ? skill.CalculateDamage(casterStats) : 20f);

            this.moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : caster.forward;
            this.speed = missile != null ? missile.SpeedInUnitsPerSec : 10f;
            this.collisionRadius = missile != null ? missile.CollisionRadius : 0.6f;
            this.maxDistance = skill != null && skill.range > 0f ? skill.range : (missile != null && missile.dmgRange > 0f ? (missile.dmgRange / 10f) : 15f);
            this.lifeTimer = missile != null ? missile.LifeTimeInSeconds : 2.5f;
            this.distanceTraveled = 0f;
            this.flyTimer = 0f;

            // Thiết lập nảy bật liên hoàn (MissileForm = 4 theo DATA_CONVENTIONS.md Mục 2)
            this.bouncesRemaining = (skill != null && skill.missileForm == 4 && skill.skillParam1 > 0) ? (int)skill.skillParam1 : 0;
            this.bounceRadius = (skill != null && skill.skillParam3 > 0) ? (skill.skillParam3 / 100f) : 10f;

            transform.position = startPos;
            transform.rotation = Quaternion.LookRotation(this.moveDirection);

            // Nạp và gắn hiệu ứng đạn bay (MissileResID)
            string flyEffect = missile != null ? missile.FlyEffectPath : "";
            if (!string.IsNullOrEmpty(flyEffect))
            {
                flyingEffectInstance = EffectManager.Instance.SpawnEffect(flyEffect, transform.position, transform.rotation, transform, 0f);
            }

            // Phát âm thanh đạn bay nếu có cấu hình (FlySoundID)
            if (missile != null && missile.flySoundID > 0)
            {
                SoundManager.Instance.PlaySound(missile.flySoundID, transform);
            }

            isInitialized = true;
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

            // 0. Chu kỳ kích hoạt Sub-Skill theo nhịp đạn bay (FlySkillId & FlyEventInterval)
            if (skillData != null && skillData.HasFlySkill && skillData.flyEventInterval > 0)
            {
                flyTimer += dt;
                float intervalSec = skillData.FlyEventIntervalInSeconds;
                if (intervalSec > 0f && flyTimer >= intervalSec)
                {
                    flyTimer -= intervalSec;
                    TriggerFlySkill();
                }
            }

            // 1. Tự động bẻ lái uốn lượn bám theo mục tiêu (Homing / Target tracking theo DATA_CONVENTIONS.md: MoveKind = 2)
            bool isHoming = missileData != null && (missileData.moveKind == MissileMoveKind.HomingTracking || (int)missileData.moveKind == 2 || missileData.isFollowTarget);
            if (isHoming && homingTarget != null)
            {
                Vector3 toTarget = (homingTarget.position - transform.position);
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    moveDirection = Vector3.RotateTowards(moveDirection, toTarget.normalized, 8f * dt, 0f);
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
            }

            // 1.5 Gia tốc tăng tốc khi bay (Acceleration = AcceSpeed / 10.0f m/s^2)
            if (missileData != null && missileData.acceSpeed > 0f)
            {
                speed += missileData.AccelerationInUnitsPerSec2 * dt;
            }

            // 2. Tính toán bước di chuyển & va chạm
            float stepDistance = speed * dt;
            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + moveDirection * stepDistance;

            // 3. Quét va chạm liên tục (Continuous Collision Detection)
            if (CheckCollision(currentPos, nextPos, stepDistance))
            {
                return;
            }

            // 4. Cập nhật vị trí
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

        private bool CheckCollision(Vector3 fromPos, Vector3 toPos, float stepDistance)
        {
            Vector3 castOrigin = fromPos + Vector3.up * HitHeightOffset;
            Vector3 targetCenter = toPos + Vector3.up * HitHeightOffset;

            // A. Quét hình cầu dọc đường đạn (SphereCast) khi đạn di chuyển
            if (stepDistance > 0.001f)
            {
                int hitCount = Physics.SphereCastNonAlloc(castOrigin, collisionRadius, moveDirection, raycastBuffer, stepDistance, targetLayer);
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = raycastBuffer[i];
                    if (hit.collider == null) continue;

                    Vector3 hitPoint = hit.point != Vector3.zero ? hit.point : CombatFormula.GetSafeClosestPoint(hit.collider, castOrigin);
                    if (hitPoint == Vector3.zero) hitPoint = targetCenter;

                    if (ProcessHit(hit.collider, hitPoint, moveDirection))
                    {
                        // Cơ chế nảy đạn liên hoàn (ChainBouncing)
                        if (bouncesRemaining > 0)
                        {
                            Transform nextTarget = FindNextBounceTarget(hitPoint, hit.collider);
                            if (nextTarget != null)
                            {
                                bouncesRemaining--;
                                homingTarget = nextTarget;
                                Vector3 toNext = (nextTarget.position - transform.position);
                                toNext.y = 0f;
                                moveDirection = toNext.sqrMagnitude > 0.001f ? toNext.normalized : moveDirection;
                                transform.position = new Vector3(hitPoint.x, fromPos.y, hitPoint.z);
                                transform.rotation = Quaternion.LookRotation(moveDirection);
                                distanceTraveled = 0f;
                                return false; // Tiếp tục bay sang mục tiêu kế tiếp
                            }
                        }

                        // Kiểm tra xem đạn có tự hủy khi trúng đích hay tiếp tục xuyên thấu
                        bool shouldVanish = missileData != null && missileData.ShouldVanishOnHit;
                        if (shouldVanish)
                        {
                            ExplodeAndDestroy(hitPoint, true);
                            return true;
                        }
                    }
                }
            }

            // B. Quét điểm tiếp xúc tại đích (OverlapSphere fallback)
            int overlapCount = Physics.OverlapSphereNonAlloc(targetCenter, collisionRadius, overlapBuffer, targetLayer);
            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null) continue;

                Vector3 hitPoint = CombatFormula.GetSafeClosestPoint(col, targetCenter);
                if (hitPoint == Vector3.zero) hitPoint = targetCenter;

                if (ProcessHit(col, hitPoint, moveDirection))
                {
                    // Cơ chế nảy đạn liên hoàn (ChainBouncing)
                    if (bouncesRemaining > 0)
                    {
                        Transform nextTarget = FindNextBounceTarget(hitPoint, col);
                        if (nextTarget != null)
                        {
                            bouncesRemaining--;
                            homingTarget = nextTarget;
                            Vector3 toNext = (nextTarget.position - transform.position);
                            toNext.y = 0f;
                            moveDirection = toNext.sqrMagnitude > 0.001f ? toNext.normalized : moveDirection;
                            transform.position = new Vector3(hitPoint.x, fromPos.y, hitPoint.z);
                            transform.rotation = Quaternion.LookRotation(moveDirection);
                            distanceTraveled = 0f;
                            return false;
                        }
                    }

                    // Kiểm tra xem đạn có tự hủy khi trúng đích hay tiếp tục xuyên thấu
                    bool shouldVanish = missileData != null && missileData.ShouldVanishOnHit;
                    if (shouldVanish)
                    {
                        ExplodeAndDestroy(hitPoint, true);
                        return true;
                    }
                }
            }

            return false;
        }

        private Transform FindNextBounceTarget(Vector3 origin, Collider currentHit)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, bounceRadius, overlapBuffer, targetLayer);
            Transform best = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null || col == currentHit) continue;
                if (caster != null && col.gameObject == caster.gameObject) continue;

                if (col.TryGetComponent<IDamageable>(out var dmg) || (dmg = col.GetComponentInParent<IDamageable>()) != null)
                {
                    if (dmg is EntityStats stats && stats.IsDead) continue;
                    if (dmg is Component comp)
                    {
                        bool isCasterPlayer = caster != null && (caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
                        bool isTargetPlayer = comp.CompareTag(CombatLayersAndTags.TagPlayer) || comp.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;
                        if (isCasterPlayer == isTargetPlayer) continue;

                        float dist = Vector3.Distance(origin, comp.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            best = comp.transform;
                        }
                    }
                }
            }

            return best;
        }

        private bool ProcessHit(Collider col, Vector3 hitPoint, Vector3 hitDir)
        {
            if (col == null || (caster != null && col.gameObject == caster.gameObject)) return false;

            // Nhịp lặp sát thương (DmgInterval tính theo chuẩn 15 FPS: DmgInterval / 15.0f)
            float repeatInterval = (missileData != null && missileData.canRepeatDmg && missileData.dmgInterval > 0f)
                ? (missileData.dmgInterval / 15.0f)
                : 0f;

            bool isHitSuccess = false;

            if (skillData != null && skillData.IsHeal)
            {
                bool isCasterPlayer = caster != null && (caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
                bool isTargetPlayer = col.CompareTag(CombatLayersAndTags.TagPlayer) || col.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;

                if (isCasterPlayer == isTargetPlayer)
                {
                    var targetStats = col.GetComponent<EntityStats>() ?? col.GetComponentInParent<EntityStats>();
                    if (targetStats != null && !targetStats.IsDead)
                    {
                        if (lastHitTimes.TryGetValue(targetStats, out float lastTime))
                        {
                            if (repeatInterval <= 0f || (Time.time - lastTime) < repeatInterval)
                            {
                                return false;
                            }
                        }
                        lastHitTimes[targetStats] = Time.time;
                        targetStats.Heal(damage);
                        isHitSuccess = true;
                    }
                }
            }
            else
            {
                isHitSuccess = SkillDamageResolver.ApplyDamage(col, damage, hitPoint, hitDir, caster, lastHitTimes, repeatInterval);
            }

            if (isHitSuccess)
            {
                // Hiệu ứng nổ / va chạm khi trúng đích (CollResID)
                SpawnHitEffect(hitPoint);

                // Âm thanh khi chạm trúng đích (CollSoundID)
                PlayHitSound(hitPoint);

                // Kích hoạt chiêu phụ khi đánh trúng mục tiêu (HitSkillID)
                if (skillData != null && skillData.HasHitSkill)
                {
                    SkillData hitSkill = SkillDatabase.GetSkill(skillData.hitSkillId);
                    if (hitSkill != null && hitSkill.id != skillData.id)
                    {
                        if (hitSkill.HasSound && SoundManager.Instance != null)
                        {
                            SoundManager.Instance.PlaySkillSound(hitSkill, col.transform);
                        }
                        SkillDamageResolver.CastDamage(caster, casterStats, hitSkill, targetLayer, col.transform, hitPoint);
                    }
                }

                return true;
            }

            return false;
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

        private void SpawnHitEffect(Vector3 hitPoint)
        {
            string hitEffectPath = missileData != null ? missileData.HitEffectPath : "";
            if (!string.IsNullOrEmpty(hitEffectPath))
            {
                EffectManager.Instance.SpawnEffect(hitEffectPath, hitPoint, Quaternion.identity, null, 2.0f);
            }
        }

        private void PlayHitSound(Vector3 hitPoint)
        {
            int soundId = missileData != null ? missileData.collSoundID : -1;
            if (soundId > 0)
            {
                SoundManager.Instance.PlaySoundAtPosition(soundId, hitPoint);
            }
        }

        private void ExplodeAndDestroy(Vector3 explosionPos, bool hasHitTarget = true)
        {
            if (!isInitialized || isDestroying) return;
            isDestroying = true;
            isInitialized = false;

            // Kích hoạt chiêu phụ khi đạn tan biến / hết hạn (VanishedSkillId)
            if (skillData != null && skillData.HasVanishedSkill)
            {
                SkillData vanishSkill = SkillDatabase.GetSkill(skillData.vanishedSkillId);
                if (vanishSkill != null && vanishSkill.id != skillData.id)
                {
                    if (vanishSkill.HasSound && SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlaySoundAtPosition(vanishSkill.playsound, explosionPos);
                    }
                    SkillDamageResolver.CastDamage(caster, casterStats, vanishSkill, targetLayer, homingTarget, explosionPos);
                }
            }

            // Nếu đạn tan biến khi hết tầm bay mà không chạm ai, sinh hiệu ứng VanishResID nếu có
            if (!hasHitTarget && missileData != null && !string.IsNullOrEmpty(missileData.VanishEffectPath))
            {
                EffectManager.Instance.SpawnEffect(missileData.VanishEffectPath, explosionPos, Quaternion.identity, null, 1.5f);
            }

            float delaySec = (missileData != null && missileData.delayDeleteFrame > 0f)
                ? (missileData.delayDeleteFrame / 15.0f)
                : 0f;

            if (delaySec > 0.01f)
            {
                // Dừng phát hạt mới để vệt khói/hiệu ứng tan tự nhiên
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
            flyTimer = 0f;
            lastHitTimes.Clear();

            ProjectilePool.Instance.Release(this);
        }

        /// <summary>
        /// Độ cao tâm quả cầu va chạm: Lấy bằng chính bán kính va chạm (collisionRadius)
        /// để đáy quả cầu luôn tiếp xúc vừa chạm mặt đất (Y_bottom = 0), không bị chìm xuống lòng đất
        /// </summary>
        public float HitHeightOffset => collisionRadius > 0f ? collisionRadius : 0.6f;

        private void OnDrawGizmos()
        {
            if (!isInitialized) return;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Vector3 center = transform.position + Vector3.up * HitHeightOffset;
            Gizmos.DrawWireSphere(center, collisionRadius);
            Gizmos.DrawLine(center, center + moveDirection * 1.5f);
        }
    }
}
