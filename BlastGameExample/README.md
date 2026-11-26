# Blast Game Example (Toon Blast style)

> **The core mechanic of a "tap to blast" game in 7 small scripts. Add one component, press Play.**

Tap a group of 2 or more touching cubes of the same color. They pop, the cubes above fall down, new cubes drop in from the top. Reach the target score before you run out of moves.

---

## Table of Contents

1. [Quick start](#1-quick-start)
2. [Rules](#2-rules)
3. [File overview](#3-file-overview)
4. [How a turn works](#4-how-a-turn-works)
5. [Settings](#5-settings)
6. [Make it your own](#6-make-it-your-own)
7. [Design rules (why it is built this way)](#7-design-rules-why-it-is-built-this-way)
8. [Dependencies](#8-dependencies)

---

## 1. Quick start

1. Copy the `BlastGameExample` folder into your project's `Assets`.
2. Create an **empty scene** and an **empty GameObject**.
3. Add the **`BlastGameController`** component to it.
4. Press **Play**.

That is all. The controller adds `BlastBoardView` and `BlastHUD` by itself, creates and fits the camera, and the cubes are drawn from sprites made in code, so you need no prefabs, sprites, fonts or Canvas.

Works with the old and the new Input System, with mouse and touch.

---

## 2. Rules

| Action | Result |
|--------|--------|
| Tap a group of 2+ same colored cubes (up / down / left / right) | The group pops. Costs 1 move. |
| Tap a single cube | Nothing happens. The cube wiggles. Costs no move. |
| Pop a group of **5+** cubes | The cube you tapped stays and becomes a **bomb**. |
| Tap a **bomb** | Clears the 3x3 area around it. Costs 1 move. |
| No move left on the board | The cubes are shuffled automatically. |

- **Score:** a group of `n` cubes gives `n * n * 5` points. Bigger groups are worth much more.
- **Win:** score reaches the target. **Lose:** moves reach 0 before that.
- After the game ends, tap anywhere to play again.

---

## 3. File overview

| File | Job | Unity code? |
|------|-----|:-----------:|
| `BlastBoard.cs` | **The rules.** Grid of numbers, finding groups, gravity, refill, shuffle. Plain C#. | No |
| `BlastBoardView.cs` | Draws the board. Owns all cubes, recycles them in a small pool. | Yes |
| `BlastTile.cs` | One cube: look + animations (fall, pop, punch, shake). No rules inside. | Yes |
| `BlastGameController.cs` | **The game loop.** Input, turn order, score, moves, win / lose, camera. | Yes |
| `BlastHUD.cs` | Score, moves and the result screen (drawn with `OnGUI`). | Yes |
| `BlastInput.cs` | One tap / click for the old and new Input System. | Yes |
| `BlastSprites.cs` | Makes the cube and bomb sprites in code. | Yes |

```
BlastGameController  (rules + flow)
   ├── BlastBoard        what is on the board          (numbers only)
   ├── BlastBoardView    what you see on the screen
   │      └── BlastTile  one cube, with animations
   ├── BlastHUD          score text
   └── BlastInput        where the player tapped
```

The important idea: **`BlastBoard` decides, `BlastBoardView` shows.** The board changes first, then it tells the view what happened (`moves`, `spawns`, which cells were removed), and the view only plays the animation.

---

## 4. How a turn works

Everything happens inside one coroutine, `BlastGameController.PlayMove`:

```
tap  ->  screen position -> world position -> cell index          (BlastBoardView.WorldToCell)
     ->  FindGroup: which cells would be removed?                 (BlastBoard)
            not a group? -> shake the cube, stop (no move used)
     ->  moves--, score += n * n * 5
     ->  group of 5+? keep the tapped cube and turn it into a bomb
     ->  Clear + Pop                    board and screen: cubes disappear
     ->  Collapse + ApplyMoves          board and screen: cubes fall
     ->  Refill + ApplySpawns           board and screen: new cubes drop in
     ->  win? lose? no move left -> Shuffle
     ->  State = Playing                the player may tap again
```

While this runs, `State` is `Busy` and every tap is ignored, so two moves can never overlap.

---

## 5. Settings

All settings are in the Inspector of `BlastGameController`.

| Setting | Default | Meaning |
|---------|---------|---------|
| Width / Height | 8 / 8 | Board size in cells |
| Color Count | 5 | Different colors in play. Fewer colors = easier |
| Seed | 0 | `0` = random board. Any other number = same board every time |
| Start Moves | 20 | Moves the player has |
| Target Score | 1500 | Score needed to win |
| Bomb Threshold | 5 | Group size that makes a bomb. `0` = no bombs |
| Points Per Tile | 5 | Group of `n` gives `n * n * this` |
| Fit Camera | on | Fits the board to any screen. Turn off to use your own camera |

`BlastBoardView` has the colors (palette), cube size, gap and animation speeds.

---

## 6. Make it your own

**Use real sprites.** In `BlastBoardView.ApplyLook`, return your own sprites instead of `BlastSprites`. Use one sprite per color if you do not want to tint.

**Use real UI.** Delete `BlastHUD` (remove it from the `RequireComponent` list in the controller) and read these from `BlastGameController`: `Score`, `MovesLeft`, `TargetScore`, `State`, and the event `OnGameFinished(bool won)`.

**Add a new cube type** (for example a rocket):

1. Add a constant next to `Bomb` in `BlastBoard`.
2. Return its cells from `FindGroup` (a rocket clears a row, for example).
3. Give it a look in `BlastBoardView.ApplyLook`.

**Add levels.** Put `width`, `height`, `colorCount`, `startMoves`, `targetScore` into a `ScriptableObject` and pass it to the controller.

**Use only the rules.** `BlastBoard` has no Unity code. You can use it with your own view, on a server, or in a unit test.

---

## 7. Design rules (why it is built this way)

- **Rules and visuals are separate.** The board never touches a `GameObject`. This makes it easy to test and to change the look.
- **One input per turn.** `State` blocks taps while animations run.
- **Gravity is calculated bottom to top.** A cube always falls into a cell that is already free, so the view can move cubes one by one without conflicts.
- **No garbage while playing.** Cubes are recycled in a pool. Lists and the flood-fill buffer are reused.
- **The player is never stuck.** The first board always has a move, and after every turn the board is checked. If no move is left, it is shuffled.
- **A wrong tap is free.** Tapping a single cube costs no move.

---

## 8. Dependencies

None. It uses only Unity itself. The new Input System package is optional: `BlastInput` works with both input systems.

Every file in this folder is needed. The other folders of this library are **not** needed.
