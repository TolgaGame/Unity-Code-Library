// Standalone: no other scripts needed.
// Shows FPS, frame time and memory on screen. Add it to any GameObject.
// Only active in the Editor and Development Builds, it does nothing in release builds.
// Optional: press the toggle key (or tap the screen with 3 fingers) to show / hide it.
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class FPSCounter : MonoBehaviour
{
    public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

    [Header("Display")]
    public Corner corner = Corner.TopLeft;
    public int fontSize = 18;
    public bool showMemory = true;
    public bool visible = true;

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.F3;

    [Min(0.1f)] public float updateInterval = 0.5f;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    readonly StringBuilder builder = new StringBuilder(128);
    GUIStyle style;
    string text = string.Empty;
    float timer;
    int frames;
    float lowestFps = float.MaxValue;

    void Update()
    {
        if (TogglePressed())
            visible = !visible;

        // Unscaled time, so the counter is correct even when timeScale is changed.
        float deltaTime = Time.unscaledDeltaTime;
        timer += deltaTime;
        frames++;
        if (deltaTime > 0f)
            lowestFps = Mathf.Min(lowestFps, 1f / deltaTime);

        if (timer < updateInterval)
            return;

        float fps = frames / timer;
        float frameMs = timer / frames * 1000f;

        builder.Clear();
        builder.Append("FPS: ").Append(fps.ToString("0")).Append(" (min ").Append(lowestFps.ToString("0")).Append(")\n");
        builder.Append("Frame: ").Append(frameMs.ToString("0.0")).Append(" ms");
        if (showMemory)
        {
            builder.Append("\nMemory: ").Append((Profiler.GetTotalAllocatedMemoryLong() / 1048576f).ToString("0.0")).Append(" MB");
            builder.Append("\nMono: ").Append((System.GC.GetTotalMemory(false) / 1048576f).ToString("0.0")).Append(" MB");
        }
        text = builder.ToString();

        timer = 0f;
        frames = 0;
        lowestFps = float.MaxValue;
    }

    bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current != null && toggleKey == KeyCode.F3 && Keyboard.current.f3Key.wasPressedThisFrame)
            return true;
        return Touchscreen.current != null && Touchscreen.current.touches.Count >= 3 &&
               Touchscreen.current.touches[2].press.wasPressedThisFrame;
#else
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            return true;
        return Input.touchCount >= 3 && Input.GetTouch(2).phase == TouchPhase.Began;
#endif
    }

    void OnGUI()
    {
        if (!visible || string.IsNullOrEmpty(text))
            return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { richText = false };
            style.normal.textColor = Color.white;
        }

        style.fontSize = fontSize;
        style.alignment = (corner == Corner.TopRight || corner == Corner.BottomRight)
            ? TextAnchor.UpperRight
            : TextAnchor.UpperLeft;

        Vector2 size = style.CalcSize(new GUIContent(text));
        const float margin = 10f;
        float x = (corner == Corner.TopLeft || corner == Corner.BottomLeft) ? margin : Screen.width - size.x - margin;
        float y = (corner == Corner.TopLeft || corner == Corner.TopRight) ? margin : Screen.height - size.y - margin;

        // Keep it readable on mobile: stay inside the safe area (notch).
        Rect safeArea = Screen.safeArea;
        x = Mathf.Clamp(x, safeArea.xMin, Mathf.Max(safeArea.xMin, safeArea.xMax - size.x));
        y = Mathf.Clamp(y, Screen.height - safeArea.yMax, Mathf.Max(Screen.height - safeArea.yMax, Screen.height - safeArea.yMin - size.y));

        Rect rect = new Rect(x, y, size.x, size.y);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(rect.x - 4f, rect.y - 2f, rect.width + 8f, rect.height + 4f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(rect, text, style);
    }
#endif
}
