using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Detects tap, double tap and long press on a UI element (or a full-screen transparent Image).
/// Uses UI pointer events, so it works with the old and the new Input System, with touch and mouse.
///
/// Note: a double tap raises OnTap for the first tap and then OnDoubleTap for the second one.
/// If you need "tap OR double tap", delay your tap reaction by doubleTapInterval.
/// </summary>
public class TapDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("A press longer than this is not a tap.")]
    [SerializeField] private float maxTapSeconds = 0.25f;

    [Tooltip("Two taps within this time make a double tap.")]
    [SerializeField] private float doubleTapInterval = 0.3f;

    [Tooltip("Holding at least this long raises OnLongPress.")]
    [SerializeField] private float longPressSeconds = 0.6f;

    [Tooltip("Finger movement allowed during a tap, in inches.")]
    [SerializeField] private float maxMoveInches = 0.1f;

    public event Action<Vector2> OnTap;
    public event Action<Vector2> OnDoubleTap;
    public event Action<Vector2> OnLongPress;

    private const int NoPointer = int.MinValue;

    private int _pointerId = NoPointer;
    private Vector2 _downPosition;
    private float _downTime;
    private float _lastTapTime = float.NegativeInfinity;
    private bool _longPressFired;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_pointerId != NoPointer)
            return;

        _pointerId = eventData.pointerId;
        _downPosition = eventData.position;
        _downTime = Time.unscaledTime;
        _longPressFired = false;
    }

    private void Update()
    {
        // The long press fires while the finger is still down, so it cannot wait for the release.
        if (_pointerId != NoPointer && !_longPressFired &&
            Time.unscaledTime - _downTime >= longPressSeconds)
        {
            _longPressFired = true;
            OnLongPress?.Invoke(_downPosition);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != _pointerId)
            return;

        _pointerId = NoPointer;

        if (_longPressFired)
            return;

        float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        bool movedTooFar = (eventData.position - _downPosition).magnitude / dpi > maxMoveInches;
        bool heldTooLong = Time.unscaledTime - _downTime > maxTapSeconds;

        if (movedTooFar || heldTooLong)
            return;

        if (Time.unscaledTime - _lastTapTime <= doubleTapInterval)
        {
            // Reset so a third quick tap starts a new sequence instead of another double tap.
            _lastTapTime = float.NegativeInfinity;
            OnDoubleTap?.Invoke(eventData.position);
        }
        else
        {
            _lastTapTime = Time.unscaledTime;
            OnTap?.Invoke(eventData.position);
        }
    }

    private void OnDisable()
    {
        _pointerId = NoPointer;
    }
}
