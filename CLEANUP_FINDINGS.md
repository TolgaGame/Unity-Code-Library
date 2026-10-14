# Cleanup Findings

Problems noticed during the language cleanup pass (`chore/language-cleanup`) that were intentionally **not** fixed. Each item lists the file, the problem and a suggested fix. Nothing here was compiled in Unity, so compile-error items are based on reading the code.

## ADS

- `ADS/ApplovinAdsManager.cs`: Derives from `Singleton<AdsManagerAPPLOVIN>`, which does not exist (likely compile error). Fix: use `Singleton<ApplovinAdsManager>` or add the missing type.
- `ADS/ApplovinAdsManager.cs`: `MaxSdk.SetHasUserConsent(true)` hard-codes consent (GDPR risk). Fix: set it from the real UMP/CMP consent result.
- `ADS/ApplovinAdsManager.cs`: Hard-coded iOS ad unit IDs and an Android placeholder ID. Fix: move to serialized fields or a config asset.
- `ADS/ApplovinAdsManager.cs`: `Invoke("LoadInterstitial", ...)` uses a string method name. Fix: use `nameof(LoadInterstitial)`.
- `ADS/ApplovinAdsManager.cs`: `Awake` override only calls `base.Awake()`. Fix: remove it.
- `ADS/ApplovinAdsManager.cs`: Depends on `GameManager.Instance.SceneLoad()`, coupling ads to game code. Fix: raise an event/callback instead.
- `ADS/AdManager.cs`: Private `ListenToAdEvents()` is never called, so banner event logging never runs. Fix: call it after creating the banner or delete it.
- `ADS/AdManager.cs`: `ShowRewardedAd()` has a `TODO: Reward the user` and no else branch when the ad is not ready. Fix: add a reward callback/event and a not-ready log.
- `ADS/AdManager.cs`: Public fields `ADSEnabled`, `TestAdsEnabled`, `bannerID`, `interstitialID`, `rewardVideoID` use inconsistent casing. Fix: rename to PascalCase in a coordinated change (needs `FormerlySerializedAs`).
- `ADS/AdManager.cs`: `ShowAdInterstitial()` does nothing when `ADSEnabled` is false and never runs the scene-load continuation; callers may hang. Fix: define and implement the fallback behavior.
- `ADS/AdManager.cs`: Google sample test ad unit IDs are hard-coded in `Awake`. Fix: move to constants/config.
- `ADS/UnityAdsManager.cs`: `Start()` assigns the banner ad unit from the interstitial IDs, so the banner ID fields are never used. Fix: use `_iOSBannerAdUnitId` / `_androidBannerAdUnitId`.
- `ADS/UnityAdsManager.cs`: Callback parameters `_adUnitId` shadow the `_adUnitId` field (interface signatures). Fix: rename the parameters to `adUnitId`.
- `ADS/UnityAdsManager.cs`: The `#elif UNITY_EDITOR` branch in `InitializeAds()` is unreachable on mobile targets and leaves `_gameId` unset on other targets. Fix: reorder or add an `#else`.
- `ADS/UnityAdsManager.cs`: `HideBannerAd()` is private and unused; failures are logged with `Debug.Log`. Fix: expose or remove it; use the proper log level.
- `ADS/UnityAdsManager.cs`: `OnUnityAdsShowComplete` always calls `GameManager.Instance.SceneLoad()`. Fix: raise an event instead.

## GDPR - ATT

