# Observer Pattern in Unity

> **One object (the *subject*) announces that something changed. Any number of other objects (the *observers*) react to it, and the subject never needs to know who they are.**

This guide explains the pattern, shows how each implementation in this folder works, and gives step-by-step usage instructions.

---

## Table of Contents

1. [The problem it solves](#1-the-problem-it-solves)
2. [How the pattern works](#2-how-the-pattern-works)
3. [Which version should I use?](#3-which-version-should-i-use)
4. [File overview](#4-file-overview)
5. [Version 1: Interface based](#5-version-1-interface-based)
6. [Version 2: C# `event`](#6-version-2-c-event)
7. [Version 3: ScriptableObject event channel](#7-version-3-scriptableobject-event-channel)
8. [Rules to avoid bugs](#8-rules-to-avoid-bugs)
9. [Common mistakes](#9-common-mistakes)
10. [Dependencies](#10-dependencies)

---

## 1. The problem it solves

Imagine the player earns points. Several things must react:

- The score label on screen must update.
- An achievement must unlock at 100 points.
- A sound must play.

**Without the pattern**, the score code calls every one of those systems directly:

```csharp
public void AddScore(int amount)
{
    score += amount;
    scoreUI.Refresh(score);          // knows about the UI
    achievements.Check(score);       // knows about achievements
    audioManager.PlayCoinSound();    // knows about audio
}
```

Now the score code depends on every system. Adding a new reaction means editing it again, and removing the UI breaks it.

**With the pattern**, the score code only says "the score changed", and whoever cares reacts on their own:

```csharp
public void AddScore(int amount)
{
    score += amount;
    scoreSubject.Notify(score);      // that's all it knows
}
```

Result: the subject stays small, observers can be added or removed freely, and the systems stay **decoupled**.

---

## 2. How the pattern works

```
                       ┌──────────────────┐
                       │     Subject      │
                       │  (holds state)   │
                       │                  │
                       │  Subscribe()     │◄─── observers register
                       │  Unsubscribe()   │◄─── observers leave
                       │  Notify(data)    │───┐
                       └──────────────────┘   │ calls OnNotify(data)
                                              │ on every observer
                    ┌─────────────────────────┼─────────────────────┐
                    ▼                         ▼                     ▼
              ┌───────────┐            ┌─────────────┐        ┌──────────┐
              │ Observer A│            │  Observer B │        │Observer C│
              │ (UI)      │            │(Achievements)│       │ (Audio)  │
              └───────────┘            └─────────────┘        └──────────┘
```

Three roles:

| Role | Responsibility |
|---|---|
| **Subject** | Owns the state, keeps a list of observers, and notifies them when the state changes |
| **Observer** | Subscribes to a subject and reacts when notified |
| **Notification** | The data sent along (the new score, the new health value, or nothing at all) |

The key rule: **the dependency points one way.** Observers know the subject. The subject does not know the observers.

---

## 3. Which version should I use?

This folder contains three implementations that solve the same problem. Pick the lightest one that fits.

| Version | Best when | Trade-off |
|---|---|---|
| **1. Interface based** | You want a strict contract, or control over the observer list (count, order, clearing) | More boilerplate |
| **2. C# `event`** | You want the simplest Unity-friendly option. **This is the default choice.** | Observers are plain method handlers, so there is no shared interface |
| **3. ScriptableObject channel** | Designers wire things in the Inspector, or the sender and the receiver live in different scenes or prefabs | Extra assets to manage, and the sender needs the asset reference |

Quick decision guide:

- Two scripts that already know each other, such as `PlayerHealth` and `HealthBarUI`? Use **C# `event`**.
- A manager with many independent listeners, such as score and achievements? Use **interface based**.
- A global "something happened" signal like `OnLevelCompleted`? Use the **ScriptableObject channel**.

---

## 4. File overview

| File | Version | Role |
|---|---|---|
| `IObserver.cs` | Interface | `IEventObserver<T>` and `IEventSubject<T>` contracts |
| `Subject.cs` | Interface | Reusable observer list with `Subscribe`, `Unsubscribe`, `Notify` |
| `ScoreManager.cs` | Interface | Example **subject**: holds the score |
| `ScoreUI.cs` | Interface | Example **observer**: shows the score |
| `AchievementTracker.cs` | Interface | Example **observer**: unlocks achievements at score thresholds |
| `PlayerHealth.cs` | C# event | Example **subject**: exposes `OnHealthChanged` and `OnDied` |
| `HealthBarUI.cs` | C# event | Example **observer**: updates a slider |
| `GameEvent.cs` | ScriptableObject | The **subject**, stored as an asset |
| `GameEventListener.cs` | ScriptableObject | The **observer**, a component with an Inspector response |

---

## 5. Version 1: Interface based

### Structure

```
ScoreManager (subject) ──Notify(score)──► ScoreUI             (observer)
                                       └► AchievementTracker  (observer)
```

### The contracts (`IObserver.cs`)

```csharp
public interface IEventObserver<in T>
{
    void OnNotify(T data);
}

public interface IEventSubject<T>
{
    void Subscribe(IEventObserver<T> observer);
    void Unsubscribe(IEventObserver<T> observer);
}
```

The names are `IEventObserver` / `IEventSubject` on purpose, so they never clash with `System.IObserver<T>` when a file has `using System;`.

### The subject (`ScoreManager.cs`)

`ScoreManager` uses `Subject<int>` by **composition**. That works inside a MonoBehaviour, which cannot inherit from a second base class.

```csharp
private readonly Subject<int> _scoreSubject = new Subject<int>();
public IEventSubject<int> ScoreChanged => _scoreSubject;

public void AddScore(int amount)
{
    Score += amount;
    _scoreSubject.Notify(Score);
}
```

The public property returns `IEventSubject<int>`, so outside code can **subscribe but cannot call `Notify`**. Only the owner raises notifications.

### Writing an observer

```csharp
public class CoinSound : MonoBehaviour, IEventObserver<int>
{
    private void OnEnable()  => ScoreManager.Instance.ScoreChanged.Subscribe(this);
    private void OnDisable() => ScoreManager.Instance.ScoreChanged.Unsubscribe(this);

    public void OnNotify(int score)
    {
        // play the sound
    }
}
```

### Raising a notification

```csharp
ScoreManager.Instance.AddScore(50);
```

### Scene setup

1. Add `ScoreManager` to an empty GameObject.
2. Add `ScoreUI` to a UI object and assign a TextMeshPro label to **Score Text**.
3. Add `AchievementTracker` to any GameObject and set the **Thresholds** (default 100, 500, 1000).
4. Call `ScoreManager.Instance.AddScore(...)` from gameplay code. The label updates and achievements unlock without any further wiring.

`AchievementTracker` also exposes `OnAchievementUnlocked`, which you can subscribe to for popups.

---

## 6. Version 2: C# `event`

The `event` keyword **is** an observer list. `+=` means subscribe and `-=` means unsubscribe. No interfaces or helper classes are needed.

### The subject (`PlayerHealth.cs`)

```csharp
public event Action<int, int> OnHealthChanged;   // current, max
public event Action OnDied;

public void TakeDamage(int amount)
{
    CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
    OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

    if (IsDead)
        OnDied?.Invoke();
}
```

`?.Invoke` makes the call safe when nobody is subscribed.

### The observer (`HealthBarUI.cs`)

```csharp
private void OnEnable()
{
    playerHealth.OnHealthChanged += HandleHealthChanged;
    playerHealth.OnDied += HandleDied;
}

private void OnDisable()
{
    playerHealth.OnHealthChanged -= HandleHealthChanged;
    playerHealth.OnDied -= HandleDied;
}

private void HandleHealthChanged(int current, int max)
{
    slider.value = (float)current / max;
}
```

### Scene setup

1. Add `PlayerHealth` to the player and set **Max Health**.
2. Add `HealthBarUI` to a UI object.
3. Assign **Player Health**, the **Slider**, and optionally a **Death Panel**.
4. Call `playerHealth.TakeDamage(25)` or `Heal(10)` from anywhere.

### Why `event` and not a plain `Action` field?

Outside classes can only use `+=` and `-=` on an `event`. They cannot invoke it or overwrite it with `=`. That keeps the subject in charge of raising notifications.

---

## 7. Version 3: ScriptableObject event channel

The subject is an **asset** in the Project window. Sender and listener only share a reference to that asset, so they never reference each other. This works across scenes and prefabs.

### Setup

1. In the Project window: **Create → Design Patterns → Game Event**. Name it, for example, `OnLevelCompleted`.
2. **Raise it from code**:

   ```csharp
   [SerializeField] private GameEvent onLevelCompleted;

   void Finish()
   {
       onLevelCompleted.Raise();
   }
   ```

3. **Listen with no code**: add `GameEventListener` to any GameObject, drag the same asset into **Game Event**, and add actions to **Response** (for example `AudioSource.Play`, or `GameObject.SetActive`).

Multiple listeners can use the same asset, and each one has its own response.

### Notes

- This version sends **no data**, only a signal. If you need a value, copy the pattern with a generic version such as `GameEvent<int>`, or use Version 1 or 2.
- The listener registers in `OnEnable` and unregisters in `OnDisable`, so disabled objects are never called.

---

## 8. Rules to avoid bugs

- **Every subscribe needs an unsubscribe.** Always use `OnEnable` / `OnDisable` as a pair. A missing `-=` leaks memory and causes `MissingReferenceException` when the destroyed observer is called.
- **Safe removal during notification.** `Subject<T>` and `GameEvent` loop backwards, so an observer can unsubscribe itself inside its own callback.
- **No duplicate subscriptions.** `Subject<T>` ignores an observer that is already in the list. With C# `event`, subscribing twice means being called twice.
- **Do not depend on order.** Never write observers that rely on running before or after another observer.
- **Keep notifications small.** Send the changed value, not the whole subject.
- **Observers must be cheap.** Everything runs synchronously inside `Notify`, so a slow observer delays the subject and every observer after it.

---

## 9. Common mistakes

| Mistake | Symptom | Fix |
|---|---|---|
| Subscribing in `Start`, unsubscribing never | Callbacks keep firing after the object is destroyed | Use `OnEnable` / `OnDisable` |
| Subscribing in `Awake` to a singleton that is not ready | `NullReferenceException` | Subscribe in `OnEnable` or `Start` and null-check `Instance` |
| Not unsubscribing in `OnDisable` while using `Instance` on quit | Null reference during shutdown | Null-check `Instance` before unsubscribing (see `ScoreUI.cs`) |
| Observer changes the subject inside `OnNotify` | Notification loops | Avoid writing back to the subject, or guard with a flag |
| Using static events without cleaning up | Handlers survive scene reloads | Unsubscribe in `OnDisable`, or avoid static events |

---

## 10. Dependencies

- `ScoreManager` needs `Singleton<T>` from `Utilities/Singleton.cs` in this repository.
- `ScoreUI` needs **TextMeshPro**.
- `HealthBarUI` needs **UnityEngine.UI**.
- No third-party SDKs.