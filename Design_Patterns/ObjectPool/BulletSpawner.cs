using UnityEngine;

/// <summary>
/// Example pool user. Fires automatically on an interval, or call Fire() yourself from a Button
/// or from your input code (works with both the old and the new Input System).
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private GameObjectPool pool;
    [SerializeField] private Transform muzzle;
    [SerializeField] private bool autoFire = true;
    [SerializeField] private float fireInterval = 0.2f;

    private float _nextFireTime;

    private void Update()
    {
        if (autoFire && Time.time >= _nextFireTime)
        {
            Fire();
        }
    }

    public void Fire()
    {
        _nextFireTime = Time.time + fireInterval;

        Transform origin = muzzle != null ? muzzle : transform;

        // Get() returns null when the pool is capped and every object is in use.
        pool.Get(origin.position, origin.rotation);
    }
}