- `GDPR - ATT/GDPR.cs`: Class is named `GDPRS` but the file is `GDPR.cs` (MonoBehaviour class/file mismatch). Fix: rename the class to `GDPR` (update scene references) or rename the file with its `.meta`.
- `GDPR - ATT/GDPR.cs`: Hard-coded test device hashed ID and `DebugGeography.EEA` are applied in release builds too. Fix: wrap in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `GDPR - ATT/GDPR.cs`: `SceneManager.LoadScene(1)` runs right after starting the form load (can interrupt the form) and loads by build index. Fix: load after the form is dismissed, by name/constant.
- `GDPR - ATT/GDPR.cs`: `OnShowForm` calls `LoadConsentForm()` after dismissal, which can loop while the status is still `Required`. Fix: only reload when needed.
- `GDPR - ATT/GDPR.cs`: Uses fully-qualified `UnityEngine.Debug`. Fix: use plain `Debug` if there is no ambiguity.
- `GDPR - ATT/PostBuildStep.cs`: Whole file is commented out and has no `#if UNITY_EDITOR` / Editor folder guard when enabled. Fix: ship it in an `Editor` folder or wrap it in `#if UNITY_EDITOR`.
- `GDPR - ATT/PostBuildStep.cs`: `NSUserTrackingUsageDescription` text is hard-coded and not localized. Fix: make it configurable.

## Utilities

- `Utilities/RateUs.cs`: Depends on `Locator.Instance.SaveSystemInstance.SetRateUsPanelShown(true)` (external project type). Fix: inject via event/interface.
- `Utilities/RateUs.cs`: `reviewRequested` is only used on iOS, and `Rate()` on Android dereferences `rateUsPanel` without a null check. Fix: wrap the field in `#if UNITY_IOS` and null-check the panel.
- `Utilities/RateUs.cs`: Public field naming (`rateUsPanel`, `androidPackageName`) is inconsistent with other classes. Fix: align naming project-wide.
- `Utilities/InternetCheck.cs`: Mixed public field casing (`CheckPanel`, `testUrl`, `checkInterval`); manual `new UnityWebRequest(testUrl)` with no timeout; `CheckPanel` not null-checked. Fix: use `UnityWebRequest.Head`, set a timeout, null-check the panel.
- `Utilities/InternetCheck.cs`: The loop runs forever and logs every interval. Fix: log only on state change.
- `Utilities/PanelShower.cs`: `PanelShowerEditor` lives in the runtime file (guarded by `#if UNITY_EDITOR`). Fix: move it to an `Editor` folder.
- `Utilities/PanelShower.cs`: `Undo.RecordObject(panelScript.gameObject, ...)` records the GameObject but the changed components are `Canvas`/`GraphicRaycaster`. Fix: record the actual components.
- `Utilities/Singleton.cs`: Uses obsolete `FindObjectOfType<T>()`; `Awake` is `public virtual`; `Instance` is not reset on destroy. Fix: use `FindFirstObjectByType<T>()` and `protected virtual Awake` in a coordinated change.
- `Utilities/TransformExtension.cs`: Parameters `Target`, `PosX`, `PosY`, `PosZ` in `SetOnlyLocalPosX/Y/Z` are PascalCase (public API). Fix: rename to `target`, `x`, `y`, `z` if no caller uses named arguments.
- `Utilities/TransformExtension.cs`: `//Shady` author tag left as attribution. Fix: replace with a proper header or remove.
- `Utilities/TransformExtension.cs`: Extension `SetActive(this Transform, bool)` can be confused with `GameObject.SetActive`. Fix: consider renaming to `SetGameObjectActive`.

## Editor

