using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Reads keyboard and gamepad input without caring which Input System the project uses
/// (Project Settings > Player > Active Input Handling).
///
///   New Input System package  -> uses Keyboard.current / Gamepad.current
///   Old Input Manager         -> uses UnityEngine.Input
///
/// Calling the wrong API throws errors (InvalidOperationException) as soon as the setting does not match,
/// which is the most common input problem for beginners. This helper hides that choice in one place.
/// </summary>
public static class InputReader
{
    /// <summary>WASD, arrow keys or the left stick. Length is never more than 1.</summary>
    public static Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 move = Vector2.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            move += gamepad.leftStick.ReadValue();
        }

        return Vector2.ClampMagnitude(move, 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        // "Horizontal" and "Vertical" are the default axes: WASD, arrows and the left stick.
        return Vector2.ClampMagnitude(
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
        return Vector2.zero;
#endif
    }

    /// <summary>True only on the frame the Space key or the gamepad's south button was pressed.</summary>
    public static bool JumpPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool button = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        return key || button;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
#else
        return false;
#endif
    }
}
