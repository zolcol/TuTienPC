using UnityEngine;

namespace TopDownGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMovement : MonoBehaviour
    {
        [Header("=== MOVEMENT SETTINGS ===")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSmoothTime = 0.08f;
        [Tooltip("Độ mượt khi xoay hướng lúc đang đánh (0.12 - 0.18s)")]
        [SerializeField] private float attackRotationSmoothTime = 0.14f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController characterController;
        private UnityEngine.Camera mainCamera;
        private bool isGrounded;
        private float turnSmoothVelocity;
        private Vector3 moveDirection;
        private Vector3 verticalVelocity;

        public CharacterController CharacterController => characterController;
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
        public float RotationSmoothTime => rotationSmoothTime;
        public float AttackRotationSmoothTime => attackRotationSmoothTime;
        public float Gravity => gravity;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = UnityEngine.Camera.main;
        }

        public void SetCamera(UnityEngine.Camera camera)
        {
            mainCamera = camera;
        }

        public Vector3 CalculateCameraRelativeInput(Vector2 rawInput)
        {
            if (rawInput.sqrMagnitude < 0.001f) return Vector3.zero;

            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }

            if (mainCamera != null)
            {
                Vector3 camForward = mainCamera.transform.forward;
                Vector3 camRight = mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                return (camForward * rawInput.y + camRight * rawInput.x).normalized;
            }

            return new Vector3(rawInput.x, 0f, rawInput.y);
        }

        public void Move(Vector3 direction)
        {
            moveDirection = direction * moveSpeed;
        }

        public void MoveWithSpeed(Vector3 direction, float speed)
        {
            moveDirection = direction * speed;
        }

        public void UpdateMovementAndGravity()
        {
            if (characterController == null) return;

            isGrounded = characterController.isGrounded;
            if (isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }

            Vector3 finalMove = (moveDirection + verticalVelocity) * Time.deltaTime;
            characterController.Move(finalMove);
            moveDirection = Vector3.zero;
        }

        public void RotateTowards(Vector3 direction)
        {
            RotateTowards(direction, rotationSmoothTime);
        }

        public void RotateTowards(Vector3 direction, float smoothTime)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                if (smoothTime <= 0.001f)
                {
                    transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
                    turnSmoothVelocity = 0f;
                }
                else
                {
                    float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, smoothTime);
                    transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                }
            }
        }

        public void RotateTowardsDirection(Vector3 direction, float speedDegPerSec)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
                float speed = speedDegPerSec > 0f ? speedDegPerSec : 1000f;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
            }
        }

        public void RotateTowardsInstantly(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }

        public void ResetTurnVelocity()
        {
            turnSmoothVelocity = 0f;
        }
    }
}
