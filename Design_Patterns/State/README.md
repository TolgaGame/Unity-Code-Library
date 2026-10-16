# State Pattern in Unity

> **One object (the *context*) changes its behavior depending on which *state* it is in. Each state is its own piece of logic, and exactly one is active at a time.**

This guide explains the pattern, shows how each implementation in this folder works, and gives step-by-step usage instructions.

---

## Table of Contents

1. [The problem it solves](#1-the-problem-it-solves)
2. [How the pattern works](#2-how-the-pattern-works)
3. [Which version should I use?](#3-which-version-should-i-use)
4. [File overview](#4-file-overview)
5. [Version 1: Interface based](#5-version-1-interface-based)
6. [Version 2: Enum + switch](#6-version-2-enum--switch)
7. [Version 3: ScriptableObject states](#7-version-3-scriptableobject-states)
8. [Rules to avoid bugs](#8-rules-to-avoid-bugs)
9. [Common mistakes](#9-common-mistakes)
10. [Dependencies](#10-dependencies)

---

## 1. The problem it solves

Imagine an enemy that idles, patrols, chases the player and attacks. The first attempt is usually a pile of boolean flags:

```csharp
void Update()
{
    if (isDead) return;

    if (isAttacking)
    {
        // attack logic...
    }
    else if (isChasing && !isStunned)
    {
        // chase logic...
    }
    else if (isPatrolling)
    {
        // patrol logic...
    }
    else
    {
        // idle logic...
    }
}
```

This gets worse with every feature:

- Flags can contradict each other (`isAttacking` and `isPatrolling` both `true`).
- Every new behavior means editing the same big method.
- Nobody can tell at a glance which transitions are allowed.
- Setup and cleanup code (reset a timer, stop a sound) is scattered everywhere.

**With the pattern**, each behavior lives in its own state, and the enemy only says "run the current state":

```csharp
void Update()
{
    Machine.Tick();   // that's all the enemy knows
}
```

Result: one behavior per class, transitions are explicit, and adding a "Stunned" state does not touch the existing ones.

---

## 2. How the pattern works

```
                 ┌───────────────────────────┐
                 │   Context  (EnemyAI)      │
                 │   owns a StateMachine     │
                 │   Update() → Machine.Tick │
                 └─────────────┬─────────────┘
                               │ delegates to
                               ▼
                      ┌─────────────────┐
                      │  CurrentState   │   exactly one at a time
                      │  Enter()        │
                      │  Tick()         │
                      │  Exit()         │
                      └─────────────────┘
                               ▲
          ┌──────────────┬─────┴────────┬──────────────┐
     ┌────────┐     ┌─────────┐    ┌─────────┐    ┌──────────┐
     │  Idle  │     │ Patrol  │    │  Chase  │    │  Attack  │
     └────────┘     └─────────┘    └─────────┘    └──────────┘
```

Three roles:

| Role | Responsibility |
|---|---|
| **Context** | Owns the state machine and the shared data and helpers (target, speed, movement). Contains no behavior logic |
| **State** | The behavior for one situation: `Enter` (setup), `Tick` (every frame), `Exit` (cleanup) |
| **Transition** | The rule that moves from one state to another, usually an `if` inside a state's `Tick` |

The key rule: **states decide their own transitions.** The context never needs to know which state is active, and the machine never needs to know what a state does.

### The enemy used in the example

| From | To | When |
|---|---|---|
| Idle | Patrol | Idle time is over and waypoints exist |
| Idle | Chase | Target is within sight range |
| Patrol | Idle | A waypoint was reached |
| Patrol | Chase | Target is within sight range |
| Chase | Attack | Target is within attack range |
| Chase | Idle | Target is beyond lose-sight range |
| Attack | Chase | Target moved out of attack range |

### State vs Observer

They look similar (both are "objects talking through an interface") but solve different problems:

| | Observer | State |
|---|---|---|
| Question it answers | "Who needs to know that something happened?" | "What should I do right now?" |
| Relationship | One subject, many reactors | One context, one active behavior |
| Typical use | Score changed, UI refreshes | Enemy switches from patrol to chase |

They combine well: `StateMachine` exposes an `OnStateChanged` event (an Observer), so audio, UI or logging can react to transitions without the states knowing about them.

---

## 3. Which version should I use?

This folder contains three implementations that solve the same problem. Pick the lightest one that fits.

| Version | Best when | Trade-off |
|---|---|---|
| **1. Interface based** | States have real logic, their own timers or data, or there are 4+ of them (enemy AI, player controller, boss) | One class per state, more files |
| **2. Enum + switch** | A few simple states in one script (door, lamp, button, menu panel). **This is the default starting point.** | The `switch` grows and becomes hard to read as states get complex |
| **3. ScriptableObject states** | Designers tune or rearrange states in the Inspector, or one state is reused by many objects | States must be stateless, and there are assets to manage |

Quick decision guide:

- A door, a lamp or a toggle with 3 to 4 simple modes? Use **Enum + switch**.
- An enemy or player with distinct behaviors that each have their own logic? Use **interface based**.
- A traffic light, a timed sequence or any designer-driven cycle? Use **ScriptableObject states**.
- Started with an enum and the `switch` no longer fits on one screen, or one state needs its own fields? Move to **interface based**.

---

## 4. File overview

| File | Version | Role |
|---|---|---|
| `IState.cs` | Interface | `Enter` / `Tick` / `Exit` contract |
| `StateMachine.cs` | Interface | Holds the current state, `ChangeState`, `Tick`, `OnStateChanged` |
| `EnemyAI.cs` | Interface | Example **context**: shared data, helpers, owns the machine |
| `EnemyIdleState.cs` | Interface | Example **state**: waits |
| `EnemyPatrolState.cs` | Interface | Example **state**: walks between waypoints |
| `EnemyChaseState.cs` | Interface | Example **state**: runs at the target |
| `EnemyAttackState.cs` | Interface | Example **state**: attacks on a cooldown |
| `DoorController.cs` | Enum | Complete door: `Closed`, `Opening`, `Open`, `Closing` |
| `ScriptableState.cs` | ScriptableObject | Abstract base for state assets |
| `ScriptableStateRunner.cs` | ScriptableObject | The **context** component that runs a state asset |
| `TimedColorState.cs` | ScriptableObject | Example state asset: show a color, then go to the next state |

---

## 5. Version 1: Interface based

### Structure

```
EnemyAI (context) ── owns ──► StateMachine ── runs ──► IState
                                                         ├─ EnemyIdleState
                                                         ├─ EnemyPatrolState
                                                         ├─ EnemyChaseState
                                                         └─ EnemyAttackState
```

### The contract (`IState.cs`)

```csharp
public interface IState
{
    void Enter();   // once, when the state starts
    void Tick();    // every frame while active
    void Exit();    // once, when the state ends
}
```

### The machine (`StateMachine.cs`)

A plain C# class, used by composition (a MonoBehaviour cannot inherit from a second base class).

```csharp
public IState CurrentState { get; private set; }
public event Action<IState, IState> OnStateChanged;   // (from, to)

public void ChangeState(IState next);   // Exit old → Enter new → raise event
public void Tick();                     // runs CurrentState.Tick()
```

If `ChangeState` is called from inside an `Enter` or `Exit`, the request is queued and applied right after, so `Exit` / `Enter` pairs never interleave.

### The context (`EnemyAI.cs`)

It creates each state **once** in `Awake`, starts the machine in `Start`, and ticks it in `Update`:

```csharp
private void Awake()
{
    Machine = new StateMachine();
    IdleState = new EnemyIdleState(this);
    PatrolState = new EnemyPatrolState(this);
    ChaseState = new EnemyChaseState(this);
    AttackState = new EnemyAttackState(this);
}

private void Start()  => Machine.Initialize(IdleState);
private void Update() => Machine.Tick();
```

It also exposes the data and conditions the states need (`Target`, `CanSeeTarget`, `InAttackRange`, `MoveTowards(...)`), so a state never reaches for anything global.

### A state (`EnemyChaseState.cs`)

```csharp
public void Tick()
{
    if (_enemy.HasLostTarget)
    {
        _enemy.Machine.ChangeState(_enemy.IdleState);
        return;                                   // always return after a transition
    }

    if (_enemy.InAttackRange)
    {
        _enemy.Machine.ChangeState(_enemy.AttackState);
        return;
    }

    _enemy.MoveTowards(_enemy.Target.position, _enemy.ChaseSpeed);
}
```

### Adding a new state in 3 steps

Example: a `Stunned` state.

**1. Write the state:**

```csharp
public class EnemyStunnedState : IState
{
    private readonly EnemyAI _enemy;
    private float _timer;

    public EnemyStunnedState(EnemyAI enemy) { _enemy = enemy; }

    public void Enter() => _timer = 1.5f;

    public void Tick()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            _enemy.Machine.ChangeState(_enemy.IdleState);
    }

    public void Exit() { }
}
```

**2. Register it in `EnemyAI`:**

```csharp
public EnemyStunnedState StunnedState { get; private set; }

// in Awake:
StunnedState = new EnemyStunnedState(this);

// a public entry point for other scripts:
public void Stun() => Machine.ChangeState(StunnedState);
```

**3. Trigger it** from anywhere: `enemy.Stun();`

No existing state had to change.

### Scene setup

1. Add `EnemyAI` to a GameObject (a Capsule works well).
2. Assign **Target** (for example the player).
3. Create a few empty GameObjects as waypoints and drag them into **Waypoints**.
4. Tune the ranges and speeds.
5. Press Play. The Console prints every transition (`EnemyIdleState -> EnemyPatrolState`). Select the enemy to see the sight, lose-sight and attack ranges as gizmos.

---

## 6. Version 2: Enum + switch

When there are only a few simple states, a full class per state is overkill. An `enum` names the states and a `switch` runs the right code.

### The states (`DoorController.cs`)

```csharp
public enum DoorState { Closed, Opening, Open, Closing }
```

### The behavior

```csharp
private void Update()
{
    switch (State)
    {
        case DoorState.Opening:
            if (RotateTowards(_openRotation)) SetState(DoorState.Open);
            break;

        case DoorState.Closing:
            if (RotateTowards(_closedRotation)) SetState(DoorState.Closed);
            break;

        case DoorState.Open:
            // count down, then SetState(DoorState.Closing)
            break;
    }
}
```

### One gate for every transition

All changes go through `SetState`, the only place that writes `State`. It is where "enter" logic and the `OnStateChanged` event live:

```csharp
private void SetState(DoorState next)
{
    if (State == next) return;

    State = next;
    EnterState(next);                 // per-state setup (reset timer, play sound)
    OnStateChanged?.Invoke(next);
}
```

### Scene setup

1. Add `DoorController` to the door object. Its pivot must be at the hinge, or nest the door under a hinge object.
2. Set **Open Angle**, **Rotate Speed** and **Auto Close Delay** (0 means it never closes by itself).
3. Call `door.Interact()` from your interaction code. For a quick test, press Play, then right-click the component header and choose **Interact**.
4. Interacting while the door is moving reverses it.

---

## 7. Version 3: ScriptableObject states

Each state is an **asset** in the Project window. A runner component just holds a reference to the current one. Designers can create, tune and chain states without touching code, and one asset can drive any number of objects.

### Example: a traffic light

`TimedColorState` shows a color for a set time, then switches to the `nextState` asset.

### Setup

1. In the Project window: **Create → Design Patterns → Timed Color State**. Create three assets named `Green`, `Yellow` and `Red`.
2. Set each one's **Color** and **Duration** (for example 4s, 1s, 3s).
3. Chain them with **Next State**: `Green → Yellow → Red → Green`. Circular references between assets are fine in Unity.
4. Create a Sphere (or any object with a `Renderer`) and add `ScriptableStateRunner`.
5. Drag `Green` into **Initial State**. Press Play.

### Writing your own state asset

```csharp
[CreateAssetMenu(menuName = "Design Patterns/My State")]
public class MyState : ScriptableState
{
    public override void OnEnter(ScriptableStateRunner runner) { /* setup */ }

    public override void OnTick(ScriptableStateRunner runner)
    {
        if (runner.TimeInState > 2f)
            runner.ChangeState(/* another asset */ null);
    }

    public override void OnExit(ScriptableStateRunner runner) { /* cleanup */ }
}
```

### The important rule: states must be stateless

An asset is **shared** by every runner that uses it. If `TimedColorState` kept a `float timer` field, ten traffic lights would fight over one timer. So:

- Per-object data (timers, counters, references) lives on the **runner**. That is why `TimeInState` is a property of `ScriptableStateRunner`.
- Never write to a state's serialized fields at runtime. In the Editor, changes made to ScriptableObjects during Play mode **stay** after you stop.

### Notes

- If you need richer shared data, add properties to the runner, or derive a more specific runner.
- `Renderer.material` creates a material instance for that object. That is fine for a few objects, but for many, use a `MaterialPropertyBlock`.

---

## 8. Rules to avoid bugs

- **Exactly one active state.** Do not keep boolean flags next to the machine (`isAttacking`). Two sources of truth will disagree.
- **Always `return` right after `ChangeState` in `Tick`.** Otherwise the rest of the old state's `Tick` still runs on a state that was already exited.
- **Setup goes in `Enter`, cleanup in `Exit`.** Reset timers in `Enter`, never only in the constructor. A state is entered many times.
- **Create states once.** Cache them in the context (`Awake`). Do not `new` a state on every transition; that allocates garbage.
- **Reach shared data through the context.** A state should not use singletons or `Find` calls to get what it needs.
- **Avoid flicker with hysteresis.** If a state is entered at a threshold, leave it at a different threshold. In the example, Chase starts at 8 m but ends at 10 m, and Attack starts at 1.5 m but ends at 2.0 m.
- **Use `OnStateChanged` for side effects, not decisions.** Logging, audio and UI are good uses. Transition logic belongs in the states.
- **Unsubscribe from `OnStateChanged`.** Use `OnEnable` / `OnDisable` as a pair, the same rule as the Observer pattern.
- **Keep transitions readable.** Write the transition table (see section 2) before coding. If it becomes spaghetti, you have too many states or need a hierarchy.
- **Avoid chaining `ChangeState` inside `Enter` / `Exit`.** The machine queues it safely, but long chains are hard to follow.

---

## 9. Common mistakes

| Mistake | Symptom | Fix |
|---|---|---|
| No `return` after `ChangeState` in `Tick` | Old state keeps acting for the rest of the frame, double transitions | `return;` right after the transition |
| Timer initialized in the constructor | Works the first time, then the timer is stale | Reset it in `Enter` |
| Same threshold to enter and leave a state | Rapid flicker between two states at the boundary | Use a larger exit threshold (hysteresis) |
| Creating `new` states every transition | GC spikes | Create once in `Awake`, reuse |
| Calling `Machine.Tick()` before `Initialize` | Nothing happens | Call `Initialize` in `Start` |
| Boolean flags kept beside the machine | The flag and the state disagree | Ask the machine: `Machine.CurrentState is EnemyAttackState` |
| Mutable fields in a ScriptableObject state | Every object using the asset shares the value | Keep per-object data on the runner |
| Writing to the enum from many places | Transitions skip `EnterState` and the event | Route every change through `SetState` |
| Forgetting a case after adding an enum value | New state silently does nothing | Add a `case` for every member |
| Not unsubscribing from `OnStateChanged` | Callbacks fire on destroyed objects | Pair `+=` / `-=` in `OnEnable` / `OnDisable` |

---

## 10. Dependencies

- Versions 1 and 2 need only `UnityEngine`. No singleton or third-party code is required.
- Version 3 needs a `Renderer` on the object. `Renderer.material.color` works with the Built-in and URP Lit shaders.
- The scripts use no namespaces; add one if your project needs it.
- No third-party SDKs.
