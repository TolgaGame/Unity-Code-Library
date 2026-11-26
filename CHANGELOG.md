# Changelog

## v1.3.0

### Added
- **BlastGameExample/** - a Toon Blast style blast mechanic in 7 scripts. Add `BlastGameController` to an empty GameObject and press Play. Standalone, no art files needed.
  - Tap groups of 2+ same colored cubes, gravity and refill, score and moves, win / lose, tap to restart.
  - Groups of 5+ leave a bomb that clears 3x3. Automatic shuffle when no move is left.
  - `BlastBoard` holds the rules in plain C# (no Unity code), `BlastBoardView` / `BlastTile` only show them.
  - Works with the old and the new Input System. Has its own `README.md`.

## v1.2.0

### Added
- **Managers/SaveSystem** - JSON save / load / delete in `persistentDataPath` with safe (temp file) writing and optional obfuscation. Standalone.
- **Managers/AudioManager** - music with crossfade, SFX pool with random pitch, saved volume and mute settings. Standalone.
- **Utilities/ScreenSafeArea** - fits a UI panel into `Screen.safeArea` (notch / cutout support), updates on rotation. Standalone.
- **Utilities/FPSCounter** - on-screen FPS, frame time and memory overlay. Only active in the Editor and development builds. Standalone.

## v1.1.0

### Added
- **Design_Patterns/Command** - command pattern with undo / redo.
- **Design_Patterns/ObjectPool** - generic GameObject pool with a bullet example.
- **Input/** - `SwipeDetector`, `TapDetector`, `VirtualJoystick`, `InputReader` (old and new Input System), `InputDemoMover`.
- **ADS/MetaAdsManager** - Meta Audience Network banner, interstitial and rewarded ads.
- `GDPR_ATT/README.md` and `Input/README.md`.
- `AdManager.ShowRewardedAd(Action)` for rewarded ads and `AdManager.DisableAds()`.
- `Utilities/PersistentSingleton.cs` (was used by Firebase and Tutorial scripts but missing from the repo).
- `RateUs.OnRatePromptShown` and `InternetCheck.OnConnectionChanged` events.

### Changed
- **Standalone scripts:** `AdManager` and `GDPR` no longer reference each other, and `AdManager` / `ApplovinAdsManager` no longer need `Singleton.cs`. Copy any single file without compile errors (SDK packages aside). `AdManager` waits for consent by itself.
- `GDPR` has `Auto Start` and `StartConsentFlow()`, and `AttPermissionRequest` has an `On Finished Event`, so ATT → consent is wired in the Inspector.
- `IAPManager` no longer depends on `AdManager`: use the `On Ads Removed` event (wire it to `AdManager.DisableAds`).
- `FakeLeaderboardManager` no longer depends on `Locator`. Set `playerScore` or call `SetPlayerScore`.
- **GDPR / ATT** now follows the current Google UMP flow (`Update` → `LoadAndShowConsentFormIfRequired` → `CanRequestAds`), with a privacy options button, debug geography and test devices.
- `AttPermissionRequest` runs before consent and reports when it finishes. `PostBuildStep` is active again.
- Ads are initialized only after consent. `AdManager` uses platform-specific test IDs, adaptive banners and main-thread callbacks.
- Ad managers no longer depend on `GameManager`. Use the `OnInterstitialFinished` event instead.
- `Singleton` uses `FindFirstObjectByType` on Unity 2023.1+.
- `InternetCheck` has a request timeout, disposes requests and works while the game is paused.
- `RateUs` no longer depends on `Locator`.

### Fixed
- `ApplovinAdsManager` inherited from a class that does not exist (`AdsManagerAPPLOVIN`) and forced user consent to `true`.
- `UnityAdsManager` used the interstitial ID for the banner.
- `GDPRS` class name did not match the `GDPR.cs` file name.
- AdMob event listeners were never registered.
- README folder names (`GDPR_ATT`, `Tutorial_System`) and the missing `Design_Patterns` folder.
