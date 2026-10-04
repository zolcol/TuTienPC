using UnityEngine;
using TopDownGame.Stats;

namespace TopDownGame.Combat
{
    /// <summary>
    /// Pure C# Static Class tập trung toàn bộ công thức toán học và chuyển đổi thời gian/chỉ số trong Combat.
    /// Đảm bảo không phụ thuộc vào trạng thái MonoBehaviour, không cấp phát bộ nhớ rác (Zero GC Alloc).
    /// </summary>
    public static class CombatFormula
    {
        /// <summary>
        /// Chuẩn 15 FPS cho ActionEvent, Frame Timing (Animation) theo DATA_CONVENTIONS.md
        /// </summary>
        public const float ACTION_EVENT_FPS = 15f;

        public const float DEFAULT_RUN_SPEED = 5.0f;
        public const float DEFAULT_WALK_SPEED = 2.5f;

        /// <summary>
        /// Độ lệch Y chuẩn (0.02m) nâng VFX mặt đất lên khỏi sàn để khử triệt để hiện tượng Z-Fighting.
        /// </summary>
        public const float GROUND_VFX_Y_OFFSET = 0.02f;

        /// <summary>
        /// Lấy LayerMask mặc định của mặt đất / địa hình (tự động loại trừ layer Player, Enemy, Ignore Raycast).
        /// </summary>
        public static int GetDefaultGroundLayerMask()
        {
            int playerLayer = GameConstants.Layers.PlayerLayer;
            int enemyLayer = GameConstants.Layers.EnemyLayer;
            int ignoreRaycast = GameConstants.Layers.IgnoreRaycastLayer;

            int maskToExclude = 0;
            if (playerLayer >= 0) maskToExclude |= (1 << playerLayer);
            if (enemyLayer >= 0) maskToExclude |= (1 << enemyLayer);
            if (ignoreRaycast >= 0) maskToExclude |= (1 << ignoreRaycast);

            return ~maskToExclude;
        }

        /// <summary>
        /// Bắn tia Raycast thẳng đứng xuống dưới để căn chỉnh độ cao Y bám sát địa hình/mặt sàn và cộng thêm yOffset khử Z-fighting.
        /// </summary>
        public static Vector3 SnapToGround(Vector3 worldPos, float yOffset = GROUND_VFX_Y_OFFSET, float raycastDistance = 6.0f, int groundLayerMask = 0)
        {
            if (groundLayerMask == 0) groundLayerMask = GetDefaultGroundLayerMask();

            Vector3 rayStart = worldPos + Vector3.up * 2.0f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                return new Vector3(worldPos.x, hit.point.y + yOffset, worldPos.z);
            }
            return new Vector3(worldPos.x, worldPos.y + yOffset, worldPos.z);
        }

