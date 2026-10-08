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

        [Header("=== RANGE CIRCLE INDICATOR ===")]
        [SerializeField] private LineRenderer rangeCircleRenderer;
        [SerializeField] private int circleSegments = 64;
        [SerializeField] private float circleLineWidth = 0.05f;
        [SerializeField] private Color circleColor = new Color(0.2f, 0.75f, 1f, 0.45f);

        private UnityEngine.Camera mainCamera;
        private SkillData aimingSkill;
        private GameObject currentIndicator;
        private Vector3 aimGroundPosition, currentTargetPoint, currentTargetDirection = Vector3.forward;
        private Transform currentLockTarget;
        private Vector3[] circlePositionsBuffer;

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

        private void Awake()
        {
            mainCamera = UnityEngine.Camera.main;
            EnsureRangeCircleRenderer();
        }

        public void SetCamera(UnityEngine.Camera camera) => mainCamera = camera;

        private void EnsureRangeCircleRenderer()
        {
            if (rangeCircleRenderer != null) return;
            GameObject circleObj = new GameObject("RangeCircleIndicator");
            circleObj.transform.SetParent(transform, false);
            rangeCircleRenderer = circleObj.AddComponent<LineRenderer>();
            rangeCircleRenderer.useWorldSpace = true;
            rangeCircleRenderer.loop = true;
            rangeCircleRenderer.positionCount = circleSegments;
            rangeCircleRenderer.startWidth = circleLineWidth;
            rangeCircleRenderer.endWidth = circleLineWidth;
            rangeCircleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rangeCircleRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material mat = new Material(shader) { color = circleColor };
                rangeCircleRenderer.material = mat;
            }
            rangeCircleRenderer.startColor = circleColor;
            rangeCircleRenderer.endColor = circleColor;
            rangeCircleRenderer.enabled = false;
        }

        private void UpdateRangeCircle(float radius)
        {
            if (rangeCircleRenderer == null) EnsureRangeCircleRenderer();
            if (rangeCircleRenderer == null) return;
            if (circlePositionsBuffer == null || circlePositionsBuffer.Length != circleSegments)
                circlePositionsBuffer = new Vector3[circleSegments];

            int groundMask = GetGroundMask();
            Vector3 center = transform.position;
            float angleStep = 360f / circleSegments;

            for (int i = 0; i < circleSegments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                Vector3 worldPos = center + offset;
                circlePositionsBuffer[i] = CombatFormula.SnapToGround(worldPos, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
            }

            rangeCircleRenderer.positionCount = circleSegments;
            rangeCircleRenderer.SetPositions(circlePositionsBuffer);
            rangeCircleRenderer.enabled = true;
        }

        public void StartAiming(SkillData skill)
        {
            if (aimingSkill != skill) CancelAiming();
            aimingSkill = skill;

            float maxRange = GetEffectiveRange(skill);
            UpdateRangeCircle(maxRange);

            int resId = (skill.selectorType == SkillSelectorType.DirectionalArrow) ? IndicatorVfxResID.DirectionArrow : (skill.IsHeal ? IndicatorVfxResID.SelectedAllyAOE : IndicatorVfxResID.SelectedEnemyAOE);
            string path = (resId > 0) ? TopDownGame.Data.EffectDatabase.GetEffectPath(resId) : null;
            if (!string.IsNullOrEmpty(path))
            {
                Vector3 spawnPos = CombatFormula.SnapToGround(transform.position, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, GetGroundMask());
                currentIndicator = EffectManager.Instance.SpawnEffect(path, spawnPos, transform.rotation, null, 99f);
                if (skill.selectorType == SkillSelectorType.TargetLock && currentIndicator != null)
                {
                    currentIndicator.SetActive(false);
                }
            }
        }

        public void CancelAiming()
        {
            aimingSkill = null;
            if (currentIndicator != null)
            {
                if (EffectManager.HasInstance)
                {
                    EffectManager.Instance.RecycleEffect(currentIndicator);
                }
                else
                {
                    Destroy(currentIndicator);
                }
                currentIndicator = null;
            }
            if (rangeCircleRenderer != null) rangeCircleRenderer.enabled = false;
        }

        private float GetEffectiveRange(SkillData skill)
        {
            if (skill == null) return 5f;
            if (skill.selectorRange > 0f) return skill.selectorRange;
            if (skill.range > 0f) return skill.range;
            return 5f;
        }

        private static float GetHorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public void UpdateSkillAiming(Vector3 inputVector, bool isUsingGamepad)
        {
            if (TopDownGame.UI.UIModalManager.IsAnyModalOpen)
            {
                CancelAiming();
                return;
            }
            if (aimingSkill == null || currentIndicator == null) return;
            float maxRange = GetEffectiveRange(aimingSkill);
            UpdateRangeCircle(maxRange);
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
                if (!currentIndicator.activeSelf) currentIndicator.SetActive(true);
                currentIndicator.transform.position = CombatFormula.SnapToGround(transform.position, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
                currentIndicator.transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
            }
            else if (aimingSkill.selectorType == SkillSelectorType.SmartcastCircleAOE)
            {
                if (!currentIndicator.activeSelf) currentIndicator.SetActive(true);
                aimGroundPosition = targetGroundPos;
                currentIndicator.transform.position = targetGroundPos;
            }
            else if (aimingSkill.selectorType == SkillSelectorType.TargetLock)
            {
                Transform target = null;
                if (!isUsingGamepad && UnityEngine.InputSystem.Mouse.current != null && mainCamera != null)
                {
                    Ray ray = mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                    target = RaycastTarget(ray, aimingSkill.IsHeal, maxRange);
                }
                else if (isUsingGamepad)
                {
                    target = FindTargetInFront(maxRange, aimingSkill.IsHeal);
                }

                if (target != null && GetHorizontalDistance(transform.position, target.position) <= maxRange)
                {
                    if (!currentIndicator.activeSelf) currentIndicator.SetActive(true);
                    Vector3 targetPos = CombatFormula.SnapToGround(target.position, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
                    aimGroundPosition = targetPos;
                    currentIndicator.transform.position = targetPos;
                }
                else
                {
                    if (currentIndicator.activeSelf) currentIndicator.SetActive(false);
                    aimGroundPosition = targetGroundPos;
                }
            }
        }

        public bool AimSkill(SkillData skill, Vector3 inputVector, bool isUsingGamepad)
        {
            if (skill == null) return false;
            currentLockTarget = null;
            int groundMask = GetGroundMask();
            float effectiveRange = GetEffectiveRange(skill);
            currentTargetPoint = CombatFormula.SnapToGround(transform.position + transform.forward * effectiveRange, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);
            currentTargetDirection = transform.forward;
            if (skill.targetSelf || skill.relation == SkillRelation.Self) { currentTargetPoint = CombatFormula.SnapToGround(transform.position, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask); return true; }

            var missile = skill.childId > 0 ? TopDownGame.Data.MissileDatabase.GetMissile(skill.childId) : null;
            bool isTargetLockSkill = skill.IsHeal 
                || skill.skillType == SkillType.TargetLock 
                || skill.selectorType == SkillSelectorType.TargetLock
                || (missile != null && missile.moveKind == MissileMoveKind.HomingTracking)
                || (skill.startPosType == TopDownGame.Skills.VfxStartPosType.Target && (missile == null || (int)missile.moveKind == 0));
            if (!isUsingGamepad)
            {
                if (UnityEngine.InputSystem.Mouse.current == null || (mainCamera == null && (mainCamera = UnityEngine.Camera.main) == null)) return true;
                Ray ray = mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                if (isTargetLockSkill)
                {
                    Transform found = RaycastTarget(ray, skill.IsHeal, effectiveRange);
                    if (found != null && GetHorizontalDistance(transform.position, found.position) <= effectiveRange) return SetTarget(found);
                    Transform fallback = FindTargetInFront(effectiveRange, skill.IsHeal, preferFurthest: true);
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
                    hitPoint = transform.position + transform.forward * effectiveRange;
                }

                Vector3 offset = hitPoint - transform.position;
                offset.y = 0f;
                Vector3 targetPos = (effectiveRange > 0f && offset.magnitude > effectiveRange)
                    ? transform.position + (offset.sqrMagnitude > 0.001f ? offset.normalized : transform.forward) * effectiveRange
                    : hitPoint;
                currentTargetPoint = CombatFormula.SnapToGround(targetPos, CombatFormula.GROUND_VFX_Y_OFFSET, 6f, groundMask);

                Vector3 aimDir = currentTargetPoint - transform.position;
                aimDir.y = 0f;
                currentTargetDirection = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : transform.forward;
            }
            else
            {
                if (isTargetLockSkill) return SetTarget(FindTargetInFront(effectiveRange, skill.IsHeal));
                currentTargetDirection = inputVector.sqrMagnitude > 0.01f ? inputVector.normalized : transform.forward;
                Vector3 rawPos = transform.position + currentTargetDirection * effectiveRange;
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

        private Transform RaycastTarget(Ray ray, bool isHeal, float maxRange = float.MaxValue)
        {
            int hitCount = Physics.SphereCastNonAlloc(ray, 1.5f, aimSphereCastBuffer, 100f, targetLayer);
            Transform found = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = aimSphereCastBuffer[i];
                if (hit.collider == null) continue;
                if (!IsValidTarget(hit.collider, isHeal, out Transform validRoot)) continue;

                if (maxRange < float.MaxValue && GetHorizontalDistance(transform.position, validRoot.position) > maxRange)
                    continue;

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

        public Transform FindTargetInFront(float range, bool isHeal = false, bool preferFurthest = false)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, targetInFrontBuffer, targetLayer);
            Transform best = null;
            float bestDst = preferFurthest ? -1f : float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider h = targetInFrontBuffer[i];
                if (h == null) continue;
                if (!IsValidTarget(h, isHeal, out Transform validRoot)) continue;

                Vector3 dir = validRoot.position - transform.position;
                dir.y = 0;
                float dst = dir.magnitude;
                if (dst > range) continue;
                bool isFacing = dst > 0.01f && Vector3.Dot(transform.forward, dir / dst) > 0.3f;
                if (isFacing)
                {
                    if (preferFurthest)
                    {
                        if (dst > bestDst) { bestDst = dst; best = validRoot; }
                    }
                    else
                    {
                        if (dst < bestDst) { bestDst = dst; best = validRoot; }
                    }
                }
                else if (dst <= 0.01f && best == null) best = validRoot;
            }
            return best;
        }
    }
}
