using UnityEngine;
using TopDownGame.Skills;
using TopDownGame.Combat;
using TopDownGame.Stats;

namespace TopDownGame.Player
{
    public class PlayerAiming : MonoBehaviour
    {
        [Header("=== TARGETING SETTINGS ===")]
        [SerializeField] private LayerMask targetLayer = ~0;
        [SerializeField] private LayerMask groundLayer = 0;

        private UnityEngine.Camera mainCamera;
        private SkillData aimingSkill;
        private GameObject currentIndicator;
        private Vector3 aimGroundPosition, currentTargetPoint, currentTargetDirection = Vector3.forward;
        private Transform currentLockTarget;

        private static readonly RaycastHit[] aimSphereCastBuffer = new RaycastHit[32];
        private static readonly Collider[] targetInFrontBuffer = new Collider[32];

        public LayerMask TargetLayer { get => targetLayer; set => targetLayer = value; }
        public LayerMask GroundLayer { get => groundLayer; set => groundLayer = value; }
        public int GetGroundMask() => groundLayer.value != 0 ? groundLayer.value : CombatFormula.GetDefaultGroundLayerMask();
        public SkillData AimingSkill => aimingSkill;
        public GameObject CurrentIndicator => currentIndicator;
        public Transform CurrentLockTarget => currentLockTarget;
        public Vector3 CurrentTargetPoint => currentTargetPoint;
        public Vector3 CurrentTargetDirection => currentTargetDirection;
        public Vector3 AimGroundPosition => aimGroundPosition;
        public bool IsAiming => aimingSkill != null;

        private void Awake() => mainCamera = UnityEngine.Camera.main;
        public void SetCamera(UnityEngine.Camera camera) => mainCamera = camera;

        public void StartAiming(SkillData skill)
        {
            if (aimingSkill != skill) CancelAiming();
            aimingSkill = skill;
            int resId = (skill.selectorType == SkillSelectorType.DirectionalArrow) ? IndicatorVfxResID.DirectionArrow : (skill.IsHeal ? IndicatorVfxResID.SelectedAllyAOE : IndicatorVfxResID.SelectedEnemyAOE);
            string path = (resId > 0) ? TopDownGame.Data.EffectDatabase.GetEffectPath(resId) : null;
            if (!string.IsNullOrEmpty(path))
            {
                Vector3 spawnPos = CombatFormula.SnapToGround(transform.position, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, GetGroundMask());
                currentIndicator = EffectManager.Instance.SpawnEffect(path, spawnPos, transform.rotation, null, 99f);
            }
        }

        public void CancelAiming()
        {
            aimingSkill = null;
            if (currentIndicator != null) { EffectManager.Instance.RecycleEffect(currentIndicator); currentIndicator = null; }
        }

