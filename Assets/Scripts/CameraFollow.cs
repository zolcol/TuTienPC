using UnityEngine;

namespace TopDownGame
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [Tooltip("Khoảng cách lệch của Camera so với Player (X, Y, Z)")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -7f);
        [Tooltip("Độ trễ mượt mà khi camera bám theo nhân vật")]
        [SerializeField] private float smoothTime = 0.2f;

        [Header("Rotation")]
        [Tooltip("Nếu tích chọn, camera sẽ luôn hướng góc nhìn về phía player")]
        [SerializeField] private bool lookAtTarget = false;

        private Vector3 currentVelocity = Vector3.zero;

        private void Start()
        {
            // Tự động tìm player theo tag nếu chưa gán thủ công
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Tính toán vị trí mục tiêu
            Vector3 targetPosition = target.position + offset;

            // Di chuyển camera mượt mà không bị giật lag
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

            if (lookAtTarget)
            {
                transform.LookAt(target.position + Vector3.up * 1.5f);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
