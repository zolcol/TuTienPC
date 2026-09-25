using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

namespace TopDownGame.Camera
{
    public class CinemachineDragInputProvider : MonoBehaviour, AxisState.IInputAxisProvider
    {
        public enum MouseButtonOption
        {
            RightClick = 1,  // Chuột phải (Chuẩn RPG/Genshin/MMO)
            LeftClick = 0,   // Chuột trái
            MiddleClick = 2  // Chuột giữa (Con lăn)
        }

        [Header("Mouse Settings")]
        [Tooltip("Phím chuột cần giữ để xoay Camera (Khuyên dùng: Chuột phải để không vướng nút đánh thường)")]
        [SerializeField] private MouseButtonOption mouseButton = MouseButtonOption.RightClick;
        [SerializeField] private float mouseSensitivityX = 0.5f;
        [SerializeField] private float mouseSensitivityY = 0.5f;

        [Header("Gamepad Right Stick Settings")]
        [SerializeField] private float gamepadSpeedX = 200f;
        [SerializeField] private float gamepadSpeedY = 2f;

        public float GetAxisValue(int axis)
        {
            // 1. Với Tay cầm Gamepad: Cần gạt phải (Right Stick) luôn xoay tự do
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.01f)
                {
                    if (axis == 0) return stick.x * gamepadSpeedX * Time.deltaTime;
                    if (axis == 1) return stick.y * gamepadSpeedY * Time.deltaTime;
                }
            }

            // 2. Với Chuột: CHỈ XOAY KHI ĐANG GIỮ CHUỘT
            if (Mouse.current != null)
            {
                bool isPressed = false;
                switch (mouseButton)
                {
                    case MouseButtonOption.LeftClick:
                        isPressed = Mouse.current.leftButton.isPressed;
                        break;
                    case MouseButtonOption.RightClick:
                        isPressed = Mouse.current.rightButton.isPressed;
                        break;
                    case MouseButtonOption.MiddleClick:
                        isPressed = Mouse.current.middleButton.isPressed;
                        break;
                }

                if (isPressed)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    if (axis == 0) return mouseDelta.x * mouseSensitivityX;
                    if (axis == 1) return mouseDelta.y * mouseSensitivityY;
                }
            }

            return 0f;
        }
    }
}