- `Editor/FileTextureCompression.cs`: Class `FixTextureCompressionDXT5_GameSourceSprites` does not match the file name. Fix: rename the file or class (with its `.meta`).
- `Editor/FileTextureCompression.cs`: Hard-coded folder `Assets/_GameSource/Sprites/GameplaySprites/CharacterSpritess` (likely typo "Spritess"). Fix: verify the real folder name and expose it as a constant/setting.
- `Editor/FileTextureCompression.cs`: Method `FixSpritesInGameSourceFolder` is `static` with no access modifier in a non-static class. Fix: make the class `static` and the method `private`.
- `Editor/MissingRefFinder/EditorTools.cs`: Hard-coded `Assets/_GameSource/_Scenes/...` paths; `OpenScene` throws if a scene is missing. Fix: use constants and guard with `File.Exists`.
- `Editor/MissingRefFinder/EditorTools.cs`: Derives from `Editor` for no reason; public method `OpenSceneMENU` is badly cased. Fix: make the class `static` and rename to `OpenSceneMenu` (update callers).
- `Editor/MissingRefFinder/EditorTools.cs`: Unrelated to missing references and sits outside the `Editor` subfolder/asmdef, so it compiles into the runtime assembly. Fix: move it to an `Editor` folder (e.g. `Editor/Tools/`).
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: `findMissingReferences(string, string[], ...)` uses `count = ...` instead of `count += ...`, so only the last asset's count is returned. Fix: use `+=`.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: Editor-only tool derives from `MonoBehaviour`. Fix: make it `static`.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: `FindMissingReferencesInCurrentPrefab` dereferences `prefabStage` before a null check. Fix: add an early null return.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: `SerializedObject` / `GetIterator()` are not disposed in two overloads. Fix: wrap them in `using`.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: `Remove()` is a vague public name and destructive with no confirmation or Undo. Fix: rename to `RemoveMissingScripts` and add a confirmation dialog.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: `showInitialProgressBar(..., bool clearConsole)` parameter shadows the `clearConsole()` method. Fix: rename the parameter.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: Private methods use camelCase while public ones use PascalCase. Fix: use PascalCase consistently.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: `isProjectAsset` uses `path.Substring(1, 2)`, which throws for paths shorter than 3 characters. Fix: use `StartsWith("Assets/")`.
- `Editor/MissingRefFinder/Editor/MissingReferencesFinder.cs`: Uses obsolete `UnityEditor.Experimental.SceneManagement`. Fix: guard with `#if` and use `UnityEditor.SceneManagement`.
- `Editor/MissingRefFinder/Editor/MissingPrefabsFinder.cs`: `catch { }` swallows all exceptions and `GetAllPrefabs` uses `Contains(".prefab")`. Fix: catch specific exceptions and use `EndsWith(".prefab")`.
- `Editor/MissingRefFinder/Editor/MissingPrefabsFinder.cs`: Progress bar is not cleared and the instantiated prefab leaks if `FindMissingPrefabInGO` throws. Fix: use `try/finally`.
- `Editor/MissingRefFinder/Editor/MissingPrefabsFinder.cs`: Version-specific `#if !UNITY_2021_3_OR_NEWER` branch may be dead. Fix: drop it if the minimum Unity version is 2021.3+.
- `Editor/MissingRefFinder/Editor/MissingReferencesResultsWindow.cs`: `ComponentProperty` and `instance` are public/static state; `OnGUI` dereferences `SelectedGameObject.name` while searching. Fix: null-check before reading `.name`.
- `Editor/MissingRefFinder/Editor/MissingReferencesResultsWindow.cs`: `isSearching` is set and cleared synchronously, so the spinner branch is unreachable. Fix: remove it or make the search async.
- `Editor/MissingRefFinder/Editor/MissingReferencesResultsWindow.cs`: camelCase private methods (e.g. `refreshMissingReferences`). Fix: use PascalCase.
- `Editor/MissingRefFinder/Editor/MissingReferencesResultsWindow.cs`: `ComponentProperty` lives here but is used by `MissingReferencesFinder.cs`. Fix: move it to its own file.
- `Editor/Tools/FontReplacer.cs`: Menu path `Top Race 3D/Replace Fonts...` hard-codes a game name. Fix: move under a shared `Tools/` root.
- `Editor/Tools/FontReplacer.cs`: `ReplaceFonts` modifies prefabs without `EditorUtility.SetDirty` / `AssetDatabase.SaveAssets` and never marks scenes dirty. Fix: mark objects dirty and save.
- `Editor/Tools/FontReplacer.cs`: Hard-coded window height `151`. Fix: derive from layout or use a constant.
- `Editor/Tools/DuplicateAssetDetector.cs`: Dummy texture path `Assets/zzy_dummyy_texturee.png` contains typos (asset path, left untouched). Fix: rename the constant value and clean up the old file.
- `Editor/Tools/DuplicateAssetDetector.cs`: Private method `CalculateModelHash` is never used. Fix: remove it.
- `Editor/Tools/DuplicateAssetDetector.cs`: Hash uses `int` arithmetic (`r * 256 * 256 * 256`) and a 32x32 downsample, giving false positives. Fix: use a stronger hash (e.g. MD5 of pixel bytes).
- `Editor/Tools/DuplicateAssetDetector.cs`: An exception with message `"Search aborted"` is used to cancel and `Refresh` logs it as an error. Fix: use a dedicated cancellation path.
- `Editor/Tools/DuplicateAssetDetector.cs`: Appears derived from yasirkula's Duplicate Asset Detector without a license header. Fix: add attribution/license.
- `Editor/**`: Only `MissingRefFinder/Editor` has an `.asmdef`; confirm the other Editor scripts compile under `Assembly-CSharp-Editor`. Fix: verify the folder layout.

