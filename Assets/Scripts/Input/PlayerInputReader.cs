using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Input
{
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Action Asset (Bảng cấu hình phím)")]
        [Tooltip("Kéo file PlayerInputActions.inputactions vào đây")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction moveAction;
        private InputAction attackAction;
        private InputAction skill1Action;
        private InputAction skill2Action;
        private InputAction skill3Action;
        private InputAction toggleCharacterStatsAction;

        public Vector2 MoveInput { get; private set; }
        public bool AttackTriggered { get; private set; }
        public bool IsAttackHeld { get; private set; }
        public bool Skill1Triggered { get; private set; }
        public bool Skill1Held { get; private set; }
        public bool Skill1Released { get; private set; }
        public bool Skill2Triggered { get; private set; }
        public bool Skill2Held { get; private set; }
        public bool Skill2Released { get; private set; }
        public bool Skill3Triggered { get; private set; }
        public bool Skill3Held { get; private set; }
        public bool Skill3Released { get; private set; }
        public bool ToggleCharacterStatsTriggered { get; private set; }
        public bool IsUsingGamepad { get; private set; }

        private void Awake()
        {
            InitializeInputActions();
        }

        private void InitializeInputActions()
        {
            if (inputActions != null)
            {
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    moveAction = playerMap.FindAction("Move");
                    attackAction = playerMap.FindAction("Attack");
                    skill1Action = playerMap.FindAction("Skill1");
                    skill2Action = playerMap.FindAction("Skill2");
                    skill3Action = playerMap.FindAction("Skill3");
                    toggleCharacterStatsAction = playerMap.FindAction("ToggleCharacterStats") ?? playerMap.FindAction("CharacterStats");
                }
            }
        }

        private void OnEnable()
        {
            if (inputActions != null) inputActions.Enable();

            moveAction?.Enable();
            attackAction?.Enable();
            skill1Action?.Enable();
            skill2Action?.Enable();
            skill3Action?.Enable();
            toggleCharacterStatsAction?.Enable();
        }

        private void OnDisable()
        {
            if (inputActions != null) inputActions.Disable();

            moveAction?.Disable();
            attackAction?.Disable();
            skill1Action?.Disable();
            skill2Action?.Disable();
            skill3Action?.Disable();
            toggleCharacterStatsAction?.Disable();
        }

        private void Update()
        {
            // Di chuyển
            if (moveAction != null)
            {
                MoveInput = moveAction.ReadValue<Vector2>();
                if (moveAction.activeControl != null)
                {
                    IsUsingGamepad = moveAction.activeControl.device is Gamepad || moveAction.activeControl.device is Joystick;
                }
            }
            else
            {
                MoveInput = Vector2.zero;
            }

            // Đánh thường
            if (attackAction != null)
            {
                AttackTriggered = attackAction.WasPressedThisFrame();
                IsAttackHeld = attackAction.IsPressed();
                if (attackAction.activeControl != null && AttackTriggered)
                {
                    IsUsingGamepad = attackAction.activeControl.device is Gamepad || attackAction.activeControl.device is Joystick;
                }
            }
            else
            {
                AttackTriggered = false;
                IsAttackHeld = false;
            }

            // Kỹ năng 1, 2, 3
            if (skill1Action != null)
            {
                Skill1Triggered = skill1Action.WasPressedThisFrame();
                Skill1Held = skill1Action.IsPressed();
                Skill1Released = skill1Action.WasReleasedThisFrame();
            }
            if (skill2Action != null)
            {
                Skill2Triggered = skill2Action.WasPressedThisFrame();
                Skill2Held = skill2Action.IsPressed();
                Skill2Released = skill2Action.WasReleasedThisFrame();
            }
            if (skill3Action != null)
            {
                Skill3Triggered = skill3Action.WasPressedThisFrame();
                Skill3Held = skill3Action.IsPressed();
                Skill3Released = skill3Action.WasReleasedThisFrame();
            }

            // Bật/tắt bảng thuộc tính
            if (toggleCharacterStatsAction != null)
            {
                ToggleCharacterStatsTriggered = toggleCharacterStatsAction.WasPressedThisFrame();
            }
            else
            {
                // Fallback: nếu chưa config trong inputactions asset, vẫn hỗ trợ phím C hoặc Gamepad Select/Back
                ToggleCharacterStatsTriggered = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);
            }
        }

        public Vector3 GetMovementDirection3D()
        {
            return new Vector3(MoveInput.x, 0f, MoveInput.y);
        }
    }
}
