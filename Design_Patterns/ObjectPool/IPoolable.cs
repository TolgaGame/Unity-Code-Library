/// <summary>
/// Optional. Put it on any script of a pooled object to get notified when the object is
/// taken from the pool (OnSpawn) and when it goes back (OnDespawn).
/// Use OnSpawn to reset state, because a pooled object is reused and Awake / Start do not run again.
/// </summary>
public interface IPoolable
{
    /// <summary>Called right after the object was activated and positioned.</summary>
    void OnSpawn();

    /// <summary>Called right before the object is deactivated and stored again.</summary>
    void OnDespawn();
}