        public void UpdateSkillAiming(Vector3 inputVector, bool isUsingGamepad)
        {
            if (aimingSkill == null || currentIndicator == null) return;
            float maxRange = aimingSkill.selectorRange > 0f ? aimingSkill.selectorRange : aimingSkill.range;
            int groundMask = GetGroundMask();
            Vector3 targetGroundPos = CombatFormula.SnapToGround(transform.position + transform.forward * maxRange, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
            Vector3 aimDir = transform.forward;

            if (!isUsingGamepad)
            {
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
                    if (mainCamera != null)
                    {
                        Ray ray = mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                        Vector3 hitPoint;
                        if (Physics.Raycast(ray, out RaycastHit groundHit, 200f, groundMask, QueryTriggerInteraction.Ignore))
                        {
                            hitPoint = groundHit.point;
                        }
                        else if (new Plane(Vector3.up, transform.position).Raycast(ray, out float enter))
                        {
                            hitPoint = ray.GetPoint(enter);
                        }
                        else
                        {
                            hitPoint = transform.position + transform.forward * maxRange;
                        }

                        Vector3 offset = hitPoint - transform.position;
                        offset.y = 0f;
                        if (offset.magnitude > maxRange)
                        {
                            Vector3 clampedPos = transform.position + (offset.sqrMagnitude > 0.001f ? offset.normalized : transform.forward) * maxRange;
                            targetGroundPos = CombatFormula.SnapToGround(clampedPos, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
                        }
                        else
                        {
                            targetGroundPos = CombatFormula.SnapToGround(hitPoint, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
                        }
                        aimDir = (targetGroundPos - transform.position);
                        aimDir.y = 0f;
                    }
                }
            }
            else if (inputVector.sqrMagnitude > 0.01f)
            {
                aimDir = inputVector;
                Vector3 rawPos = transform.position + aimDir * maxRange;
                targetGroundPos = CombatFormula.SnapToGround(rawPos, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
            }

            if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;
            else aimDir.Normalize();

            if (aimingSkill.selectorType == SkillSelectorType.DirectionalArrow)
            {
                currentIndicator.transform.position = CombatFormula.SnapToGround(transform.position, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
                currentIndicator.transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
            }
            else if (aimingSkill.selectorType == SkillSelectorType.SmartcastCircleAOE)
            {
                aimGroundPosition = targetGroundPos;
                currentIndicator.transform.position = targetGroundPos;
            }
        }

        public bool AimSkill(SkillData skill, Vector3 inputVector, bool isUsingGamepad)
        {
            if (skill == null) return false;
            currentLockTarget = null;
            int groundMask = GetGroundMask();
            currentTargetPoint = CombatFormula.SnapToGround(transform.position + transform.forward * (skill.range > 0 ? skill.range : 5f), CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
            currentTargetDirection = transform.forward;
            if (skill.targetSelf || skill.relation == SkillRelation.Self) { currentTargetPoint = CombatFormula.SnapToGround(transform.position, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask); return true; }

            var missile = skill.childId > 0 ? TopDownGame.Data.MissileDatabase.GetMissile(skill.childId) : null;
            bool isTargetLockSkill = skill.IsHeal 
                || skill.skillType == SkillType.TargetLock 
                || (missile != null && missile.moveKind == MissileMoveKind.HomingTracking)
                || (skill.startPosType == TopDownGame.Skills.VfxStartPosType.Target && (missile == null || (int)missile.moveKind == 0));
            if (!isUsingGamepad)
            {
                if (UnityEngine.InputSystem.Mouse.current == null || (mainCamera == null && (mainCamera = UnityEngine.Camera.main) == null)) return true;
                Ray ray = mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                if (isTargetLockSkill)
                {
                    Transform found = RaycastTarget(ray, skill.IsHeal);
                    if (found != null && Vector3.Distance(transform.position, found.position) <= skill.range) return SetTarget(found);
                    Transform fallback = FindTargetInFront(skill.range, skill.IsHeal);
                    return fallback != null && SetTarget(fallback);
                }

                Vector3 hitPoint;
                if (Physics.Raycast(ray, out RaycastHit groundHit, 200f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    hitPoint = groundHit.point;
                }
                else if (new Plane(Vector3.up, transform.position).Raycast(ray, out float enter))
                {
                    hitPoint = ray.GetPoint(enter);
                }
                else
                {
                    hitPoint = transform.position + transform.forward * (skill.range > 0 ? skill.range : 5f);
                }

                float maxRange = skill.selectorRange > 0f ? skill.selectorRange : skill.range;
                Vector3 offset = hitPoint - transform.position;
                offset.y = 0f;
                Vector3 targetPos = (maxRange > 0f && offset.magnitude > maxRange)
                    ? transform.position + (offset.sqrMagnitude > 0.001f ? offset.normalized : transform.forward) * maxRange
                    : hitPoint;
                currentTargetPoint = CombatFormula.SnapToGround(targetPos, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);

                Vector3 aimDir = currentTargetPoint - transform.position;
                aimDir.y = 0f;
                currentTargetDirection = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : transform.forward;
            }
            else
            {
                if (isTargetLockSkill) return SetTarget(FindTargetInFront(skill.range, skill.IsHeal));
                currentTargetDirection = inputVector.sqrMagnitude > 0.01f ? inputVector.normalized : transform.forward;
                float maxRange = skill.selectorRange > 0f ? skill.selectorRange : (skill.range > 0f ? skill.range : 5f);
                Vector3 rawPos = transform.position + currentTargetDirection * maxRange;
                currentTargetPoint = CombatFormula.SnapToGround(rawPos, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
            }
            return true;
        }

        private bool IsValidTarget(Collider col, bool isHeal, out Transform targetRoot)
        {
            targetRoot = null;
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) return false;

            var stats = col.GetComponent<EntityStats>() ?? col.GetComponentInParent<EntityStats>();
            if (stats != null)
            {
                if (stats.IsDead) return false;
                bool isPlayer = stats.CompareTag(CombatLayersAndTags.TagPlayer) || stats.GetComponent<PlayerController>() != null;
                if (isHeal)
                {
                    if (isPlayer) { targetRoot = stats.transform; return true; }
                    return false;
                }
                else
                {
                    if (!isPlayer) { targetRoot = stats.transform; return true; }
                    return false;
                }
            }

            if (isHeal) return false;

            var dummy = col.GetComponent<DummyTarget>() ?? col.GetComponentInParent<DummyTarget>();
            if (dummy != null)
            {
                targetRoot = dummy.transform;
                return true;
            }

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable is Component comp)
            {
                bool isPlayer = comp.CompareTag(CombatLayersAndTags.TagPlayer) || comp.GetComponent<PlayerController>() != null;
                if (!isPlayer) { targetRoot = comp.transform; return true; }
            }

            return false;
        }

        private Transform RaycastTarget(Ray ray, bool isHeal)
        {
            int hitCount = Physics.SphereCastNonAlloc(ray, 1.5f, aimSphereCastBuffer, 100f, targetLayer);
            Transform found = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = aimSphereCastBuffer[i];
                if (hit.collider == null) continue;
                if (!IsValidTarget(hit.collider, isHeal, out Transform validRoot)) continue;

                float dist = Vector3.Cross(ray.direction, validRoot.position - ray.origin).magnitude;
                if (dist < minDist) { minDist = dist; found = validRoot; }
            }
            return found;
        }

        private bool SetTarget(Transform target)
        {
            if (target == null) return false;
            currentLockTarget = target;
            currentTargetPoint = target.position;
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            currentTargetDirection = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
            return true;
        }

        public Transform FindTargetInFront(float range, bool isHeal = false)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, targetInFrontBuffer, targetLayer);
            Transform best = null;
            float minDst = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider h = targetInFrontBuffer[i];
                if (h == null) continue;
                if (!IsValidTarget(h, isHeal, out Transform validRoot)) continue;

                Vector3 dir = validRoot.position - transform.position;
                dir.y = 0;
                float dst = dir.magnitude;
                if (dst > 0.01f && Vector3.Dot(transform.forward, dir / dst) > 0.3f && dst < minDst) { minDst = dst; best = validRoot; }
                else if (dst <= 0.01f && best == null) best = validRoot;
            }
            return best;
        }
    }
}