## Firebase

- `Firebase/FirebaseManagerInit.cs`, `Firebase/Leaderboard/FirebaseManager.cs`: Near-duplicate initialization and analytics code. Fix: merge or share one init path.
- `Firebase/FirebaseManagerInit.cs`: `app` field is assigned but never read. Fix: remove it or use it.
- `Firebase/FirebaseManagerInit.cs`: `FetchRemoteData` uses `TimeSpan.Zero`, which hits the Remote Config throttle in production. Fix: make it configurable or use 12 h in release builds.
- `Firebase/FirebaseManagerInit.cs`: Remote Config defaults (`game_difficulty`, `daily_reward_coins`, `show_ads`) do not match the keys read (`fail_ad_count`, `hard_mode`). Fix: add defaults for the keys actually used.
- `Firebase/FirebaseManagerInit.cs`: `RemoteGameDifficulty` is a bool filled from `hard_mode`, so the name is misleading (public). Fix: rename to `RemoteHardMode`.
- `Firebase/FirebaseManagerInit.cs`: `GetRemote*` can be called before fetch and silently returns defaults. Fix: expose a ready flag or event.
- `Firebase/Leaderboard/FirebaseManager.cs`: `app` field is assigned but never read. Fix: remove it.
- `Firebase/Leaderboard/FirebaseManager.cs`: `InitializeFirebase` dereferences `Locator.Instance.GameManagerInstance` without a null check. Fix: add a null guard.
- `Firebase/Leaderboard/FirebaseManager.cs`: `RefreshCurrentPlayerRankAndNeighbours(int queryLimit)` ignores `queryLimit`; the +/-5 window is hard-coded. Fix: remove the parameter or apply it.
- `Firebase/Leaderboard/FirebaseManager.cs`: `GetTopLeaderboard` returns without calling `onError` when Firebase is not initialized. Fix: invoke `onError` (or `onSuccess(null)`).
- `Firebase/Leaderboard/FirebaseManager.cs`: `GetOrCreatePlayerProfile` returns the unsanitized `username` on error but `safeName` on success. Fix: pick one consistently.
- `Firebase/Leaderboard/FirebaseManager.cs`: `ChangeCurrentPlayerUsername` updates only the `username` field, so the key stays the old name and rank lookup works only via a fallback match. Fix: define one key policy.
- `Firebase/Leaderboard/FirebaseManager.cs`: The `leaderboard` node is client-writable and the whole leaderboard (3000 default) is downloaded for rank. Fix: add Realtime Database rules and a capped/server-side rank query.
- `Firebase/Leaderboard/FirebaseManager.cs`: Depends on external `Locator`/`GameManagerInstance`, so it does not compile standalone. Fix: document or abstract the dependency.
- `Firebase/FriendSystem/AuthManager.cs`: Unused local `authResult` in `SignInAnonymouslyAndSaveProfileAsync`. Fix: drop the assignment.
- `Firebase/FriendSystem/AuthManager.cs`: Static events (`OnSignInComplete`, `OnSignInFailed`, ...) are never cleared on `OnDestroy`. Fix: clear them in `OnDestroy`.
- `Firebase/FriendSystem/AuthManager.cs`: Document ID is the username, so anyone can claim or overwrite another player's `users/{username}` document. Fix: bind profile creation to the account on the server.
- `Firebase/FriendSystem/AuthManager.cs`: `SanitizeUsername` does not normalize case while `FriendRequestManager` lookups are case-sensitive. Fix: normalize (e.g. lowercase) for storage and lookup.
- `Firebase/FriendSystem/AuthManager.cs`: Stores `firebaseUid` in `users/{username}`, but the rules' `hasOnly(['username','photonUserId'])` reject it. Fix: add `firebaseUid` to the allowed keys.
- `Firebase/FriendSystem/FirestoreRules.txt`: Documents are keyed by username but rules compare `request.auth.uid` with the path ID, so owner checks always fail. Fix: store the owner UID in a field or key documents by `request.auth.uid`.
- `Firebase/FriendSystem/FirestoreRules.txt`: No rules for `gameInvites/{targetUsername}/pending/{inviterUsername}` used by `GameInviteManager`. Fix: add rules.
- `Firebase/FriendSystem/FirestoreRules.txt`: Friend-request `create` accepts a client-supplied `sentAt` with no `request.time` check. Fix: validate `sentAt == request.time`.
- `Firebase/FriendSystem/FirestoreRules.txt`: Stored as `.txt`; the Firebase CLI expects `firestore.rules`. Fix: rename on deploy.
- `Firebase/FriendSystem/FriendManager.cs`: `OnFriendsUpdated` passes a live `_friends.AsReadOnly()` wrapper. Fix: pass a snapshot copy.
- `Firebase/FriendSystem/FriendManager.cs`: Presence polling starts only in `HandlePhotonConnected` when the list is already loaded. Fix: call `StartPresencePolling` from `RebuildFriendList` when `_photonReady`.
- `Firebase/FriendSystem/FriendManager.cs`: The 2 s initial delay and `FindFriends` interval are hard-coded. Fix: move to a serialized field.
- `Firebase/FriendSystem/FriendRequestManager.cs`: `SendFriendRequestAsync` can throw in the pre-checks (only the batch commit is in try/catch). Fix: wrap the whole method body.
- `Firebase/FriendSystem/FriendRequestManager.cs`: Target lookup and pre-check reads are not atomic with the batch write. Fix: use a transaction.
- `Firebase/FriendSystem/FriendItemUI.cs`: `UpdatePresence` is never called by `FriendListView`. Fix: remove it or use it.
- `Firebase/FriendSystem/FriendItemUI.cs`: `OnRemoveClicked` re-enables the button after the await even if the object was destroyed. Fix: check `this != null` first.
- `Firebase/FriendSystem/FriendListView.cs`: `_showInviteButton` is only set by `SetInviteButtonsVisible`, which nothing in this folder calls. Fix: confirm the caller exists.
- `Firebase/FriendSystem/GameInviteManager.cs`: `DeleteInviteDocAsync` is fire-and-forget in the listener callback and `_localUser` may be null after sign-out. Fix: null-check before use.
- `Firebase/FriendSystem/GameInviteManager.cs`: `InviteExpiry` (5 min) differs from `GameInvitePopupUI.AutoRejectSeconds` (10 min). Fix: align the two values.
- `Firebase/FriendSystem/GameInviteManager.cs`: `AcceptInviteAsync` deletes the invite before checking room state, so it is lost if the player is already in a room. Fix: check first, then delete.
- `Firebase/FriendSystem/GameInviteManager.cs`: `bl_LobbyUI.Instance.blackScreenFader.FadeIn(0.3f)` has no null check; 0.3 s is hard-coded. Fix: null-check and expose the duration.
- `Firebase/FriendSystem/GameInvitePopupUI.cs`: `AutoRejectAfterDelay` and the button handlers can race because `_current` is shared across awaits. Fix: capture `_current` in a local.
- `Firebase/FriendSystem/GameInvitePopupUI.cs`: Invite message text is hard-coded. Fix: use a localization table.
- `Firebase/FriendSystem/AddFriendView.cs`: Result strings and the 4 s clear delay are hard-coded; `OnPressedSend` has no try/finally, so an exception leaves the input disabled. Fix: wrap in try/finally.
- `Firebase/FriendSystem/IncomingRequestItemUI.cs`: `OnAcceptClicked` / `OnRejectClicked` never re-enable the buttons if the call fails. Fix: re-enable on failure.

