# Object Pool Pattern in Unity

> **Create objects once, then reuse them. Instead of `Instantiate` and `Destroy` every time, switch objects off and on again.**

---

## Table of Contents

1. [The problem it solves](#1-the-problem-it-solves)
2. [How the pattern works](#2-how-the-pattern-works)
3. [File overview](#3-file-overview)
4. [Scene setup](#4-scene-setup)
5. [Making your own pooled object](#5-making-your-own-pooled-object)
6. [Rules to avoid bugs](#6-rules-to-avoid-bugs)
7. [Common mistakes](#7-common-mistakes)
8. [Unity's built-in pool](#8-unitys-built-in-pool)
9. [Dependencies](#9-dependencies)

---

## 1. The problem it solves

A shooter fires 10 bullets per second:

```csharp
void Fire()
{
    var bullet = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
    Destroy(bullet, 3f);
}
```

Every `Instantiate` allocates memory and every `Destroy` leaves garbage behind. The garbage collector eventually runs and the game **stutters**, especially on mobile. The same happens with particles, enemies, coins, damage numbers and UI items.

---

## 2. How the pattern works

```
        Get()                              Release()
  ┌──────────────┐   take one    ┌──────────────────┐   give back   ┌──────────────┐
  │  Spawner     │ ────────────► │  GameObjectPool  │ ◄──────────── │  Bullet      │
  └──────────────┘               │  [inactive][..]  │               └──────────────┘
                                 └──────────────────┘
          empty? → create one (up to maxSize)
```

1. At start the pool **prewarms**: it creates some inactive objects.
2. `Get()` takes one from the pool, positions it and activates it. If the pool is empty it creates a new one.
3. When the object is finished it calls `Release()`: it is deactivated and stored again.

| Role | Class | Responsibility |
|---|---|---|
| **Pool** | `GameObjectPool` | Creates, stores and hands out instances of one prefab |
| **Pooled object** | `PooledObject` | Added automatically. Knows its pool, so the object can release itself |
| **Callbacks** | `IPoolable` | `OnSpawn` / `OnDespawn` to reset state |
| **Client** | `BulletSpawner` | Calls `pool.Get(...)` |
| **Example item** | `Bullet` | Flies, then releases itself |

---

## 3. File overview

| File | Role |
|---|---|
| `GameObjectPool.cs` | The pool: `Get`, static `Release`, prewarm, optional `maxSize` |
| `PooledObject.cs` | Per-instance helper: `Release()`, `ReleaseAfter(seconds)`, `IsInPool` |
| `IPoolable.cs` | Optional `OnSpawn` / `OnDespawn` callbacks |
| `Bullet.cs` | Example pooled object |
| `BulletSpawner.cs` | Example client that fires bullets |

---

## 4. Scene setup

1. Create a Bullet prefab: a small Sphere or Cube with `Bullet`, a Collider (**Is Trigger** on) and a Rigidbody (**Is Kinematic** on).
2. Create an empty GameObject named `BulletPool` at position (0,0,0), add `GameObjectPool`, assign the prefab and set **Prewarm Count** (for example 20).
3. Create another GameObject, add `BulletSpawner`, assign the **Pool** and a **Muzzle** (any child Transform that points forward, its blue Z axis).
4. Press Play. The pool object's children are the bullets. Watch them switch on and off in the Hierarchy instead of being created and destroyed.

To fire on a key press or a button, turn **Auto Fire** off and call `BulletSpawner.Fire()` from your input code or a UI Button.

---

## 5. Making your own pooled object

You only need the prefab and a pool. To reset state when the object is reused, implement `IPoolable`:

```csharp
public class Enemy : MonoBehaviour, IPoolable
{
    private int _health;

    public void OnSpawn()   => _health = 100;     // reset everything here
    public void OnDespawn() { /* stop effects, clear targets */ }
}
```

Get and release:

```csharp
GameObject enemy = pool.Get(spawnPoint.position, spawnPoint.rotation);

// when it dies:
enemy.GetComponent<PooledObject>().Release();
// or from anywhere:
GameObjectPool.Release(enemy);
// or automatically after 2 seconds:
enemy.GetComponent<PooledObject>().ReleaseAfter(2f);
```

---

## 6. Rules to avoid bugs

- **Reset state in `OnSpawn`, not in `Awake` / `Start`.** Those only run once. A reused object would keep its old health, velocity, timers and effects.
- **Never call `Destroy` on a pooled object.** Release it. The pool would otherwise keep a reference to a destroyed object (it skips destroyed ones, but the object is lost).
- **Release exactly once.** `PooledObject.Release()` ignores a second call, but your logic should not depend on that.
- **Prewarm enough.** Choose a number close to what you need at the same time, so the game does not create objects mid-fight.
- **Set a `maxSize` for things that can spawn endlessly.** `Get()` then returns `null`, so always check the result if a cap is set.
- **Keep the pool at identity.** Inactive objects are stored as its children, so a scaled pool scales them.
- **One pool per prefab.** Do not put different prefabs in one pool.

---

## 7. Common mistakes

| Mistake | Symptom | Fix |
|---|---|---|
| Resetting values in `Start` | Reused object behaves like the previous one (old health, old velocity) | Move the reset to `OnSpawn` |
| `Destroy(gameObject)` on a pooled object | Pool shrinks, spikes return | Call `Release()` |
| Forgetting to reset a Rigidbody | Reused object keeps flying | Set `velocity` / `angularVelocity` to zero in `OnSpawn` |
| Not stopping coroutines or particles | Old effects play on the next use | Clean up in `OnDespawn` |
| Ignoring a `null` from `Get()` | `NullReferenceException` when `maxSize` is reached | Check for `null` |
| Pool object scaled or moved | Spawned objects look wrong | Keep it at position 0, rotation 0, scale 1 |
| Trigger hits the shooter's own collider | Bullet disappears immediately | Use layers / the collision matrix |

---

## 8. Unity's built-in pool

Unity 2021.1 and newer ships `UnityEngine.Pool.ObjectPool<T>` (also `ListPool`, `DictionaryPool`). It is a good choice when you want a generic pool for any C# class, not just GameObjects:

```csharp
using UnityEngine.Pool;

private ObjectPool<GameObject> _pool;

void Awake()
{
    _pool = new ObjectPool<GameObject>(
        createFunc:      () => Instantiate(prefab),
        actionOnGet:     go => go.SetActive(true),
        actionOnRelease: go => go.SetActive(false),
        actionOnDestroy: go => Destroy(go),
        defaultCapacity: 10,
        maxSize: 50);
}
```

This folder's `GameObjectPool` does the same thing by hand, so you can see how a pool works and add features such as per-instance callbacks (`IPoolable`) and self-releasing objects (`PooledObject`).

---

## 9. Dependencies

- Only `UnityEngine`. No singleton or third-party code. No namespaces; add one if your project needs it.
- The example bullet needs a Collider and a Rigidbody for `OnTriggerEnter`.
