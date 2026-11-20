# Input Examples

> **Touch, mouse, keyboard and gamepad input that works with both Unity Input Systems.**

---

## Files

| File | What it does |
|------|--------------|
| [SwipeDetector.cs](SwipeDetector.cs) | Four-direction swipe detection. Event: `OnSwipe(SwipeDirection)` |
| [TapDetector.cs](TapDetector.cs) | Tap, double tap and long press. Events: `OnTap`, `OnDoubleTap`, `OnLongPress` |
| [VirtualJoystick.cs](VirtualJoystick.cs) | On-screen joystick. Read `Direction` (Vector2) in `Update` |
| [InputReader.cs](InputReader.cs) | Static helper for keyboard / gamepad movement and jump, for both Input Systems |
| [InputDemoMover.cs](InputDemoMover.cs) | Example that moves an object with the joystick, keyboard, gamepad and swipes |

---

## Setup

### Swipe, Tap, Joystick (UI based)

These three scripts use `IPointerDownHandler`, `IDragHandler` and `IPointerUpHandler`. They need:

1. A **Canvas** in the scene.
2. An **EventSystem** in the scene. With the new Input System package, use the **Input System UI Input Module** on it (Unity shows a button to replace the old module).
3. An **Image** with **Raycast Target** enabled on the object that has the script. For swipe / tap, use a full-screen Image. The alpha can be 0, but the Image must exist.

Because they use UI events, they work with touch **and** mouse, and with both Input Systems.

### Joystick

```
Canvas
└── JoystickBackground  (Image circle + VirtualJoystick)
    └── Handle          (smaller Image circle)
```

Keep anchors and pivots of both images in the center, then assign `background` and `handle`.

### Reading input in code

```csharp
[SerializeField] private SwipeDetector swipe;

void OnEnable()  => swipe.OnSwipe += HandleSwipe;
void OnDisable() => swipe.OnSwipe -= HandleSwipe;   // always unsubscribe

void HandleSwipe(SwipeDirection dir) { Debug.Log(dir); }
```

```csharp
Vector2 move = InputReader.ReadMove();         // WASD / arrows / left stick
if (InputReader.JumpPressedThisFrame()) Jump();
```

---

## Which Input System do I have?

Check **Project Settings > Player > Other Settings > Active Input Handling**:

| Setting | `InputReader` uses |
|---------|--------------------|
| Input Manager (Old) | `UnityEngine.Input` |
| Input System Package (New) | `Keyboard.current`, `Gamepad.current` |
| Both | New Input System |

If you see `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class...`, your code uses the old API while the project uses the new one. `InputReader` avoids this because it picks the API at compile time.

---

## Common mistakes

| Mistake | Result |
|---------|--------|
| No EventSystem in the scene | Nothing is detected |
| Image has Raycast Target turned off | Nothing is detected |
| Another UI element covers the swipe area | It takes the touch instead |
| Forgetting to unsubscribe from events | Errors after the scene reloads |
| Using pixels for swipe distance | Behaves differently on every phone. These scripts use inches (DPI) instead |

---

## Dependencies

- `InputReader.cs` uses `UnityEngine.InputSystem` only when the **Input System** package is installed (`ENABLE_INPUT_SYSTEM`).
- No other dependencies on this library.