## Managers

- `Managers/DailyTaskManager.cs`: Legacy keys `TaskCompleted1..4` duplicate the `DailyTasks_Task_{i}_Completed` keys. Fix: use a single key scheme.
- `Managers/DailyTaskManager.cs`: `CheckTaskStatus` calls `SetTaskCompleted` -> `BindUI()` up to 4 times per `OnEnable`, and `BindUI` re-adds listeners. Fix: bind once and only refresh.
- `Managers/DailyTaskManager.cs`: `AwardReward` index 1 uses `ingameBoosterAmount[PaintBrush] - 2` and index 3 writes `num_boosters_3` (same as index 2); likely copy/paste bugs. Fix: verify keys/amounts per reward.
- `Managers/DailyTaskManager.cs`: Hard-coded task count (4), 24 h reset, reward amount 500 and booster keys. Fix: move to constants/config.
- `Managers/DailyTaskManager.cs`: `DailyTask`/`TaskUIRow` share the file; `Bind`/`Refresh` and the `ClaimTask` rewardText override duplicate code. Fix: extract a shared `ApplyState` and split files.
- `Managers/DailyTaskManager.cs`: `using ColorCraze.Core;` / `ColorCraze.Game.Common` could not be verified as used (external types). Fix: confirm in the IDE and prune.
- `Managers/DailyTaskManager.cs`: Countdown format `"{Hours:00}H:{Minutes:00}M"` is not localized. Fix: localize.
- `Managers/DailyTaskManager.cs`: Serialized field renamed `rewardtDescriptions` -> `rewardDescriptions` (`FormerlySerializedAs` added); scene/prefab YAML was not checked. Fix: confirm prefabs/scenes re-serialize cleanly.
- `Managers/FakeLeaderboardManager.cs`: `[Header]` on private non-serialized fields (`playerScore`, `totalFakeUsers`) has no effect. Fix: use `[SerializeField]` or remove the headers.
- `Managers/FakeLeaderboardManager.cs`: Local `rangeBelow` in `BuildLeaderboard` is unused. Fix: remove it.
- `Managers/FakeLeaderboardManager.cs`: Hard-coded seed 42, IQ range 88-160, league thresholds, `"MyPlayer"` name and `Locator.Instance.GameManagerInstance.IQPoints` dependency. Fix: move to serialized config.
- `Managers/FakeLeaderboardManager.cs`: `Start` builds the leaderboard without a null check on `Locator.Instance`. Fix: guard or initialize lazily.
- `Managers/FakeLeaderboardManager.cs`: `ClearGrid` destroys all children except the prefab, but `ShowLeaderboard` instantiates under `gridParent`. Fix: verify the prefab is not a scene child.
- `Managers/IAPManager.cs`: Private `AddGold` is empty and never called, so gold purchases grant nothing. Fix: implement the reward or remove the method.
- `Managers/IAPManager.cs`: `OnPurchaseFailed` / `OnPurchaseConfirmationFailed` continue after a null product and log failures with `Debug.Log`. Fix: return early and use `LogWarning`/`LogError`.
- `Managers/IAPManager.cs`: `async void InitializeIAP` swallows `Connect()` exceptions; `Start` dereferences `AdManager.Instance`/`removeAdsButton` without null checks. Fix: add try/catch and null guards.
- `Managers/IAPManager.cs`: Remove-ads purchase uses `PlayerPrefs.SetInt` without `PlayerPrefs.Save()`; gold amounts (500/1500) appear only in log text. Fix: save explicitly and configure amounts.
- `Managers/IAPManager.cs`: `GetFirstProductInOrder` uses `.First()` (throws on an empty cart); `RestorePurchases` calls `UnityIAPServices.StoreController()` instead of `m_StoreController`. Fix: use `FirstOrDefault` and the cached controller.
- `Managers/IAPManager.cs`: Public fields `goldProductId_1/_2` use underscores and `m_StoreController` a legacy prefix. Fix: rename in a dedicated pass (`FormerlySerializedAs` for serialized fields).
- `Managers/LocalizationManager.cs`: `ChangeLanguage` repeats a 13-case switch mapping index to locale code. Fix: use a static array of codes. (Index 0 = Turkish `tr` is language data by design and was kept.)
- `Managers/LocalizationManager.cs`: `GetLocalizedString` returns English debug strings ("Table name empty", "Not found [key]") as user-visible text on failure. Fix: return the key or log a warning.
- `Managers/LocalizationManager.cs`: Bare `catch` in `GetLocalizedString` hides errors. Fix: catch specific exceptions and log.
- `Managers/LocalizationManager.cs`: Default language index 0 is written to PlayerPrefs without `Save()`. Fix: use a named constant and save.
- `Managers/NotifyManager.cs`: Hard-coded channel ID `"default_channel"`, icon names and Android permission string. Fix: extract constants.
- `Managers/NotifyManager.cs`: `Start` schedules notifications on every launch without cancelling earlier ones, so they can duplicate. Fix: call `CancelAllNotifications()` first.
- `Managers/NotifyManager.cs`: New default texts only apply to new instances; serialized scenes/prefabs keep the old text (e.g. "Come back we miss!"). Fix: update the serialized data.
- `Managers/NotifyManager.cs`: `using System.Collections` is only needed under `UNITY_IOS`; Android re-requests the permission each launch. Fix: wrap the using in `#if UNITY_IOS`; request once.
- `Managers/UpgradeManager.cs`: Heavy copy/paste per upgrade (HP/SPEED/GUNRELOAD) and inconsistent public naming (`TankHPUpgradeButton`, `OnPressedUPGRADEButton`). Fix: data-driven upgrade entries; rename in a dedicated pass.
- `Managers/UpgradeManager.cs`: Price is parsed from UI text with `int.Parse` (throws on "FULL"); max level 5, base price 100, satellite price 2000 and the `"money"` key are hard-coded. Fix: compute price from level; use constants.
- `Managers/UpgradeManager.cs`: `OnPressedUPGRADEButton` has no `PlayerPrefs.Save()` and no default case for an unknown `upgradeName`. Fix: add both.
- `Managers/UpgradeManager.cs`: UI text `"BUYED"` was changed to `"BOUGHT"` (grammar); UI strings are not localized. Fix: localize.
- `Managers/UpgradeManager.cs`: `[Header("UPGRADE LEVELS")]` is on private non-serialized fields and has no effect. Fix: remove it.

