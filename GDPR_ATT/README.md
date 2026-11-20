# GDPR & ATT (Consent) Setup

> **Ask for tracking and ad consent in the correct order, then start your ads.**

Files: [GDPR.cs](GDPR.cs) (Google UMP consent), [AttPermissionRequest.cs](AttPermissionRequest.cs) (iOS App Tracking Transparency), [PostBuildStep.cs](PostBuildStep.cs) (adds the iOS permission text automatically).

---

## Every file works alone

You can copy any one file into your project. None of them needs another file from this folder or from `ADS/`.

| File | Needs |
|------|-------|
| `GDPR.cs` | Google Mobile Ads plugin |
| `AttPermissionRequest.cs` | iOS only: "Advertisement Legacy" package (`Unity.Advertisement.IosSupport`) |
| `PostBuildStep.cs` | Nothing (Editor script, iOS builds only) |
| `ADS/AdManager.cs` | Google Mobile Ads plugin. It waits by itself until consent allows ads |

If you use several files together, connect them in the **Inspector** (see below), not in code.

---

## Order of events

```
App start
   │
   ▼
ATT prompt (iOS only)        AttPermissionRequest
   │  On Finished Event -> GDPR.StartConsentFlow
   ▼
UMP consent form (GDPR)      GDPR  -> ConsentInformation.Update -> LoadAndShowConsentFormIfRequired
   │
   ▼
CanRequestAds() == true ?    AdManager checks ConsentInformation by itself
   │ yes
   ▼
MobileAds.Initialize         AdManager.InitializeAds()
```

Ads must **not** be initialized before the consent step finishes.

---

## Setup

1. Install the **Google Mobile Ads Unity plugin** (v11 or newer).
2. In AdMob, create a **GDPR message** (Privacy & messaging) and an **IDFA explainer message** if you target iOS.
3. Enter your AdMob App ID in **Assets > Google Mobile Ads > Settings**.
4. Add `GDPR` to a scene that loads first (for example a Splash scene).
5. `AdManager` (AdMob) checks `ConsentInformation.CanRequestAds()` on its own and initializes once it is true (it gives up after `consentWaitTimeout` seconds). For other ad managers, call their `InitializeAds` from `GDPR.OnConsentGathered`.

### Using it with ATT (iOS, optional)

No code is needed. On the same GameObject:

1. `GDPR` > turn **Auto Start** off.
2. `AttPermissionRequest` > **Request On Start** on, then add `GDPR.StartConsentFlow` to **On Finished Event**.

ATT runs first, then the consent form. On Android and in the Editor, `AttPermissionRequest` finishes immediately, so the same setup works everywhere.

### ATT script or UMP's IDFA message?

| Option | When to use |
|--------|-------------|
| UMP IDFA explainer message | You already use UMP. Google shows the ATT prompt for you, so you do **not** need `AttPermissionRequest`. |
| `AttPermissionRequest` | You want to control the timing of the prompt yourself. |

Do not use both for the same prompt.

### iOS text

`PostBuildStep.cs` adds `NSUserTrackingUsageDescription` to Info.plist only if it is missing. Change the text in the script to explain why you track. A vague text can lead to App Store rejection.

---

## Testing

`GDPR` has debug options that work only in the **Editor and development builds**:

- Debug geography: pretend to be in the EEA to see the form.
- Test device IDs: copy your device hash from the Logcat / Xcode log.
- `resetConsentOnStart`: forgets the saved answer so you can see the form again.

Checklist:

- [ ] Form appears with debug geography set to EEA
- [ ] Ads load after accepting
- [ ] Game still starts when the user is outside the EEA
- [ ] Game still starts when there is no internet
- [ ] Privacy options button appears only when `PrivacyOptionsRequirementStatus` says it is required
- [ ] ATT prompt appears on a real iOS device (it never appears in the Editor)

Turn off the debug options before releasing.

---

## Notes

- The ATT dialog appears only once per install. After that, the user must change it in iOS Settings.
- Consent callbacks are moved to the Unity main thread, so you can use Unity APIs inside them.
- Policies change. Check the [Google UMP documentation](https://developers.google.com/admob/unity/privacy) before you release.
