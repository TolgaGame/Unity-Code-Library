using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ScriptableObject event channel: the OBSERVER.
/// Add it to any GameObject, drag a GameEvent asset into the slot,
/// and wire the response in the Inspector (no code needed).
/// </summary>
public class GameEventListener : MonoBehaviour
{
    [SerializeField] private GameEvent gameEvent;
    [SerializeField] private UnityEvent response;

    private void OnEnable()
    {
        if (gameEvent != null)
            gameEvent.Register(this);
    }

    private void OnDisable()
    {
        if (gameEvent != null)
            gameEvent.Unregister(this);
    }

    public void OnEventRaised()
    {
        response?.Invoke();
    }
}
