# Tutorial System — Setup Guide

---

## 1. Creating a TutorialStep

Right-click → **Create → FPS → Tutorial Step**

| Field | Description |
|---|---|
| Title / Description | Title and description shown at the top |
| Narrator Text | Dialogue written out with a typewriter effect |
| Speaker Name / Image | Speaker name and portrait |
| Target Object | Scene object used by ReachLocation / LookAtObject |
| Target UI | Spotlight prefab — drag it from the **Project panel** |
| Completion Type | See the table below |
| Reach Distance | ReachLocation distance (meters) |
| Auto Skip | Advances automatically 2 seconds after the text finishes |
| Skippable | Shows the Skip button |

---

## 2. Scene Setup

**TutorialManager** Inspector fields:
- **Steps** → arrange the TutorialStep assets in order
- **UI** → the TutorialUI component
- **Player Transform** → the Player object
- **Main Canvas / Raycaster** → references to the tutorial canvas

**Canvas hierarchy:**
```
TutorialCanvas  (Screen Space Overlay, Sort Order: 99)
├── Overlay          → CanvasGroup component  ← overlayGroup
├── NarratorPanel    → bottom-center panel
│   ├── SpeakerImage → Image  ← speaker portrait, slides in from the left
│   ├── SpeakerName  → TextMeshProUGUI
│   └── NarratorText → TextMeshProUGUI
├── TitleText        → TextMeshProUGUI
├── DescriptionText  → TextMeshProUGUI
└── SkipButton       → Button
```

> The 4 dimming panels used for the spotlight are created **automatically** at runtime. Don't add anything for them in the Inspector.

---

## 3. Using the Spotlight

To highlight a specific UI area without dimming it:

1. Create an empty GameObject and size its RectTransform to the target area
2. **Save it as a prefab** (Project panel)
3. Drag that prefab into the **Target UI** field of the TutorialStep

The system instantiates the prefab on the canvas, dims everything outside its rect, and destroys it when the step ends.
`targetUI = null` → normal overlay, no spotlight.

> **Note:** If `overlayGroup` is the parent of the narrator panel, leave `Spotlight Overlay Alpha` at 1 instead of 0.

---

## 4. Completion Type Guide

| Type | How It Is Triggered |
|---|---|
| `ReachLocation` | Update loop — checks whether the player is closer to `targetObject` than `reachDistance` |
| `LookAtObject` | Update loop — camera-to-target dot product > 0.97 |
| `FireWeapon` | `TutorialTrigger.Notify(CompletionType.FireWeapon)` |
| `EquipWeapon` | `TutorialTrigger.Notify(CompletionType.EquipWeapon)` |
| `EnterTrigger` | A `TutorialTrigger` component + Collider (IsTrigger) on `targetObject` |
| `KillEnemy` | `TutorialTrigger.Notify(CompletionType.KillEnemy)` |
| `UIElementInteract` | `TutorialTrigger.Notify(CompletionType.UIElementInteract)` |
| `Manual` | `TutorialManager.Instance.NotifyEvent(CompletionType.Manual)` |

---

## 5. Progression Logic

- **autoSkip = true** → the text finishes, waits 2 seconds, then moves to the next step automatically
- **autoSkip = false** → advances only when the Skip button is pressed (`skippable` must be `true`)

---

## 6. Gameplay Integration

```csharp
// Start the tutorial
TutorialManager.Instance.StartTutorial();
TutorialManager.OnTutorialComplete += () => { /* resume the game */ };

// Notify gameplay events (static, no reference needed)
TutorialTrigger.Notify(CompletionType.FireWeapon);
TutorialTrigger.Notify(CompletionType.KillEnemy);
TutorialTrigger.Notify(CompletionType.UIElementInteract); // e.g. input field submit
```
