using System.Collections;
using UnityEngine;

/// <summary>
/// Added automatically by GameObjectPool to every instance it creates.
/// It remembers which pool the object belongs to, so the object can send itself back
/// (<c>GetComponent&lt;PooledObject&gt;().Release()</c>) without knowing about the pool.
/// </summary>
public class PooledObject : MonoBehaviour
{
    private GameObjectPool _pool;
    private IPoolable[] _poolables;

    /// <summary>True while the object is stored in the pool, false while it is in use.</summary>
    public bool IsInPool { get; private set; } = true;

    internal void Init(GameObjectPool pool)
    {
        _pool = pool;

        // Cached once so spawning and despawning never allocate.
        _poolables = GetComponentsInChildren<IPoolable>(true);
    }

    internal void Spawn()
    {
        IsInPool = false;
        gameObject.SetActive(true);

        for (int i = 0; i < _poolables.Length; i++)
        {
            _poolables[i].OnSpawn();
        }
    }

    internal void Despawn()
    {
        for (int i = 0; i < _poolables.Length; i++)
        {
            _poolables[i].OnDespawn();
        }

        gameObject.SetActive(false);
        IsInPool = true;
    }

    /// <summary>Sends the object back to its pool. Safe to call twice.</summary>
    public void Release()
    {
        // Releasing twice would put the same object in the pool twice.
        if (IsInPool)
            return;

        if (_pool == null)
        {
            // The pool was destroyed (for example on scene change), so nothing can reuse this object.
            Destroy(gameObject);
            return;
        }

        _pool.ReturnToPool(this);
    }

    /// <summary>Releases the object after a delay. The timer stops by itself if the object is released earlier.</summary>
    public void ReleaseAfter(float seconds)
    {
        StartCoroutine(ReleaseRoutine(seconds));
    }

    private IEnumerator ReleaseRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Release();
    }
}
