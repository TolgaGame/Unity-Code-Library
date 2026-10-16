using UnityEngine;

/// <summary>
/// Base class for states that live as assets in the Project window.
/// IMPORTANT: one asset is shared by every runner that uses it, so a state must be STATELESS.
/// Never store per-object data (timers, counters, references) in a field here.
/// Per-object data lives on the ScriptableStateRunner.
/// </summary>
public abstract class ScriptableState : ScriptableObject
{
    public virtual void OnEnter(ScriptableStateRunner runner) { }
    public virtual void OnTick(ScriptableStateRunner runner) { }
    public virtual void OnExit(ScriptableStateRunner runner) { }
}
