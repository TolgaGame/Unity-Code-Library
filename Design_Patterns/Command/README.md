# Command Pattern in Unity

> **Turn an action ("move left", "paint red") into an object. Objects can be stored, so you get undo, redo, replays and input queues almost for free.**

---

## Table of Contents

1. [The problem it solves](#1-the-problem-it-solves)
2. [How the pattern works](#2-how-the-pattern-works)
3. [File overview](#3-file-overview)
4. [Scene setup](#4-scene-setup)
5. [Writing your own command](#5-writing-your-own-command)
6. [Rules to avoid bugs](#6-rules-to-avoid-bugs)
7. [Common mistakes](#7-common-mistakes)
8. [Where else to use it](#8-where-else-to-use-it)
9. [Dependencies](#9-dependencies)

---

## 1. The problem it solves

You want an **Undo** button in a puzzle game or a level editor. If the object is changed directly, the old value is gone:

```csharp
public void MoveRight()
{
    target.position += Vector3.right;   // the previous position is lost
}
```

With the pattern, every action is wrapped in a small object that knows how to do **and** undo itself, and a history list remembers them.

---

## 2. How the pattern works

```
 Button / input ──► creates ──► MoveCommand ──► CommandInvoker.Execute(cmd)
                                                      │
                                   runs cmd.Execute() │ stores cmd in history
                                                      ▼
                          [ Move ][ Paint ][ Move ][ Move ]
                                                    ▲ cursor
                         Undo → cmd.Undo(), cursor moves left
                         Redo → cmd.Execute(), cursor moves right
```

| Role | Class | Responsibility |
|---|---|---|
| **Command** | `ICommand` | `Execute()` and `Undo()` |
| **Concrete command** | `MoveCommand`, `ChangeColorCommand` | One action plus the data needed to reverse it |
| **Invoker** | `CommandInvoker` | Runs commands, keeps the history, handles undo/redo |
| **Receiver** | `Transform`, `Renderer` | The object that is actually changed |
| **Client** | `CommandDemo` | Creates commands and gives them to the invoker |

---

## 3. File overview

| File | Role |
|---|---|
| `ICommand.cs` | The `Execute` / `Undo` contract |
| `CommandInvoker.cs` | History list with a cursor, `Undo`, `Redo`, `Clear`, `OnHistoryChanged` |
| `MoveCommand.cs` | Moves a `Transform` by an offset |
| `ChangeColorCommand.cs` | Changes a `Renderer` color |
| `CommandDemo.cs` | Example client: public methods to hook to UI Buttons |

---

## 4. Scene setup

1. Create a Cube. Add `CommandDemo` to any GameObject and assign **Target** (the cube's Transform) and **Target Renderer**.
2. Create a UI Canvas with Buttons: Up, Down, Left, Right, Paint, Undo, Redo.
3. In each Button's **OnClick**, drag the `CommandDemo` object and pick the matching method (`MoveUp`, `PaintRandomColor`, `Undo`, ...).
4. Optionally assign the **Undo Button** and **Redo Button** fields. They are disabled automatically when there is nothing to undo or redo.
5. Press Play, move and paint a few times, then press Undo repeatedly.

The demo uses UI Buttons on purpose: it works with the old and the new Input System. To drive it from a keyboard or gamepad, call the same public methods from your input code.

---

## 5. Writing your own command

Example: a command that adds coins to a wallet and can take them back (`Wallet` is your own class).

```csharp
public class AddCoinsCommand : ICommand
{
    private readonly Wallet _wallet;
    private readonly int _amount;

    public AddCoinsCommand(Wallet wallet, int amount)
    {
        _wallet = wallet;
        _amount = amount;
    }

    public void Execute() => _wallet.AddCoins(_amount);
    public void Undo()    => _wallet.AddCoins(-_amount);
}
```

Use it: `invoker.Execute(new AddCoinsCommand(wallet, 10));`

---

## 6. Rules to avoid bugs

- **Capture "before" data in `Execute`, not in the constructor.** The state may change between creating and running the command. Capture it once (see the `_captured` flag) so redo lands on the same result.
- **`Undo` must exactly reverse `Execute`.** Test it: execute, undo, and the object must equal its original state.
- **Never change the object directly and through commands.** Mixed changes break the history. Everything that should be undoable goes through the invoker.
- **A new command clears the redo list.** This is standard behavior and is already handled in `CommandInvoker.Execute`.
- **Limit the history size.** `CommandInvoker(maxHistory)` drops the oldest command when full, so memory stays bounded.
- **Check for destroyed objects.** A stored command may point to an object that no longer exists; `MoveCommand` returns early when `_target == null`.

---

## 7. Common mistakes

| Mistake | Symptom | Fix |
|---|---|---|
| Storing the old value in the constructor | Undo jumps to the wrong place after other changes | Capture it the first time `Execute` runs |
| `Undo` does not restore everything `Execute` changed | Object drifts after several undo/redo cycles | Store and restore every changed value |
| Executing commands without the invoker | Action works but cannot be undone | Always call `invoker.Execute(cmd)` |
| Unlimited history | Memory grows during a long session | Set `maxHistory` |
| Command keeps a reference to a destroyed object | `MissingReferenceException` | Check `== null` before use |
| Using `material.color` on many objects | Material instances are created per renderer | Use a `MaterialPropertyBlock` for large scenes |

---

## 8. Where else to use it

- Level editors and drawing tools (undo / redo)
- Turn-based and puzzle games (undo the last move)
- Replay systems (record commands with timestamps, play them back)
- Input buffering and queued actions (store commands, run them one by one)
- Macro or cheat console commands

---

## 9. Dependencies

- Only `UnityEngine` and `UnityEngine.UI` (for the optional demo buttons).
- No singleton or third-party code. No namespaces; add one if your project needs it.
