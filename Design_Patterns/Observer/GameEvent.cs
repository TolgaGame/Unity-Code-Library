using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject event channel: the SUBJECT, stored as an asset.
/// Create one with Create > Design Patterns > Game Event (for example "OnLevelCompleted").
/// Neither the raiser nor the listeners need a reference to each other, only to the same asset,
/// so systems in different scenes and prefabs stay fully decoupled.
/// </summary>
[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Design Patterns/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<GameEventListener> _listeners = new List<GameEventListener>();

    /// <summary>Call this from anywhere to notify every registered listener.</summary>
    public void Raise()
    {
        // Backwards loop: a listener may unregister itself while responding.
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            if (i >= _listeners.Count)
                continue;

            _listeners[i].OnEventRaised();
        }
    }

    public void Register(GameEventListener listener)
    {
        if (listener != null && !_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void Unregister(GameEventListener listener)
    {
        _listeners.Remove(listener);
    }
}
