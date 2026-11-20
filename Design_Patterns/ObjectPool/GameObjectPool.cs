using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reuses GameObjects instead of calling Instantiate / Destroy again and again.
/// Instantiate and Destroy allocate memory and cause garbage collection spikes,
/// which show up as stutter in bullet, particle, enemy and coin heavy games.
///
/// One pool = one prefab. Keep the pool object at position (0,0,0), rotation 0 and scale 1,
/// because inactive objects are stored as its children.
/// </summary>
public class GameObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    [Tooltip("Instances created up front so the first spawns do not allocate.")]
    [SerializeField] private int prewarmCount = 10;

    [Tooltip("Maximum number of instances that may exist at once. 0 means unlimited.")]
    [SerializeField] private int maxSize = 0;

    private readonly Stack<PooledObject> _inactive = new Stack<PooledObject>();
    private int _totalCreated;

    public int InactiveCount => _inactive.Count;
    public int ActiveCount => _totalCreated - _inactive.Count;

    private void Awake()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            if (maxSize > 0 && _totalCreated >= maxSize)
                break;

            _inactive.Push(CreateInstance());
        }
    }

    /// <summary>Takes an object from the pool (or creates one). Returns null when maxSize is reached.</summary>
    public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        PooledObject item = null;

        while (_inactive.Count > 0)
        {
            item = _inactive.Pop();
            if (item != null)
                break;

            // Someone destroyed a stored object, so it no longer counts.
            _totalCreated--;
        }

        if (item == null)
        {
            if (maxSize > 0 && _totalCreated >= maxSize)
            {
                Debug.LogWarning($"[GameObjectPool] '{name}' reached its max size ({maxSize}).");
                return null;
            }

            item = CreateInstance();
        }

        Transform t = item.transform;
        if (parent != null)
        {
            t.SetParent(parent, false);
        }

        t.SetPositionAndRotation(position, rotation);
        item.Spawn();

        return item.gameObject;
    }

    /// <summary>Returns an object to the pool. Objects that did not come from a pool are destroyed.</summary>
    public static void Release(GameObject instance)
    {
        if (instance == null)
            return;

        if (instance.TryGetComponent(out PooledObject pooled))
        {
            pooled.Release();
        }
        else
        {
            Destroy(instance);
        }
    }

    // Called by PooledObject.Release.
    internal void ReturnToPool(PooledObject item)
    {
        item.Despawn();

        // Back under the pool so the hierarchy stays tidy.
        item.transform.SetParent(transform, false);
        _inactive.Push(item);
    }

    private PooledObject CreateInstance()
    {
        GameObject instance = Instantiate(prefab, transform);
        instance.SetActive(false);

        if (!instance.TryGetComponent(out PooledObject pooled))
        {
            pooled = instance.AddComponent<PooledObject>();
        }

        pooled.Init(this);
        _totalCreated++;
        return pooled;
    }
}
