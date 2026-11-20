using UnityEngine;

/// <summary>
/// Singleton that survives scene loads. Standalone: does not need Singleton.cs.
/// Used by the Firebase friend system and the Tutorial system.
/// Override Awake only if you call base.Awake() first.
/// </summary>
public abstract class PersistentSingleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<T>();
#else
                instance = FindObjectOfType<T>();
#endif
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}
