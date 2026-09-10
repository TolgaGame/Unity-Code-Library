# Tutorial System — Setup Guide

---

## 1. TutorialStep Oluşturma

Sağ tıkla → **Create → FPS → Tutorial Step**

| Alan | Açıklama |
|---|---|
| Title / Description | Üstte görünen başlık ve açıklama |
| Narrator Text | Typewriter ile yazılan diyalog |
| Speaker Name / Image | Konuşmacı adı ve fotoğrafı |
| Target Object | ReachLocation / LookAt için sahne objesi |
| Target UI | Spotlight prefabı — **Project panelinden** sürükle |
| Completion Type | Aşağıdaki tabloya bak |
| Reach Distance | ReachLocation mesafesi (metre) |
| Auto Skip | Yazı bitince 1 sn sonra otomatik geç |
| Skippable | autoSkip=false iken Skip butonu göster |

---

## 2. Sahne Kurulumu

**TutorialManager** Inspector alanları:
- **Steps** → TutorialStep asset'lerini sıraya diz
- **UI** → TutorialUI bileşeni
- **Player Transform** → Player objesi
- **Main Canvas / Raycaster** → Tutorial canvas referansları

**Canvas Hierarchy:**
```
TutorialCanvas  (Screen Space Overlay, Sort Order: 99)
├── Overlay          → CanvasGroup bileşeni  ← overlayGroup
├── NarratorPanel    → alt-orta panel
│   ├── SpeakerImage → Image  ← konuşmacı fotoğrafı, soldan kayar
│   ├── SpeakerName  → TextMeshProUGUI
│   └── NarratorText → TextMeshProUGUI
├── TitleText        → TextMeshProUGUI
├── DescriptionText  → TextMeshProUGUI
└── SkipButton       → Button
```

> Spotlight için 4 karartma paneli runtime'da **otomatik** oluşturulur, Inspector'da bir şey ekleme.

---

## 3. Spotlight Kullanımı

Belirli bir UI alanını karartmadan göstermek için:

1. Boş bir GameObject oluştur, RectTransform'unu hedef alana boyutlandır
2. **Prefab olarak kaydet** (Project paneli)
3. TutorialStep'teki **Target UI** alanına bu prefabı sürükle

Sistem prefabı canvas'a instantiate eder, o Rect dışını karartır, step bitince destroy eder.
`targetUI = null` → normal overlay, spotlight yok.

> **Not:** `overlayGroup` narrator panel'in parent'ıysa `Spotlight Overlay Alpha` değerini 0 yerine 1 bırak.

---

## 4. Completion Type Rehberi

| Tip | Nasıl Tetiklenir |
|---|---|
| `ReachLocation` | Update loop — `targetObject`'e `reachDistance`'dan yakın mı |
| `LookAtObject` | Update loop — kameradan hedefe dot > 0.97 |
| `FireWeapon` | `TutorialTrigger.Notify(CompletionType.FireWeapon)` |
| `EquipWeapon` | `TutorialTrigger.Notify(CompletionType.EquipWeapon)` |
| `EnterTrigger` | `targetObject`'e `TutorialTrigger` bileşeni + Collider (IsTrigger) |
| `KillEnemy` | `TutorialTrigger.Notify(CompletionType.KillEnemy)` |
| `UIElementInteract` | `TutorialTrigger.Notify(CompletionType.UIElementInteract)` |
| `Manual` | `TutorialManager.Instance.NotifyEvent(CompletionType.Manual)` |

---

## 5. İlerleme Mantığı

- **autoSkip = true** → yazı biter, 1 sn bekler, otomatik sonraki step
- **autoSkip = false** → sadece Skip butonuna basılınca ilerler (`skippable = true` olmalı)

---

## 6. Gameplay Entegrasyonu

```csharp
// Başlatmak
TutorialManager.Instance.StartTutorial();
TutorialManager.OnTutorialComplete += () => { /* oyun devam */ };

// Gameplay olaylarını bildirmek (static, referans gerekmez)
TutorialTrigger.Notify(CompletionType.FireWeapon);
TutorialTrigger.Notify(CompletionType.KillEnemy);
TutorialTrigger.Notify(CompletionType.UIElementInteract); // input field submit vb.
```
