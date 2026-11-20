using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

/// <summary>
/// Shows Apple's App Tracking Transparency (ATT) prompt on iOS 14.5+.
/// Standalone: it does not need GDPR.cs or any other script from this library.
/// Needs the "Advertisement Legacy" package (Unity.Advertisement.IosSupport) on iOS builds and an
/// NSUserTrackingUsageDescription entry in Info.plist (see PostBuildStep.cs).
///
/// Order matters: ask for ATT BEFORE initializing any ad SDK, so the SDK can read the IDFA if allowed.
/// Use "On Finished Event" to start the next step (for example GDPR.StartConsentFlow) or call Request(...) yourself.
///
/// If you use AdMob's UMP "IDFA explainer" message instead, UMP shows the ATT prompt itself,
/// so you do not need this component.
/// </summary>
public class AttPermissionRequest : MonoBehaviour
{
    [Tooltip("Run the request by itself on Start.")]
    [SerializeField] private bool requestOnStart = true;

    [Tooltip("iOS can silently ignore a prompt requested before the app is fully active.")]
    [SerializeField] private float startDelay = 0.5f;

    [Tooltip("Invoked when the request finished. Safe to use on every platform: non-iOS platforms finish immediately.")]
    [SerializeField] private UnityEvent onFinishedEvent;

    /// <summary>True after the user (or the OS) has answered, or on platforms without ATT.</summary>
    public bool IsFinished { get; private set; }

    private void Start()
    {
        if (requestOnStart)
        {
            Request(null);
        }
    }

    /// <summary>Requests ATT if needed, then invokes onFinished. Safe to call on every platform.</summary>
    public void Request(Action onFinished)
    {
        StartCoroutine(RequestRoutine(onFinished));
    }

    private IEnumerator RequestRoutine(Action onFinished)
    {
#if UNITY_IOS && !UNITY_EDITOR
        yield return new WaitForSecondsRealtime(startDelay);

        // Only prompt while the user's tracking status is still undetermined.
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
            ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();

            // The native call returns immediately, so wait until the user has actually answered.
            while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                   ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }
#else
        yield return null;
#endif

        IsFinished = true;
        onFinished?.Invoke();
        onFinishedEvent?.Invoke();
    }
}