        /// <summary>
        /// Thử tìm tọa độ tiếp xúc mặt đất chính xác qua Raycast.
        /// </summary>
        public static bool TryGetGroundPoint(Vector3 worldPos, out Vector3 groundPoint, float yOffset = GROUND_VFX_Y_OFFSET, float raycastDistance = 6.0f, int groundLayerMask = 0)
        {
            if (groundLayerMask == 0) groundLayerMask = GetDefaultGroundLayerMask();

            Vector3 rayStart = worldPos + Vector3.up * 2.0f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                groundPoint = new Vector3(worldPos.x, hit.point.y + yOffset, worldPos.z);
                return true;
            }
            groundPoint = new Vector3(worldPos.x, worldPos.y + yOffset, worldPos.z);
            return false;
        }

        /// <summary>
        /// Chuyển đổi số frame sang giây theo chuẩn FPS (mặc định 15 FPS).
        /// </summary>
        public static float FrameToSeconds(int frame, float fps = ACTION_EVENT_FPS)
        {
            return frame >= 0 ? (frame / fps) : -1f;
        }

        /// <summary>
        /// Chuyển đổi giây sang số frame theo chuẩn FPS (mặc định 15 FPS).
        /// </summary>
        public static int SecondsToFrame(float seconds, float fps = ACTION_EVENT_FPS)
        {
            return Mathf.RoundToInt(seconds * fps);
        }

        /// <summary>
        /// Tính toán số frame thực tế sau khi áp dụng Tốc Đánh theo DATA_CONVENTIONS_V2.md Mục 6 (SkillSetting.ini L83-L95):
        /// SpeedReduction = floor(AttackSpeed / 10) / 20
        /// Calculated Frame = Original Frame * (1.0 - SpeedReduction)
        /// Final Action Frame = Clamp(Calculated Frame, Min = 9, Max = 100)
        /// </summary>
        public static (int finalFrame, float speedFactor) CalculateScaledActionFrame(int originalFrame, float attackSpeedPercent)
        {
            if (originalFrame <= 0) return (originalFrame, 1.0f);
            float speedReduction = Mathf.Floor(attackSpeedPercent / 10f) / 20f;
            float calculatedFrame = originalFrame * (1.0f - speedReduction);
            int finalFrame = Mathf.Clamp(Mathf.RoundToInt(calculatedFrame), 1, 100);
            float factor = (float)originalFrame / finalFrame;
            return (finalFrame, factor);
        }

        /// <summary>
        /// Tính thời lượng thực tế (giây) của animation sau khi scale theo Tốc Đánh.
        /// </summary>
        public static float CalculateActionDuration(int originalFrame, float attackSpeedPercent = 0f, float fps = ACTION_EVENT_FPS)
        {
            if (originalFrame <= 0) return 0f;
            var (finalFrame, _) = CalculateScaledActionFrame(originalFrame, attackSpeedPercent);
            return finalFrame / fps;
        }

        /// <summary>
        /// Tính tốc độ di chuyển thực tế (m/s) từ giá trị raw trong CSV (cm/frame ở 15 FPS).
        /// moveSpeed = speed * 15 / 100
        /// </summary>
        public static float CalculateSpeedFromRaw(float rawSpeed, float fps = ACTION_EVENT_FPS)
        {
            return rawSpeed > 0f ? (rawSpeed * fps / 100.0f) : 0f;
        }

        /// <summary>
        /// Tính tổng sát thương dựa trên chỉ số Vật Lý và Phép của thực thể tung chiêu.
        /// </summary>
        public static float CalculateDamage(EntityStats attackerStats, float physScale, float magicScale)
        {
            if (attackerStats == null)
            {
                return (20f * physScale) + (10f * magicScale);
            }
            return (attackerStats.PhysicalDamage * physScale) + (attackerStats.MagicDamage * magicScale);
        }

        /// <summary>
        /// Tính tổng sát thương trực tiếp từ giá trị công Vật Lý và Phép.
        /// </summary>
        public static float CalculateDamage(float physDamage, float magicDamage, float physScale, float magicScale)
        {
            return (physDamage * physScale) + (magicDamage * magicScale);
        }

        /// <summary>
        /// Tính tổng lượng hồi máu dựa trên chỉ số Phép / Nội công của thực thể tung chiêu.
        /// </summary>
        public static float CalculateHeal(EntityStats casterStats, float magicScale)
        {
            float baseHeal = 100f * (magicScale > 0f ? magicScale : 1f);
            if (casterStats == null)
            {
                return baseHeal;
            }
            return baseHeal + (casterStats.MagicDamage * 2.2f);
        }

        /// <summary>
        /// Tính tổng lượng hồi máu trực tiếp từ giá trị công Phép.
        /// </summary>
        public static float CalculateHeal(float magicDamage, float magicScale)
        {
            float baseHeal = 100f * (magicScale > 0f ? magicScale : 1f);
            return baseHeal + (magicDamage * 2.2f);
        }

        /// <summary>
        /// Tìm điểm gần nhất trên Collider an toàn cho mọi loại Collider (kể cả non-convex MeshCollider, CharacterController, TerrainCollider...).
        /// </summary>
        public static Vector3 GetSafeClosestPoint(Collider col, Vector3 point)
        {
            if (col == null) return point;
            if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider || (col is MeshCollider mc && mc.convex))
            {
                return col.ClosestPoint(point);
            }
            return col.bounds.ClosestPoint(point);
        }
    }
}
