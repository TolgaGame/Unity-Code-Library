using UnityEngine;

/// <summary>
/// Example pooled object. Flies forward and returns itself to the pool after a lifetime or on a hit.
/// Add a Collider (Is Trigger) and a kinematic Rigidbody so OnTriggerEnter is called.
/// </summary>
public class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 3f;

    private PooledObject _pooled;
    private float _age;

    public void OnSpawn()
    {
        // The pool adds PooledObject right after Instantiate, i.e. after the first Awake, so look it up again here.
        // It stays null when the bullet is used without a pool; Despawn then falls back to Destroy.
        if (_pooled == null)
        {
            _pooled = GetComponent<PooledObject>();
        }

        // Pooled objects are reused, so every field must be reset here.
        _age = 0f;
    }

    public void OnDespawn() { }

    private void Update()
    {
        transform.Translate(Vector3.forward * (speed * Time.deltaTime));

        _age += Time.deltaTime;
        if (_age >= lifetime)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Despawn();
    }

    private void Despawn()
    {
        if (_pooled != null)
            _pooled.Release();
        else
            Destroy(gameObject);
    }
}
