using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Stats;
using TopDownGame.Data;
using TopDownGame.Audio;

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
        public const float DefaultBoxWidth = 1.6f;
        public const float DefaultRange = 3.5f;
        public const float BackwardOffset = 0.35f;
        public const float RelativeYMin = -0.3f;
        public const float RelativeYMaxOffset = 0.3f;
        public const float CloseTargetThreshold = 0.8f;
        public const float DefaultFanSpreadAngle = 15f;

        public static LayerMask GetTargetLayerForSkill(Transform caster, SkillData skill, LayerMask defaultTargetLayer)
        {
            if (skill == null || caster == null) return defaultTargetLayer;
            if (skill.IsHeal)
            {
                bool isCasterPlayer = caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null;
                return LayerMask.GetMask(isCasterPlayer ? CombatLayersAndTags.LayerPlayer : CombatLayersAndTags.LayerEnemy);
            }
            return defaultTargetLayer;
        }

        /// <summary>
        /// Kích hoạt quét tác dụng chiêu thức (Gây sát thương kẻ địch hoặc Hồi máu/Buff đồng đội)
        /// </summary>
        public static void CastDamage(Transform caster, EntityStats casterStats, SkillData skill, LayerMask targetLayer, Transform explicitTarget = null, Vector3 explicitTargetPoint = default)
        {
            if (caster == null || skill == null) return;

            hitEntitiesThisCast.Clear();
            LayerMask skillTargetLayer = GetTargetLayerForSkill(caster, skill, targetLayer);

            // 0. Kích hoạt chiêu phụ bắt đầu (StartSkillID) nếu có
            if (skill.HasStartSkill)
            {
                SkillData startSkill = SkillDatabase.GetSkill(skill.startSkillId);
                if (startSkill != null && startSkill.id != skill.id)
                {
                    CastDamage(caster, casterStats, startSkill, targetLayer, explicitTarget, explicitTargetPoint);
                }
            }

            // 1. Kỹ năng Hồi phục / Hỗ trợ (Relation == Recover hoặc SkillStyle có "heal")
            if (skill.IsHeal)
            {
                CastHeal(caster, casterStats, skill, explicitTarget, explicitTargetPoint);

                // Kích hoạt chiêu phụ legacy kèm theo (nếu khác start/fly/hit skill)
                if (skill.HasSubSkill && skill.subSkillId != skill.startSkillId && skill.subSkillId != skill.flySkillId && skill.subSkillId != skill.hitSkillId)
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
            float actualWidth = skill.boxWidth > 0f ? skill.boxWidth : DefaultBoxWidth;
            float actualRange = skill.range > 0f ? skill.range : DefaultRange;

            // Xử lý Area DoT cho kỹ năng không phải đạn bay (vd: Skill 312 Thiên Vũ Bảo Luân: MSGenerate=2, ChildCount=12, MSGenerateParam=7)
            if (skill.msGenerate == 2 && skill.childCount > 1 && !skill.HasProjectile && skill.skillType != SkillType.Projectile)
            {
                float delayFrames = CsvParserHelper.ParseFloat(skill.msGenerateParam, 7f);
                if (delayFrames <= 0f) delayFrames = 7f;
                float delaySec = delayFrames / SkillData.LOGIC_GAME_FPS;

                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.StartCoroutine(SpawnAreaDoTHitboxCoroutine(caster, casterStats, skill, skillTargetLayer, explicitTarget, explicitTargetPoint, skill.childCount, delaySec));
                }
                else
                {
                    CastCircle(caster, calculatedDamage, actualRange, skillTargetLayer);
                }
            }
            else if (skill.HasProjectile || skill.skillType == SkillType.Projectile)
            {
                CastProjectile(caster, casterStats, skill, skillTargetLayer, explicitTarget, explicitTargetPoint);
            }
            else
            {
                switch (skill.skillType)
                {
                    case SkillType.StraightRay:
                        CastStraightBox(caster, calculatedDamage, actualRange, actualWidth, skillTargetLayer);
                        break;

                    case SkillType.Sector:
                        float angle = skill.fanAngle > 0f ? skill.fanAngle : 90f;
                        CastSector(caster, calculatedDamage, actualRange, angle, skillTargetLayer);
                        break;

                    case SkillType.Circle:
                        CastCircle(caster, calculatedDamage, actualRange, skillTargetLayer);
                        break;

                    case SkillType.TargetLock:
                        CastTargetLock(caster, calculatedDamage, actualRange, actualWidth, skillTargetLayer, explicitTarget);
                        break;
                }
            }

            // 3. Kích hoạt chiêu phụ legacy nếu có
            if (skill.HasSubSkill && skill.subSkillId != skill.startSkillId && skill.subSkillId != skill.flySkillId && skill.subSkillId != skill.hitSkillId)
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
        public static void CastHeal(Transform caster, EntityStats casterStats, SkillData skill, Transform explicitTarget = null, Vector3 explicitTargetPoint = default, bool isSubSkillTick = false)
        {
            if (caster == null || skill == null) return;

            float healAmount = skill.CalculateHeal(casterStats);
            float range = skill.range > 0f ? skill.range : 8.0f;

            bool isCasterPlayer = caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null;
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
                    bool isExplicitPlayer = explicitStats.CompareTag(CombatLayersAndTags.TagPlayer) || explicitStats.GetComponent<TopDownGame.Player.PlayerController>() != null;
                    if (isCasterPlayer == isExplicitPlayer && !targetsToHealBuffer.Contains(explicitStats))
                    {
                        targetsToHealBuffer.Add(explicitStats);
                    }
                }
            }

            // Quét các đồng minh xung quanh trong phạm vi range
            Vector3 healOrigin = explicitTargetPoint != default ? explicitTargetPoint : (explicitTarget != null ? explicitTarget.position : caster.position);
            Vector3 sphereCenter = healOrigin + Vector3.up * 1.0f;
            int count = Physics.OverlapSphereNonAlloc(sphereCenter, range, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null || col.gameObject == caster.gameObject) continue;

                EntityStats targetStats = col.GetComponent<EntityStats>() ?? col.GetComponentInParent<EntityStats>();
                if (targetStats == null || targetStats.IsDead) continue;

                bool isTargetPlayer = targetStats.CompareTag(CombatLayersAndTags.TagPlayer) || targetStats.GetComponent<TopDownGame.Player.PlayerController>() != null;

                // Nếu người tung là Player -> đồng minh là Player/Pet; nếu là Quái -> đồng minh là Quái
                if (isCasterPlayer == isTargetPlayer)
                {
                    if (!targetsToHealBuffer.Contains(targetStats))
                    {
                        targetsToHealBuffer.Add(targetStats);
                    }
                }
            }

            // 1. Sinh hiệu ứng đài sen nở / hiệu ứng kết thúc chiêu trên mặt đất từ childId / missile (MissileResID = 306, 313)
            // Chuẩn DATA_CONVENTIONS.md: Missile / Đài sen nở bám theo chân mục tiêu (FlatGround), không có cột slotid xương.
            Transform groundTarget = explicitTarget != null ? explicitTarget : caster;
            string groundEffectPath = "";
            float groundDuration = 5.0f;
            TopDownGame.Data.MissileData missile = null;
            if (skill.childId > 0)
            {
                missile = MissileDatabase.GetMissile(skill.childId);
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

            if (!isSubSkillTick && !string.IsNullOrEmpty(groundEffectPath))
            {
                if (missile != null && missile.moveKind == MissileMoveKind.StaticTrap && explicitTargetPoint != default)
                {
                    EffectManager.Instance.SpawnEffect(groundEffectPath, explicitTargetPoint, Quaternion.identity, null, groundDuration);
                }
                else if (groundTarget != null)
                {
                    EffectManager.Instance.SpawnEffectFollowTargetGround(groundEffectPath, groundTarget, groundDuration);
                }
            }

            bool isAreaHeal = (missile != null && missile.canRepeatDmg && missile.dmgInterval > 0f);
            bool isHoT = (skill.HasFlySkill && skill.flyEventInterval > 0);

            // 2. Thực hiện hồi máu và hiển thị hiệu ứng Buff trên người từng mục tiêu theo StateEffect.csv
            // (Nếu là chiêu hồi máu định kỳ DoT/HoT, coroutine bên dưới sẽ điều phối chính xác từng nhịp hồi máu)
            for (int i = 0; i < targetsToHealBuffer.Count; i++)
            {
                var target = targetsToHealBuffer[i];
                if (target != null && !target.IsDead)
                {
                    if (isSubSkillTick || (!isAreaHeal && !isHoT))
                    {
                        target.Heal(healAmount);
                    }

                    // Kích hoạt hiệu ứng hình ảnh (VFX hồi máu / Buff) trên mục tiêu theo stateEffectId cấu hình trong Skill.csv
                    if (!isSubSkillTick && skill.stateEffectId > 0)
                    {
                        var stateEffect = TopDownGame.Data.StateEffectDatabase.GetStateEffect(skill.stateEffectId);
                        if (stateEffect != null)
                        {
                            if (!string.IsNullOrEmpty(stateEffect.effectPath1))
                            {
                                int slot1 = stateEffect.slotId1 > 0 ? stateEffect.slotId1 : (int)BoneSlotID.ChestCenter;
                                EffectManager.Instance.SpawnEffectAtSlot(stateEffect.effectPath1, target.transform, slot1, groundDuration, true);
                            }
                            if (!string.IsNullOrEmpty(stateEffect.effectPath2))
                            {
                                int slot2 = stateEffect.slotId2 > 0 ? stateEffect.slotId2 : (int)BoneSlotID.ChestCenter;
                                EffectManager.Instance.SpawnEffectAtSlot(stateEffect.effectPath2, target.transform, slot2, groundDuration, true);
                            }
                            if (!string.IsNullOrEmpty(stateEffect.headResPath))
                            {
                                EffectManager.Instance.SpawnEffectAtSlot(stateEffect.headResPath, target.transform, (int)BoneSlotID.Head, groundDuration, true);
                            }
                        }
                    }
                }
            }
            targetsToHealBuffer.Clear();

            // 3. Cơ chế hồi máu liên tục theo đợt (Heal Over Time qua FlySkill - vd: Skill 306 gọi 307 mỗi 15 frames)
            if (!isSubSkillTick && isHoT && EffectManager.Instance != null)
            {
                EffectManager.Instance.StartCoroutine(HealOverTimeCoroutine(caster, casterStats, skill, explicitTarget, skill.FlyEventIntervalInSeconds, groundDuration));
            }

            // 4. Cơ chế bãi hồi máu định kỳ cố định (Area Heal DoT qua CanRepeatDmg & DmgInterval - vd: Skill 313)
            if (!isSubSkillTick && isAreaHeal && EffectManager.Instance != null)
            {
                float intervalSec = missile.DmgIntervalInSeconds;
                float durationSec = missile.LifeTimeInSeconds;
                float healRadius = missile.CollisionRadius > 0f ? missile.CollisionRadius : range;
                EffectManager.Instance.StartCoroutine(AreaHealDoTCoroutine(caster, casterStats, skill, healOrigin, healRadius, intervalSec, durationSec, (missile.moveKind != MissileMoveKind.StaticTrap ? groundTarget : null)));
            }
        }

        /// <summary>
        /// 1. Quét vùng hộp chữ nhật phía trước (Rộng boxWidth mét, Cao 2.0m, Dài range mét)
        /// </summary>
        public static void CastStraightBox(Transform caster, float damage, float range, float boxWidth, LayerMask targetLayer)
        {
            float halfWidth = boxWidth * 0.5f;
            float halfHeight = BoxHeight * 0.5f;
            float backwardOffset = BackwardOffset;
            float totalLength = range + backwardOffset;
            float halfLength = totalLength * 0.5f;

            // Tâm Box dịch về sau để bao quát cả mục tiêu đứng sát chân / ép sát người
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

                // Kiểm tra độ cao tương đối so với chân nhân vật (-0.3m đến +2.3m)
                float relativeY = col.transform.position.y - caster.position.y;
                if (relativeY < RelativeYMin || relativeY > BoxHeight + RelativeYMaxOffset) continue;

                Vector3 dirToTarget = (col.transform.position - caster.position);
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude <= range * range)
                {
                    float angle = Vector3.Angle(caster.forward, dirToTarget);
                    // Mục tiêu áp sát trong phạm vi CloseTargetThreshold luôn trúng, xa hơn thì kiểm tra góc quạt
                    if (dirToTarget.sqrMagnitude <= CloseTargetThreshold * CloseTargetThreshold || angle <= halfAngle)
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
                if (relativeY < RelativeYMin || relativeY > BoxHeight + RelativeYMaxOffset) continue;

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
        /// Hỗ trợ đa tia đạn (ChildCount, MSGenerate, MSGenerateParam) theo DATA_CONVENTIONS_V2.md Mục 3 & 4
        /// </summary>
        public static void CastProjectile(Transform caster, EntityStats casterStats, SkillData skill, LayerMask targetLayer, Transform explicitTarget = null, Vector3 explicitTargetPoint = default)
        {
            if (caster == null || skill == null) return;

            int childCount = Mathf.Max(1, skill.childCount);
            int msGenerate = skill.msGenerate;

            float delayFrames = CsvParserHelper.ParseFloat(skill.msGenerateParam, 0f);
            float delaySec = delayFrames > 0f ? (delayFrames / SkillData.LOGIC_GAME_FPS) : 0f;

            // 1. Duy trì bãi sát thương tại chỗ (Area DoT / Hazard Zone - MSGenerate = 2)
            if (msGenerate == 2 && childCount > 1 && delaySec > 0f)
            {
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.StartCoroutine(SpawnAreaDoTCoroutine(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint, childCount, delaySec));
                }
                else
                {
                    SpawnProjectileBurst(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint, childCount);
                }
                return;
            }

            // 2. Mưa rơi liên hoàn ngẫu nhiên từ trên trời xuống (Meteor / Sky Drop Rain - MSGenerate = 3 hoặc MissileForm = 5)
            if ((msGenerate == 3 || skill.missileForm == 5) && childCount > 1 && delaySec > 0f)
            {
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.StartCoroutine(SpawnMeteorRainCoroutine(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint, childCount, delaySec));
                }
                else
                {
                    SpawnProjectileBurst(caster, casterStats, skill, targetLayer, explicitTarget, explicitTargetPoint, childCount);
                }
                return;
            }

            // 3. Bắn tuần tự cách quãng (MSGenerate = 4: TimedTrap, 5: RapidFire, hoặc Trail)
            if (childCount > 1 && delaySec > 0f && (msGenerate == 4 || msGenerate == 5 || (msGenerate == 1 && delayFrames > 0f)))
            {
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

            // 4. Bắn đồng loạt tức thời (MSGenerate = 0 hoặc 1) / Xoay tròn (MissileForm = 3)
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
                return;
            }

            // 1.5. Bắn chùm đa đạn đồng loạt song song dàn hàng ngang (MissileForm = 7 - MultiMissileWave / Sóng tỏa)
            if (skill.missileForm == 7)
            {
                // DATA_CONVENTIONS_V2.md Mục 3: Param1 = Khoảng cách cự ly giữa các tia đạn (cm -> mét)
                float spacingMeters = (skill.skillParam1 > 0f ? skill.skillParam1 : 50f) / 100.0f;
                Vector3 rightDir = Vector3.Cross(Vector3.up, aimDirection).normalized;

                for (int i = 0; i < count; i++)
                {
                    float lateralOffset = (i - (count - 1) * 0.5f) * spacingMeters;
                    Vector3 waveSpawnPos = spawnPos + rightDir * lateralOffset;
                    SpawnSingleMissileObject(caster, casterStats, skill, missile, waveSpawnPos, aimDirection, targetLayer, explicitTarget);
                }
                return;
            }

            // 2. Bắn đơn (1 tia): Bắn thẳng từ spawnPos tới đích con trỏ chuột
            if (count <= 1)
            {
                SpawnSingleMissileObject(caster, casterStats, skill, missile, spawnPos, aimDirection, targetLayer, explicitTarget);
                return;
            }

            // 3. Bắn chùm rẻ quạt (Fan Spread): Tia trung tâm hướng tới con trỏ, các tia còn lại xòe đều hai bên
            float angleStepSpread = DefaultFanSpreadAngle;
            if (skill.missileForm == 2 && skill.skillParam2 > 0f)
            {
                // DATA_CONVENTIONS_V2.md Mục 1 & 3: Binary Angle (64 units = 360°) -> góc = param2 * (360 / 64)
                angleStepSpread = skill.skillParam2 * (360f / 64f);
            }
            else
            {
                float paramVal = CsvParserHelper.ParseFloat(skill.msGenerateParam, 0f);
                angleStepSpread = paramVal > 0f ? paramVal : (skill.fanAngle > 0f ? (skill.fanAngle / Mathf.Max(1, count - 1)) : DefaultFanSpreadAngle);
            }
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

        private static System.Collections.IEnumerator SpawnAreaDoTCoroutine(
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

            Vector3 centerPos;
            if (explicitTargetPoint != default) centerPos = explicitTargetPoint;
            else if (explicitTarget != null) centerPos = explicitTarget.position;
            else centerPos = caster.position;

            for (int i = 0; i < count; i++)
            {
                if (caster == null) yield break;

                if (explicitTarget != null && skill.startPosType == TopDownGame.Skills.VfxStartPosType.Target)
                {
                    centerPos = explicitTarget.position;
                }

                SpawnSingleMissileObject(caster, casterStats, skill, missile, centerPos, caster.forward, targetLayer, explicitTarget);

                if (i < count - 1 && delaySec > 0f)
                {
                    yield return new WaitForSeconds(delaySec);
                }
            }
        }

        private static System.Collections.IEnumerator SpawnMeteorRainCoroutine(
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
            float radius = skill.range > 0f ? (skill.range * 0.5f) : 4.0f;

            Vector3 centerPos;
            if (explicitTargetPoint != default) centerPos = explicitTargetPoint;
            else if (explicitTarget != null) centerPos = explicitTarget.position;
            else centerPos = caster.position + caster.forward * (skill.range > 0f ? skill.range * 0.5f : 3.0f);

            for (int i = 0; i < count; i++)
            {
                if (caster == null) yield break;

                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 dropPos = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

                SpawnSingleMissileObject(caster, casterStats, skill, missile, dropPos, Vector3.down, targetLayer, explicitTarget);

                if (i < count - 1 && delaySec > 0f)
                {
                    yield return new WaitForSeconds(delaySec);
                }
            }
        }

        private static System.Collections.IEnumerator SpawnAreaDoTHitboxCoroutine(
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
            float damage = skill.CalculateDamage(casterStats);
            float range = skill.range > 0f ? skill.range : (missile != null && missile.dmgRange > 0f ? (missile.dmgRange / 10f) : 2.6f);

            Vector3 centerPos;
            if (explicitTargetPoint != default) centerPos = explicitTargetPoint;
            else if (explicitTarget != null) centerPos = explicitTarget.position;
            else centerPos = caster.position;

            string collEffectPath = missile != null ? missile.HitEffectPath : "";
            int collSoundId = missile != null ? missile.collSoundID : -1;

            for (int i = 0; i < count; i++)
            {
                if (caster == null) yield break;

                if (explicitTarget != null && skill.startPosType == TopDownGame.Skills.VfxStartPosType.Target)
                {
                    centerPos = explicitTarget.position;
                }

                Vector3 sphereCenter = centerPos + Vector3.up * (BoxHeight * 0.5f);
                int hitCount = Physics.OverlapSphereNonAlloc(sphereCenter, range, hitBuffer, targetLayer);
                HashSet<IDamageable> tickHitFilter = new HashSet<IDamageable>();
                for (int j = 0; j < hitCount; j++)
                {
                    Collider col = hitBuffer[j];
                    if (col == null || col.gameObject == caster.gameObject) continue;

                    float relativeY = col.transform.position.y - centerPos.y;
                    if (relativeY < RelativeYMin || relativeY > BoxHeight + RelativeYMaxOffset) continue;

                    Vector3 hitDirection = (col.transform.position - centerPos).normalized;
                    Vector3 hitPoint = col.ClosestPoint(sphereCenter);
                    if (ApplyDamage(col, damage, hitPoint, hitDirection, caster, tickHitFilter))
                    {
                        if (!string.IsNullOrEmpty(collEffectPath))
                        {
                            EffectManager.Instance.SpawnEffect(collEffectPath, hitPoint, Quaternion.identity, null, 2.0f);
                        }
                        if (collSoundId > 0)
                        {
                            SoundManager.Instance.PlaySoundAtPosition(collSoundId, hitPoint);
                        }
                    }
                }

                if (i < count - 1 && delaySec > 0f)
                {
                    yield return new WaitForSeconds(delaySec);
                }
            }
        }

        private static System.Collections.IEnumerator AreaHealDoTCoroutine(
            Transform caster,
            EntityStats casterStats,
            SkillData skill,
            Vector3 centerPos,
            float range,
            float intervalSec,
            float durationSec,
            Transform targetTransform = null)
        {
            float healAmount = skill.CalculateHeal(casterStats);
            int totalTicks = intervalSec > 0f ? Mathf.FloorToInt(durationSec / intervalSec) : 1;
            if (totalTicks <= 0) totalTicks = 1;

            bool isCasterPlayer = caster != null && (caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);

            for (int tick = 0; tick < totalTicks; tick++)
            {
                if (caster == null) yield break;

                Vector3 currentPos = targetTransform != null ? targetTransform.position : centerPos;
                Vector3 sphereCenter = currentPos + Vector3.up * 1.0f;
                int count = Physics.OverlapSphereNonAlloc(sphereCenter, range, hitBuffer);
                for (int i = 0; i < count; i++)
                {
                    Collider col = hitBuffer[i];
                    if (col == null) continue;

                    EntityStats targetStats = col.GetComponent<EntityStats>() ?? col.GetComponentInParent<EntityStats>();
                    if (targetStats == null || targetStats.IsDead) continue;

                    bool isTargetPlayer = targetStats.CompareTag(CombatLayersAndTags.TagPlayer) || targetStats.GetComponent<TopDownGame.Player.PlayerController>() != null;

                    if (isCasterPlayer == isTargetPlayer)
                    {
                        targetStats.Heal(healAmount);
                    }
                }

                if (tick < totalTicks - 1 && intervalSec > 0f)
                {
                    yield return new WaitForSeconds(intervalSec);
                }
            }
        }

        private static System.Collections.IEnumerator HealOverTimeCoroutine(
            Transform caster,
            EntityStats casterStats,
            SkillData skill,
            Transform explicitTarget,
            float intervalSec,
            float durationSec)
        {
            int totalTicks = intervalSec > 0f ? Mathf.FloorToInt(durationSec / intervalSec) : 1;
            if (totalTicks <= 0) totalTicks = 1;

            for (int tick = 0; tick < totalTicks; tick++)
            {
                if (tick > 0 && intervalSec > 0f)
                {
                    yield return new WaitForSeconds(intervalSec);
                }

                if (caster == null) yield break;

                SkillData flySkill = SkillDatabase.GetSkill(skill.flySkillId);
                if (flySkill != null && flySkill.id != skill.id)
                {
                    CastHeal(caster, casterStats, flySkill, explicitTarget, default, true);
                }
            }
        }

        public static bool ApplyDamage(Collider col, float damage, Vector3 hitPoint, Vector3 hitDirection, Transform caster, HashSet<IDamageable> hitFilter = null)
        {
            if (col == null || (caster != null && col.gameObject == caster.gameObject)) return false;

            bool isCasterPlayer = caster != null && (caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
            bool isTargetPlayer = col.CompareTag(CombatLayersAndTags.TagPlayer) || col.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;

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

            bool isCasterPlayer = caster != null && (caster.CompareTag(CombatLayersAndTags.TagPlayer) || caster.GetComponent<TopDownGame.Player.PlayerController>() != null);
            bool isTargetPlayer = col.CompareTag(CombatLayersAndTags.TagPlayer) || col.GetComponentInParent<TopDownGame.Player.PlayerController>() != null;

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
        public static void DrawGizmo(SkillType type, Vector3 origin, Vector3 forward, float range, float fanAngle, float boxWidth = DefaultBoxWidth)
        {
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.85f);

            switch (type)
            {
                case SkillType.StraightRay:
                case SkillType.TargetLock:
                    float halfHeight = BoxHeight * 0.5f;
                    float backwardOffset = BackwardOffset;
                    float totalLength = range + backwardOffset;
                    float halfLength = totalLength * 0.5f;
                    Vector3 boxCenter = origin + (Vector3.up * halfHeight) + (forward * (halfLength - backwardOffset));
                    Vector3 boxSize = new Vector3(boxWidth > 0f ? boxWidth : DefaultBoxWidth, BoxHeight, totalLength);

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
