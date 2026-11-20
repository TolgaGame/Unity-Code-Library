using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On-screen joystick for mobile games. Read <see cref="Direction"/> every frame.
///
/// Setup: Canvas > Image "Background" (a circle) with this script, and a child Image "Handle" (a smaller circle).
/// Both anchors and pivots stay in the center. Make sure the scene has an EventSystem.
/// Works with the old and the new Input System because it uses UI pointer events.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Tooltip("Input below this length is treated as zero, which avoids drifting.")]
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;

    /// <summary>Value between (-1,-1) and (1,1) with a maximum length of 1. Zero when released.</summary>
    public Vector2 Direction { get; private set; }

    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Overlay canvases must not receive a camera.
        Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventCamera, out Vector2 localPoint))
            return;

        float radius = Mathf.Min(background.rect.width, background.rect.height) * 0.5f;
        Vector2 normalized = Vector2.ClampMagnitude(localPoint / radius, 1f);

        Direction = normalized.magnitude < deadZone ? Vector2.zero : normalized;

        // The handle follows the finger but never leaves the circle.
        handle.anchoredPosition = normalized * radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    private void OnDisable()
    {
        // Without this the character keeps walking if the joystick is hidden while held.
        ResetJoystick();
    }

    private void ResetJoystick()
    {
        Direction = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
