using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum SwipeDirection { Up, Down, Left, Right }

/// <summary>
/// Detects four-direction swipes. Put it on a full-screen UI Image (Raycast Target on, alpha can be 0)
/// under a Canvas, and make sure the scene has an EventSystem.
/// It uses the UI pointer events, so it works with the old and the new Input System, with touch and mouse.
/// </summary>
public class SwipeDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Minimum finger travel. Measured in inches so it feels the same on every screen density.")]
    [SerializeField] private float minSwipeInches = 0.3f;

    [Tooltip("A slower drag is a normal drag, not a swipe.")]
    [SerializeField] private float maxSwipeSeconds = 0.6f;

    /// <summary>Raised once when a swipe finishes.</summary>
    public event Action<SwipeDirection> OnSwipe;

    private const int NoPointer = int.MinValue;

    private int _pointerId = NoPointer;
    private Vector2 _startPosition;
    private float _startTime;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Follow one finger only, so a second finger cannot corrupt the gesture.
        if (_pointerId != NoPointer)
            return;

        _pointerId = eventData.pointerId;
        _startPosition = eventData.position;
        _startTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != _pointerId)
            return;

        _pointerId = NoPointer;

        if (Time.unscaledTime - _startTime > maxSwipeSeconds)
            return;

        Vector2 delta = eventData.position - _startPosition;

        // Screen.dpi can be 0 in the Editor and on some devices.
        float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        if (delta.magnitude / dpi < minSwipeInches)
            return;

        // The longer axis decides the direction.
        SwipeDirection direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? (delta.x > 0f ? SwipeDirection.Right : SwipeDirection.Left)
            : (delta.y > 0f ? SwipeDirection.Up : SwipeDirection.Down);

        OnSwipe?.Invoke(direction);
    }

    private void OnDisable()
    {
        _pointerId = NoPointer;
    }
}
