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
        private InputAction toggleSkillBookAction;

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
        public bool ToggleSkillBookTriggered { get; private set; }
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
                    toggleSkillBookAction = playerMap.FindAction("ToggleSkillBook") ?? playerMap.FindAction("SkillBook") ?? playerMap.FindAction("Skills");
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
            toggleSkillBookAction?.Enable();
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
            toggleSkillBookAction?.Disable();
        }

        public static bool IsPointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null && 
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        private void Update()
        {
            // Bật/tắt bảng thuộc tính (luôn nhận diện phím tắt)
            if (toggleCharacterStatsAction != null)
            {
                ToggleCharacterStatsTriggered = toggleCharacterStatsAction.WasPressedThisFrame();
            }
            else
            {
                ToggleCharacterStatsTriggered = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);
            }

            // Bật/tắt bảng kỹ năng (luôn nhận diện phím tắt)
            if (toggleSkillBookAction != null)
            {
                ToggleSkillBookTriggered = toggleSkillBookAction.WasPressedThisFrame();
            }
            else
            {
                ToggleSkillBookTriggered = (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame);
            }

            // Xử lý phím ESC đóng cửa sổ UI đang mở
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UI.UIModalManager.CloseTopModal();
            }

            // Nếu đang mở cửa sổ Modal UI (Stat, Skillbook...), khóa toàn bộ input chiến đấu & di chuyển
            if (UI.UIModalManager.IsAnyModalOpen)
            {
                MoveInput = Vector2.zero;
                AttackTriggered = false;
                IsAttackHeld = false;
                Skill1Triggered = false;
                Skill1Held = false;
                Skill1Released = false;
                Skill2Triggered = false;
                Skill2Held = false;
                Skill2Released = false;
                Skill3Triggered = false;
                Skill3Held = false;
                Skill3Released = false;
                return;
            }

            bool pointerOverUI = IsPointerOverUI();

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

            // Đánh thường (Chặn click chuột khi đang trỏ trên UI)
            if (attackAction != null)
            {
                bool rawTrigger = attackAction.WasPressedThisFrame();
                bool rawHeld = attackAction.IsPressed();
                bool isMouse = attackAction.activeControl != null && attackAction.activeControl.device is Mouse;

                if (pointerOverUI && (isMouse || !IsUsingGamepad))
                {
                    AttackTriggered = false;
                    IsAttackHeld = false;
                }
                else
                {
                    AttackTriggered = rawTrigger;
                    IsAttackHeld = rawHeld;
                }

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
        }

        public Vector3 GetMovementDirection3D()
        {
            return new Vector3(MoveInput.x, 0f, MoveInput.y);
        }
    }
}
