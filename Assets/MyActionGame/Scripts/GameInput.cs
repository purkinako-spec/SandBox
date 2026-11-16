using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MyActionGame
{
    public static class GameInput
    {
        public static Vector2 MoveAxis
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    float x = 0f;
                    float z = 0f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) z -= 1f;
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) z += 1f;
                    return Vector2.ClampMagnitude(new Vector2(x, z), 1f);
                }
#endif
                return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
        }

        public static Vector2 MouseDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Mouse mouse = Mouse.current;
                if (mouse != null)
                    return mouse.delta.ReadValue();
#endif
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
        }

        public static bool FireHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Mouse mouse = Mouse.current;
                if (mouse != null)
                    return mouse.leftButton.isPressed;
#endif
                return Input.GetButton("Fire1");
            }
        }

        public static bool JumpPressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                    return keyboard.spaceKey.wasPressedThisFrame;
#endif
                return Input.GetButtonDown("Jump");
            }
        }

        public static bool RestartPressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                    return keyboard.rKey.wasPressedThisFrame;
#endif
                return Input.GetKeyDown(KeyCode.R);
            }
        }

        public static bool UnlockCursorPressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                    return keyboard.escapeKey.wasPressedThisFrame;
#endif
                return Input.GetKeyDown(KeyCode.Escape);
            }
        }
    }
}
