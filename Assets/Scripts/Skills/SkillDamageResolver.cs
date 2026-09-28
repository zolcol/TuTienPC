using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Stats;
using TopDownGame.Data;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Bộ xử lý va chạm và gây sát thương chuẩn hóa theo DATA_CONVENTIONS.md (Mục 0 & Mục 3)
    /// </summary>
    public static class SkillDamageResolver
    {
        private static readonly Collider[] hitBuffer = new Collider[60];
        private static readonly HashSet<IDamageable> hitEntitiesThisCast = new HashSet<IDamageable>();
        private static readonly List<EntityStats> targetsToHealBuffer = new List<EntityStats>(16);

        // Độ cao chuẩn của vùng quét sát thương 3D (tính từ sàn lên 2.0m)
        public const float BoxHeight = 2.0f;

        /// <summary>
        /// <summary>
        /// Kích hoạt quét tác dụng chiêu thức (Gây sát thương kẻ địch hoặc Hồi máu/Buff đồng đội)
        /// </summary>
        public static void CastDamage(Transform caster, EntityStats casterStats, SkillData skill, LayerMask targetLayer, Transform explicitTarget = null, Vector3 explicitTargetPoint = default)
        {
            if (caster == null || skill == null) return;

            hitEntitiesThisCast.Clear();

            // 1. Kỹ năng Hồi phục / Hỗ trợ (Relation == Recover hoặc SkillStyle có "heal")
            if (skill.IsHeal)
            {
                CastHeal(caster, casterStats, skill, explicitTarget);

                // Kích hoạt chiêu phụ kèm theo (nếu có cấu hình trong CSV)
                if (skill.HasSubSkill)
                {
                    SkillData subSkill = SkillDatabase.GetSkill(skill.subSkillId);
                    if (subSkill != null && subSkill.id != skill.id)
                    {
                        CastDamage(caster, casterStats, subSkill, targetLayer, explicitTarget, explicitTargetPoint);
                    }
                }
                return;
            }

            // 2. Tấn công gây sát thương kẻ địch
            float calculatedDamage = skill.CalculateDamage(casterStats);
            float actualWidth = skill.boxWidth > 0f ? skill.boxWidth : 1.6f;
            float actualRange = skill.range > 0f ? skill.range : 3.5f;

            if (skill.HasProjectile || skill.skillType == SkillType.Projectile)
            {
                CastProjectile(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint);
            }
            else
            {
                switch (skill.skillType)
                {
                    case SkillType.StraightRay:
                        CastStraightBox(caster, calculatedDamage, actualRange, actualWidth, targetLayer);
                        break;

                    case SkillType.Sector:
                        float angle = skill.fanAngle > 0f ? skill.fanAngle : 90f;
                        CastSector(caster, calculatedDamage, actualRange, angle, targetLayer);
                        break;

                    case SkillType.Circle:
                        CastCircle(caster, calculatedDamage, actualRange, targetLayer);
                        break;

                    case SkillType.TargetLock:
                        CastTargetLock(caster, calculatedDamage, actualRange, actualWidth, targetLayer, explicitTarget);
                        break;
                }
            }

            // 3. Kích hoạt chiêu phụ (SubSkill) nếu có theo cấu hình CSV (FlySkillId / StartSkillID / HitSkillID)
            if (skill.HasSubSkill)
            {
                SkillData subSkill = SkillDatabase.GetSkill(skill.subSkillId);
                if (subSkill != null && subSkill.id != skill.id)
                {
                    CastDamage(caster, casterStats, subSkill, targetLayer, explicitTarget, explicitTargetPoint);
                }
            }
        }

        /// <summary>
        /// Kích hoạt hồi phục sinh lực cho Bản thân và Đồng đội (Relation == Recover)
        /// </summary>
        public static void CastHeal(Transform caster, EntityStats casterStats, SkillData skill, Transform explicitTarget = null)
        {
            float healAmount = skill.CalculateHeal(casterStats);
            float range = skill.range > 0f ? skill.range : 8.0f;

            bool isCasterPlayer = caster.CompareTag("Player") || caster.GetComponent<TopDownGame.Player.PlayerController>() != null;
            targetsToHealBuffer.Clear();

            // Bản thân người tung chiêu (TargetSelf == true hoặc Relation là Recover/Self)
            EntityStats myStats = casterStats != null ? casterStats : caster.GetComponent<EntityStats>();
            if (myStats != null && (skill.targetSelf || skill.relation == SkillRelation.Recover || skill.relation == SkillRelation.Self))
            {
                targetsToHealBuffer.Add(myStats);
            }

            // Nếu có explicitTarget hợp lệ (đồng minh)
            if (explicitTarget != null)
            {
                EntityStats explicitStats = explicitTarget.GetComponent<EntityStats>() ?? explicitTarget.GetComponentInParent<EntityStats>();
                if (explicitStats != null && !explicitStats.IsDead)
                {
                    bool isExplicitPlayer = explicitStats.CompareTag("Player") || explicitStats.GetComponent<TopDownGame.Player.PlayerController>() != null;
                    if (isCasterPlayer == isExplicitPlayer && !targetsToHealBuffer.Contains(explicitStats))
                    {
                        targetsToHealBuffer.Add(explicitStats);
                    }
                }
            }

            // Quét các đồng minh xung quanh trong phạm vi range
            Vector3 sphereCenter = caster.position + Vector3.up * 1.0f;
            int count = Physics.OverlapSphereNonAlloc(sphereCenter, range, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null || col.gameObject == caster.gameObject) continue;

                EntityStats targetStats = col.GetComponent<EntityStats>() ?? col.GetComponentInParent<EntityStats>();
                if (targetStats == null || targetStats.IsDead) continue;

                bool isTargetPlayer = targetStats.CompareTag("Player") || targetStats.GetComponent<TopDownGame.Player.PlayerController>() != null;

                // Nếu người tung là Player -> đồng minh là Player/Pet; nếu là Quái -> đồng minh là Quái
                if (isCasterPlayer == isTargetPlayer)
                {
                    if (!targetsToHealBuffer.Contains(targetStats))
                    {
                        targetsToHealBuffer.Add(targetStats);
                    }
                }
            }

            // 1. Sinh hiệu ứng đài sen nở / hiệu ứng kết thúc chiêu trên mặt đất từ childId / missile (MissileResID = 306)
            // Chuẩn DATA_CONVENTIONS.md: Missile / Đài sen nở bám theo chân mục tiêu (FlatGround), không có cột slotid xương.
            Transform groundTarget = explicitTarget != null ? explicitTarget : caster;
            string groundEffectPath = "";
            float groundDuration = 5.0f;
            if (skill.childId > 0)
            {
                var missile = MissileDatabase.GetMissile(skill.childId);
                if (missile != null)
                {
                    if (missile.missileResID > 0)
                    {
                        groundEffectPath = EffectDatabase.GetEffectPath(missile.missileResID);
                    }
                    if (missile.lifeTime > 0f)
                    {
                        groundDuration = missile.LifeTimeInSeconds;
                    }
                }
                if (string.IsNullOrEmpty(groundEffectPath))
                {
                    groundEffectPath = EffectDatabase.GetEffectPath(skill.childId);
                }
            }

            if (!string.IsNullOrEmpty(groundEffectPath) && groundTarget != null)
            {
                EffectManager.Instance.SpawnEffectFollowTargetGround(groundEffectPath, groundTarget, groundDuration);
            }

            // 2. Thực hiện hồi máu và hiển thị hiệu ứng Buff trên người từng mục tiêu theo StateEffect.csv
            for (int i = 0; i < targetsToHealBuffer.Count; i++)
            {
                var target = targetsToHealBuffer[i];
                if (target != null && !target.IsDead)
                {
                    float hpBefore = target.Health.CurrentValue;
                    target.Heal(healAmount);
                    float actualHealed = target.Health.CurrentValue - hpBefore;
                    // Debug.Log($"💚 <color=green>[HỒI MÁU]</color> <b>{skill.name}</b> đã hồi cho <b>{target.gameObject.name}</b> +{actualHealed:F0} HP (Máu: {target.Health.CurrentValue:F0}/{target.Health.MaxValue:F0})");

                    // Kích hoạt hiệu ứng hình ảnh (VFX hồi máu / Buff) trên mục tiêu theo stateEffectId cấu hình trong Skill.csv
                    if (skill.stateEffectId > 0)
                    {
                        var stateEffect = TopDownGame.Data.StateEffectDatabase.GetStateEffect(skill.stateEffectId);
                        if (stateEffect != null)
                        {
                            if (!string.IsNullOrEmpty(stateEffect.effectPath1))
                            {
                                int slot1 = stateEffect.slotId1 > 0 ? stateEffect.slotId1 : (int)BoneSlotID.ChestCenter;
                                EffectManager.Instance.SpawnEffectAtSlot(stateEffect.effectPath1, target.transform, slot1, 5.0f, true);
                            }
                            if (!string.IsNullOrEmpty(stateEffect.effectPath2))
                            {
                                int slot2 = stateEffect.slotId2 > 0 ? stateEffect.slotId2 : (int)BoneSlotID.ChestCenter;
                                EffectManager.Instance.SpawnEffectAtSlot(stateEffect.effectPath2, target.transform, slot2, 5.0f, true);
                            }
                            if (!string.IsNullOrEmpty(stateEffect.headResPath))
                            {
                                EffectManager.Instance.SpawnEffectAtSlot(stateEffect.headResPath, target.transform, (int)BoneSlotID.Head, 5.0f, true);
                            }
                        }
                    }
                }
            }
            targetsToHealBuffer.Clear();
        }

        /// <summary>
        /// 1. Quét vùng hộp chữ nhật phía trước (Rộng boxWidth mét, Cao 2.0m, Dài range mét)
        /// </summary>
        public static void CastStraightBox(Transform caster, float damage, float range, float boxWidth, LayerMask targetLayer)
        {
            float halfWidth = boxWidth * 0.5f;
            float halfHeight = BoxHeight * 0.5f;
            float backwardOffset = 0.35f;
            float totalLength = range + backwardOffset;
            float halfLength = totalLength * 0.5f;

            // Tâm Box dịch về sau 0.35m để bao quát cả mục tiêu đứng sát chân / ép sát người
            Vector3 boxCenter = caster.position + (Vector3.up * halfHeight) + (caster.forward * (halfLength - backwardOffset));
            Vector3 halfExtents = new Vector3(halfWidth, halfHeight, halfLength);
            Quaternion boxRotation = caster.rotation;

            int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, halfExtents, hitBuffer, boxRotation, targetLayer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null || col.gameObject == caster.gameObject) continue;

                ApplyDamage(col, damage, col.ClosestPoint(boxCenter), caster.forward, caster);
            }
        }

        /// <summary>
        /// 2. Quét hình quạt 3D trước mặt (Fan/Sector)
        /// </summary>
        public static void CastSector(Transform caster, float damage, float range, float fanAngle, LayerMask targetLayer)
        {
            Vector3 sphereCenter = caster.position + Vector3.up * (BoxHeight * 0.5f);
            int hitCount = Physics.OverlapSphereNonAlloc(sphereCenter, range, hitBuffer, targetLayer);
            float halfAngle = fanAngle > 0f ? fanAngle * 0.5f : 45f;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null || col.gameObject == caster.gameObject) continue;

                // Kiểm tra độ cao tương đối so với chân nhân vật (-0.3m đến +2.2m)
                float relativeY = col.transform.position.y - caster.position.y;
                if (relativeY < -0.3f || relativeY > BoxHeight + 0.3f) continue;

                Vector3 dirToTarget = (col.transform.position - caster.position);
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude <= range * range)
                {
                    float angle = Vector3.Angle(caster.forward, dirToTarget);
                    // Mục tiêu áp sát trong phạm vi 0.8m luôn trúng, xa hơn thì kiểm tra góc quạt
                    if (dirToTarget.sqrMagnitude <= 0.8f * 0.8f || angle <= halfAngle)
                    {
                        ApplyDamage(col, damage, col.ClosestPoint(sphereCenter), dirToTarget.normalized, caster);
                    }
                }
            }
        }

        /// <summary>
        /// 3. Quét vòng tròn nổ 3D xung quanh thân (Circle AOE)
        /// </summary>
        public static void CastCircle(Transform caster, float damage, float range, LayerMask targetLayer)
        {
            Vector3 sphereCenter = caster.position + Vector3.up * (BoxHeight * 0.5f);
            int hitCount = Physics.OverlapSphereNonAlloc(sphereCenter, range, hitBuffer, targetLayer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null || col.gameObject == caster.gameObject) continue;

                float relativeY = col.transform.position.y - caster.position.y;
                if (relativeY < -0.3f || relativeY > BoxHeight + 0.3f) continue;

                Vector3 hitDirection = (col.transform.position - caster.position).normalized;
                ApplyDamage(col, damage, col.ClosestPoint(sphereCenter), hitDirection, caster);
            }
        }

        /// <summary>
        /// 4. Khóa mục tiêu hoặc quét đâm thẳng phía trước (Auto-targeting with forward box fallback)
        /// </summary>
        public static void CastTargetLock(Transform caster, float damage, float range, float boxWidth, LayerMask targetLayer, Transform explicitTarget)
        {
            Transform target = explicitTarget != null ? explicitTarget : FindBestTargetInFront(caster, range, targetLayer, 100f);
            if (target != null && Vector3.Distance(caster.position, target.position) <= range + 0.5f)
            {
                if (target.TryGetComponent<IDamageable>(out var damageable) || (damageable = target.GetComponentInParent<IDamageable>()) != null)
                {
                    if (!hitEntitiesThisCast.Contains(damageable))
                    {
                        hitEntitiesThisCast.Add(damageable);
                        Vector3 hitDirection = (target.position - caster.position).normalized;
                        damageable.TakeDamage(damage, target.position + Vector3.up * 1.0f, hitDirection);
                    }
                    return;
                }
            }

            // Fallback: nếu không khóa được mục tiêu cụ thể, tự động quét vùng hộp thẳng phía trước để đòn đánh không bị trượt vô lý
            CastStraightBox(caster, damage, range, boxWidth, targetLayer);
        }

        /// <summary>
        /// 5. Bắn ra đạn đạo / kiếm khí / phi tiêu bay trong không gian 3D (Missile / Projectile)
        /// Hỗ trợ đa tia đạn (ChildCount, MSGenerate, MSGenerateParam) theo DATA_CONVENTIONS.md
        /// </summary>
        public static void CastProjectile(Transform caster, EntityStats casterStats, SkillData skill, LayerMask targetLayer, Transform explicitTarget = null, Vector3 explicitTargetPoint = default)
        {
            if (caster == null || skill == null) return;

            int childCount = Mathf.Max(1, skill.childCount);
            int msGenerate = skill.msGenerate > 0 ? skill.msGenerate : 1;

            // Bắn tuần tự cách quãng (MSGenerate = 2)
            if (childCount > 1 && msGenerate == 2)
            {
                float delayFrames = CsvParserHelper.ParseFloat(skill.msGenerateParam, 2f);
                if (delayFrames <= 0f) delayFrames = 2f;
                float delaySec = delayFrames / SkillData.LOGIC_GAME_FPS;

                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.StartCoroutine(SpawnSequentialProjectilesCoroutine(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint, childCount, delaySec));
                }
                else
                {
                    SpawnProjectileBurst(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint, childCount);
                }
                return;
            }

            // Bắn đồng loạt (MSGenerate = 1) hoặc Xoay tròn (MSGenerate = 3 / MissileForm = 3)
            SpawnProjectileBurst(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint, childCount);
        }

        private static void SpawnProjectileBurst(Transform caster, EntityStats casterStats, SkillData skill, LayerMask targetLayer, Transform explicitTarget, Vector3 explicitTargetPoint, int count)
        {
            if (caster == null || skill == null) return;

            int missileId = skill.childId > 0 ? skill.childId : skill.id;
            var missile = TopDownGame.Data.MissileDatabase.GetMissile(missileId);
            float spawnOffset = missile != null ? missile.SpawnOffsetDistance : 1.2f;

            // 1. Xác định vị trí đích đến của con trỏ chuột / mục tiêu
            Vector3 targetDestination;
            if (explicitTarget != null)
            {
                targetDestination = explicitTarget.position;
            }
            else if (explicitTargetPoint != default)
            {
                targetDestination = explicitTargetPoint;
            }
            else
            {
                targetDestination = caster.position + caster.forward * (skill.range > 0f ? skill.range : 10f);
            }

            // 2. Khớp xương tay cầm vũ khí (Slot 1: B_RH theo PartSlot.csv)
            Transform weaponSlot = TopDownGame.Data.PartSlotDatabase.GetSlotTransform(caster, skill.slotId > 0 ? skill.slotId : 1);
            Vector3 originPos = caster.position;

            if (weaponSlot != null && weaponSlot != caster)
            {
                originPos = new Vector3(weaponSlot.position.x, caster.position.y, weaponSlot.position.z);
            }

            // Xử lý StartPosType = 2 (tại Target) hoặc 3 (tại HitPoint)
            if (skill.startPosType == TopDownGame.Skills.VfxStartPosType.Target && explicitTarget != null)
            {
                if (missile == null || (int)missile.moveKind == 0)
                {
                    originPos = explicitTarget.position;
                }
            }
            else if (skill.startPosType == TopDownGame.Skills.VfxStartPosType.HitPoint && explicitTargetPoint != default)
            {
                originPos = explicitTargetPoint;
            }

            // 3. Vị trí bắt đầu thực tế của missile (spawnPos có tính offset từ vị trí vũ khí)
            Vector3 spawnPos = originPos + caster.forward * spawnOffset;

            // 4. Hướng bay của missile: Tính trực tiếp từ vị trí bắt đầu (spawnPos) đến đích con trỏ chuột (targetDestination)
            // Không còn tính theo người chơi, triệt tiêu hoàn toàn góc lệch song song do offset vũ khí
            Vector3 toDest = targetDestination - spawnPos;
            toDest.y = 0f; // Duy trì mặt phẳng ngang chuẩn OXZ để quỹ đạo ổn định, không chúi xuống sàn

            // Đảm bảo hướng bắn luôn hướng về phía trước mặt nhân vật (tránh trường hợp click quá gần chân hoặc đã lướt qua điểm đích)
            if (Vector3.Dot(caster.forward, toDest) <= 0.05f)
            {
                toDest = caster.forward;
            }

            Vector3 aimDirection = caster.forward;
            if (toDest.sqrMagnitude > 0.001f)
            {
                aimDirection = toDest.normalized;
            }

            // 1. Bắn vòng tròn 360 độ (Circular Ring)
            if (skill.missileForm == 3 || skill.msGenerate == 3)
            {
                float angleStep = 360f / Mathf.Max(1, count);
                for (int i = 0; i < count; i++)
                {
                    float currentAngle = i * angleStep;
                    Vector3 shotDir = Quaternion.Euler(0, currentAngle, 0) * aimDirection;
                    Vector3 ringSpawnPos = originPos + shotDir * spawnOffset;
                    SpawnSingleMissileObject(caster, casterStats, skill, missile, ringSpawnPos, shotDir, targetLayer, explicitTarget);
                }
                // Debug.Log($"🚀 <color=cyan>[MISSILE]</color> Đã phóng <b>{count}</b> đạn vòng tròn <b>{skill.name}</b> (Missile ID: {missileId})");
                return;
            }

            // 2. Bắn đơn (1 tia): Bắn thẳng từ spawnPos tới đích con trỏ chuột
            if (count <= 1)
            {
                SpawnSingleMissileObject(caster, casterStats, skill, missile, spawnPos, aimDirection, targetLayer, explicitTarget);
                // Debug.Log($"🚀 <color=cyan>[MISSILE]</color> Đã phóng kiếm khí <b>{skill.name}</b> hướng thẳng tới đích con trỏ chuột (Missile ID: {missileId})");
                return;
            }

            // 3. Bắn chùm rẻ quạt (Fan Spread): Tia trung tâm hướng tới con trỏ, các tia còn lại xòe đều hai bên
            float paramVal = CsvParserHelper.ParseFloat(skill.msGenerateParam, 0f);
            float angleStepSpread = paramVal > 0f ? paramVal : (skill.fanAngle > 0f ? (skill.fanAngle / Mathf.Max(1, count - 1)) : 15f);
            float startAngle = -(count - 1) * 0.5f * angleStepSpread;

            for (int i = 0; i < count; i++)
            {
                float offsetAngle = startAngle + (i * angleStepSpread);
                Vector3 shotDir = Quaternion.Euler(0, offsetAngle, 0) * aimDirection;
                SpawnSingleMissileObject(caster, casterStats, skill, missile, spawnPos, shotDir, targetLayer, explicitTarget);
            }
            // Debug.Log($"🚀 <color=cyan>[MISSILE]</color> Đã phóng chùm <b>{count}</b> tia <b>{skill.name}</b> (Missile ID: {missileId}, Góc xòe: {angleStepSpread:F1}°)");
        }

        private static void SpawnSingleMissileObject(
            Transform caster,
            EntityStats casterStats,
            SkillData skill,
            TopDownGame.Data.MissileData missile,
            Vector3 spawnPos,
            Vector3 direction,
            LayerMask targetLayer,
            Transform explicitTarget)
        {
            ProjectileController controller = ProjectilePool.Instance.Get();
            controller.Launch(caster, casterStats, skill, missile, spawnPos, direction, targetLayer, explicitTarget);
        }

        private static System.Collections.IEnumerator SpawnSequentialProjectilesCoroutine(
            Transform caster,
            EntityStats casterStats,
            SkillData skill,
            LayerMask targetLayer,
            Transform explicitTarget,
            Vector3 explicitTargetPoint,
            int count,
            float delaySec)
        {
            int missileId = skill.childId > 0 ? skill.childId : skill.id;
            var missile = TopDownGame.Data.MissileDatabase.GetMissile(missileId);
            float spawnOffset = missile != null ? missile.SpawnOffsetDistance : 1.2f;

            for (int i = 0; i < count; i++)
            {
                if (caster == null) yield break;

                Transform weaponSlot = TopDownGame.Data.PartSlotDatabase.GetSlotTransform(caster, skill.slotId > 0 ? skill.slotId : 1);
                Vector3 originPos = caster.position;

                if (weaponSlot != null && weaponSlot != caster)
                {
                    originPos = new Vector3(weaponSlot.position.x, caster.position.y, weaponSlot.position.z);
                }

                // Xử lý StartPosType = 2 (tại Target) hoặc 3 (tại HitPoint)
                if (skill.startPosType == TopDownGame.Skills.VfxStartPosType.Target && explicitTarget != null)
                {
                    if (missile == null || (int)missile.moveKind == 0)
                    {
                        originPos = explicitTarget.position;
                    }
                }
                else if (skill.startPosType == TopDownGame.Skills.VfxStartPosType.HitPoint && explicitTargetPoint != default)
                {
                    originPos = explicitTargetPoint;
                }

                Vector3 targetDestination;
                if (explicitTarget != null)
                {
                    targetDestination = explicitTarget.position;
                }
                else if (explicitTargetPoint != default)
                {
                    targetDestination = explicitTargetPoint;
                }
                else
                {
                    targetDestination = caster.position + caster.forward * (skill.range > 0f ? skill.range : 10f);
                }

                Vector3 spawnPos = originPos + caster.forward * spawnOffset;
                Vector3 toDest = targetDestination - spawnPos;
                toDest.y = 0f;

                if (Vector3.Dot(caster.forward, toDest) <= 0.05f)
                {
                    toDest = caster.forward;
                }

                Vector3 aimDirection = toDest.sqrMagnitude > 0.001f ? toDest.normalized : caster.forward;

                SpawnSingleMissileObject(caster, casterStats, skill, missile, spawnPos, aimDirection, targetLayer, explicitTarget);

                if (i < count - 1 && delaySec > 0f)
                {
                    yield return new WaitForSeconds(delaySec);
                }
            }
        }

        public static bool ApplyDamage(Collider col, float damage, Vector3 hitPoint, Vector3 hitDirection, Transform caster, HashSet<IDamageable> hitFilter = null)
        {
            if (col == null || (caster != null && col.gameObject == caster.gameObject)) return false;

            bool isCasterPlayer = caster != null && (caster.CompareTag("Player") || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
            bool isTargetPlayer = col.CompareTag("Player") || col.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;

            // Không gây sát thương lên đồng minh (Player không đánh Player/Pet, Quái không tự đánh Quái)
            if (isCasterPlayer == isTargetPlayer) return false;

            if (col.TryGetComponent<IDamageable>(out var damageable) || (damageable = col.GetComponentInParent<IDamageable>()) != null)
            {
                var filter = hitFilter ?? hitEntitiesThisCast;
                if (!filter.Contains(damageable))
                {
                    filter.Add(damageable);
                    damageable.TakeDamage(damage, hitPoint, hitDirection);
                    return true;
                }
            }
            return false;
        }

        public static bool ApplyDamage(Collider col, float damage, Vector3 hitPoint, Vector3 hitDirection, Transform caster, Dictionary<IDamageable, float> hitTracker, float repeatInterval = 0f)
        {
            if (col == null || (caster != null && col.gameObject == caster.gameObject)) return false;

            bool isCasterPlayer = caster != null && (caster.CompareTag("Player") || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
            bool isTargetPlayer = col.CompareTag("Player") || col.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;

            if (isCasterPlayer == isTargetPlayer) return false;

            if (col.TryGetComponent<IDamageable>(out var damageable) || (damageable = col.GetComponentInParent<IDamageable>()) != null)
            {
                if (hitTracker != null)
                {
                    if (hitTracker.TryGetValue(damageable, out float lastTime))
                    {
                        if (repeatInterval <= 0f || (Time.time - lastTime) < repeatInterval)
                        {
                            return false;
                        }
                    }
                    hitTracker[damageable] = Time.time;
                }

                damageable.TakeDamage(damage, hitPoint, hitDirection);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tìm mục tiêu gần nhất trước mặt trong góc quét
        /// </summary>
        public static Transform FindBestTargetInFront(Transform caster, float maxRange, LayerMask targetLayer, float maxAngle = 100f)
        {
            Vector3 sphereCenter = caster.position + Vector3.up * (BoxHeight * 0.5f);
            int count = Physics.OverlapSphereNonAlloc(sphereCenter, maxRange, hitBuffer, targetLayer);
            Transform bestTarget = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null || col.gameObject == caster.gameObject) continue;

                Vector3 dirToTarget = (col.transform.position - caster.position);
                dirToTarget.y = 0f;
                float dist = dirToTarget.magnitude;

                if (dist <= maxRange)
                {
                    float angle = Vector3.Angle(caster.forward, dirToTarget);
                    if (angle <= maxAngle * 0.5f && dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = col.transform;
                    }
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Vẽ Gizmo hiển thị vùng sát thương 3D trực quan trong Scene View
        /// </summary>
        public static void DrawGizmo(SkillType type, Vector3 origin, Vector3 forward, float range, float fanAngle, float boxWidth = 1.6f)
        {
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.85f);

            switch (type)
            {
                case SkillType.StraightRay:
                case SkillType.TargetLock:
                    float halfHeight = BoxHeight * 0.5f;
                    float backwardOffset = 0.35f;
                    float totalLength = range + backwardOffset;
                    float halfLength = totalLength * 0.5f;
                    Vector3 boxCenter = origin + (Vector3.up * halfHeight) + (forward * (halfLength - backwardOffset));
                    Vector3 boxSize = new Vector3(boxWidth > 0f ? boxWidth : 1.6f, BoxHeight, totalLength);

                    Matrix4x4 oldMatrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(boxCenter, Quaternion.LookRotation(forward), Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, boxSize);
                    Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.25f);
                    Gizmos.DrawCube(Vector3.zero, boxSize);
                    Gizmos.matrix = oldMatrix;
                    break;

                case SkillType.Sector:
                    float halfAngle = fanAngle > 0f ? fanAngle * 0.5f : 45f;
                    Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
                    Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;

                    Vector3 btmCenter = origin + Vector3.up * 0.05f;
                    Vector3 topCenter = origin + Vector3.up * BoxHeight;

                    Gizmos.DrawLine(btmCenter, btmCenter + leftDir * range);
                    Gizmos.DrawLine(btmCenter, btmCenter + rightDir * range);
                    Gizmos.DrawLine(topCenter, topCenter + leftDir * range);
                    Gizmos.DrawLine(topCenter, topCenter + rightDir * range);

                    Gizmos.DrawLine(btmCenter, topCenter);
                    Gizmos.DrawLine(btmCenter + leftDir * range, topCenter + leftDir * range);
                    Gizmos.DrawLine(btmCenter + rightDir * range, topCenter + rightDir * range);
                    Gizmos.DrawLine(btmCenter + forward * range, topCenter + forward * range);
                    break;

                case SkillType.Circle:
                    Vector3 circCenter = origin + Vector3.up * (BoxHeight * 0.5f);
                    Gizmos.DrawWireSphere(circCenter, range);
                    break;

                case SkillType.Projectile:
                    Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
                    Vector3 pStart = origin + Vector3.up * 1.0f;
                    Vector3 pEnd = pStart + forward * range;
                    Gizmos.DrawLine(pStart, pEnd);
                    Gizmos.DrawWireSphere(pStart, 0.4f);
                    Gizmos.DrawWireSphere(pEnd, 0.5f);
                    break;
            }
        }
    }
}