## Tutorial System

- `Tutorial System/Editor/TutorialManagerEditor.cs`: File name does not match the class `TutorialDevTools`. Fix: rename the file (and its `.meta` via Unity) to `TutorialDevTools.cs`.
- `Tutorial System/Editor/TutorialManagerEditor.cs`: `GameObject.Find("SingUp")` is a hard-coded name (likely typo of "SignUp") and throws if the object is missing. Fix: expose a serialized reference or null-check; fix the scene object name together with the string.
- `Tutorial System/Editor/TutorialManagerEditor.cs`: Hard-coded step index `2` plus `step6Object` encode project-specific flow in a generic tool. Fix: move the logic out of the library or drive it from a step flag.
- `Tutorial System/Editor/TutorialManagerEditor.cs`: Menu path `Tools/WolfTeen/...` contains a project-specific studio name. Fix: use a neutral menu root.
- `Tutorial System/TutorialManager.cs`: Public field `step6Object` is project-specific and non-descriptive. Fix: rename (e.g. `stepObjectToActivate`) with `[FormerlySerializedAs]`.
- `Tutorial System/TutorialManager.cs`: `GetCurrentStepIndex()` logs on every call. Fix: remove the log.
- `Tutorial System/TutorialManager.cs`: Public `PREF_DONE` / `PREF_STEP` use a project-specific `FPS_` prefix. Fix: keep for save compatibility or make configurable.
- `Tutorial System/TutorialManager.cs`: `autoSkipCoroutine` is never assigned, so `StopAutoSkip()` is dead code (auto-skip runs in `TutorialUI.TypewriterRoutine`). Fix: remove both or move auto-skip into the manager.
- `Tutorial System/TutorialManager.cs`: `ui.ShowStep` and `AdvanceStep` can run with null/empty `ui`/`steps` (e.g. `Current`). Fix: add guards.
- `Tutorial System/TutorialManager.cs`: `IsLookingAt` uses `Camera.main` every frame with a magic `0.97f`. Fix: cache the camera and expose the threshold.
- `Tutorial System/TutorialUI.cs`: `ShowContinueIndicator` is an empty, unused public method. Fix: remove or implement it.
- `Tutorial System/TutorialUI.cs`: `spotlightOverlayAlpha`, `typewriterSpeed`, slide values, narrator positions and the 2 s auto-skip delay are hard-coded. Fix: expose as `[SerializeField]` fields.
- `Tutorial System/TutorialUI.cs`: `TypewriterRoutine` appends one character at a time and ignores rich-text tags. Fix: use `maxVisibleCharacters`.
- `Tutorial System/TutorialUI.cs`: `ShowStepCoroutine` is not stopped when `ShowStep` is called again, so coroutines can overlap. Fix: store and stop the previous coroutine.
- `Tutorial System/TutorialUI.cs`: `OnEnable`/`OnDisable` use `bl_Localization.Instance` (may be null at shutdown); `Awake` assumes a parent `Canvas`. Fix: add null checks.
- `Tutorial System/TutorialUI.cs`: `SetFade` and spotlight DOTween tweens are never killed on disable. Fix: kill tweens in `OnDisable`.
- `Tutorial System/TutorialUI.cs`: An empty `Start()` was removed (no behavior). Fix: none needed; listed for review.
- `Tutorial System/TutorialStep.cs`: `GetTitle()`/`GetNarratorText()` use `bl_Localization.Instance` without a null check; the third-party dependency is undocumented. Fix: null-check and document it.
- `Tutorial System/TutorialStep.cs`: Public fields instead of `[SerializeField] private`; `useWorldTracking` is never read. Fix: review and remove unused fields.
- `Tutorial System/TutorialStep.cs`: Menu `FPS/Tutorial Step` and namespace `FPS.Tutorial` are project-specific. Fix: use a neutral namespace when publishing.
- `Tutorial System/TutorialSetup.md`: Mentions `Title / Description`, `DescriptionText` and a `Spotlight Overlay Alpha` Inspector value that do not exist as serialized members. Fix: update the doc or expose the fields.
- `Tutorial System/TutorialSetup.md`: Skippable behavior is described as "show Skip button when autoSkip=false", but code shows Skip whenever `skippable` is true (the delay wording was changed to 2 s to match the code). Fix: confirm the intended behavior.

## Language cleanup notes

- `Firebase/Leaderboard/SampleLeaderJSON.json`: The sample key/username `Kullanıcı81399` was renamed to `User81399`; nothing in the code references it. Fix: none needed.
- `Editor/MissingRefFinder/EditorTools.cs`, `Editor/Tools/FontReplacer.cs`, `Editor/Tools/DuplicateAssetDetector.cs`: Menu paths were re-cased (`Tools/Scenes/Menu`, `Top Race 3D/...`), which can move user-assigned shortcuts. Fix: none needed unless shortcuts were bound to the old paths.
