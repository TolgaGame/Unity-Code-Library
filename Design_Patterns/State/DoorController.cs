using System;
using UnityEngine;

/// <summary>
/// Enum based state machine (the lightest version).
/// A door has four simple states, so one enum and one switch are enough.
/// Put the door's pivot at the hinge (or nest the door under a hinge object).
/// </summary>
public class DoorController : MonoBehaviour
{
    public enum DoorState { Closed, Opening, Open, Closing }

    [Header("Motion")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotateSpeed = 120f; // degrees per second

    [Header("Behavior")]
    [Tooltip("Seconds the door stays open before closing by itself. 0 = never closes by itself.")]
    [SerializeField] private float autoCloseDelay = 3f;

    public DoorState State { get; private set; } = DoorState.Closed;

    /// <summary>Raised after every state change. Other scripts (audio, UI) can subscribe.</summary>
    public event Action<DoorState> OnStateChanged;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private float _openTimer;

    private void Awake()
    {
        _closedRotation = transform.localRotation;
        _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        switch (State)
        {
            case DoorState.Opening:
                if (RotateTowards(_openRotation))
                    SetState(DoorState.Open);
                break;

            case DoorState.Closing:
                if (RotateTowards(_closedRotation))
                    SetState(DoorState.Closed);
                break;

            case DoorState.Open:
                if (autoCloseDelay > 0f)
                {
                    _openTimer += Time.deltaTime;
                    if (_openTimer >= autoCloseDelay)
                        SetState(DoorState.Closing);
                }
                break;

            case DoorState.Closed:
                break; // nothing to do
        }
    }

    /// <summary>
    /// Call this from your interaction code. Also available from the component's context menu in Play mode.
    /// Interacting while the door is moving reverses it.
    /// </summary>
    [ContextMenu("Interact")]
    public void Interact()
    {
        switch (State)
        {
            case DoorState.Closed:
            case DoorState.Closing:
                SetState(DoorState.Opening);
                break;

            case DoorState.Open:
            case DoorState.Opening:
                SetState(DoorState.Closing);
                break;
        }
    }

    /// <summary>The only place that writes State, so every change goes through the same gate.</summary>
    private void SetState(DoorState next)
    {
        if (State == next) return;

        State = next;
        EnterState(next);
        OnStateChanged?.Invoke(next);
    }

    private void EnterState(DoorState state)
    {
        switch (state)
        {
            case DoorState.Open:
                _openTimer = 0f; // restart the auto close countdown
                break;

            // Good place for per-state side effects: play a creak sound on Opening, a thud on Closed, and so on.
        }
    }

    private bool RotateTowards(Quaternion target)
    {
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, target, rotateSpeed * Time.deltaTime);

        return Quaternion.Angle(transform.localRotation, target) < 0.01f;
    }
}
