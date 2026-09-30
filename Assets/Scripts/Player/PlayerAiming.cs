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

        private UnityEngine.Camera mainCamera;
        private SkillData aimingSkill;
        private GameObject currentIndicator;
        private Vector3 aimGroundPosition, currentTargetPoint, currentTargetDirection = Vector3.forward;
        private Transform currentLockTarget;

        private static readonly RaycastHit[] aimSphereCastBuffer = new RaycastHit[32];
        private static readonly Collider[] targetInFrontBuffer = new Collider[32];

        public LayerMask TargetLayer { get => targetLayer; set => targetLayer = value; }
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
                currentIndicator = EffectManager.Instance.SpawnEffect(path, transform.position, transform.rotation, null, 99f);
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
            Vector3 targetGroundPos = transform.position + transform.forward * maxRange;
            Vector3 aimDir = transform.forward;

            if (!isUsingGamepad)
            {
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
                    if (mainCamera != null)
                    {
                        Ray ray = mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                        if (new Plane(Vector3.up, transform.position).Raycast(ray, out float enter))
                        {
                            Vector3 hitPoint = ray.GetPoint(enter);
                            Vector3 offset = hitPoint - transform.position;
                            targetGroundPos = (offset.magnitude > maxRange) ? (transform.position + offset.normalized * maxRange) : hitPoint;
                            aimDir = (targetGroundPos - transform.position).normalized;
                        }
                    }
                }
            }
            else if (inputVector.sqrMagnitude > 0.01f)
            {
                aimDir = inputVector;
                targetGroundPos = transform.position + aimDir * maxRange;
            }

            if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;
            if (aimingSkill.selectorType == SkillSelectorType.DirectionalArrow)
            {
                currentIndicator.transform.position = transform.position;
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
            currentTargetPoint = transform.position + transform.forward * (skill.range > 0 ? skill.range : 5f);
            currentTargetDirection = transform.forward;
            if (skill.targetSelf || skill.relation == SkillRelation.Self) { currentTargetPoint = transform.position; return true; }

            bool isTargetLockSkill = skill.IsHeal || (skill.childId > 0 && TopDownGame.Data.MissileDatabase.GetMissile(skill.childId)?.moveKind == MissileMoveKind.HomingTracking) || skill.skillAttackType == SkillAttackType.Target;
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
                if (new Plane(Vector3.up, transform.position).Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    float maxRange = skill.selectorRange > 0f ? skill.selectorRange : skill.range;
                    Vector3 offset = hitPoint - transform.position;
                    offset.y = 0f;
                    currentTargetPoint = (maxRange > 0f && skill.selectorType == SkillSelectorType.SmartcastCircleAOE && offset.magnitude > maxRange) ? (transform.position + offset.normalized * maxRange) : hitPoint;
                    Vector3 aimDir = currentTargetPoint - transform.position;
                    aimDir.y = 0f;
                    currentTargetDirection = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : transform.forward;
                }
            }
            else
            {
                if (isTargetLockSkill) return SetTarget(FindTargetInFront(skill.range, skill.IsHeal));
                currentTargetDirection = inputVector.sqrMagnitude > 0.01f ? inputVector.normalized : transform.forward;
                currentTargetPoint = transform.position + currentTargetDirection * (skill.range > 0 ? skill.range : 5f);
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
