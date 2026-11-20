using System;
using System.Collections.Generic;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gathers user consent with Google's User Messaging Platform (UMP), the way the current
/// Google Mobile Ads Unity plugin documents it. Put it in the very first scene.
///
/// Flow on every launch:
///   1. ConsentInformation.Update            - asks Google whether consent / a privacy form is needed.
///   2. LoadAndShowConsentFormIfRequired     - loads and shows the form only if one is needed.
///   3. ConsentInformation.CanRequestAds()   - the ONLY check that decides if ads may be requested.
///
/// Ads must be initialized after this flow, never before it.
///
/// Standalone: needs only the Google Mobile Ads Unity plugin. It does not need any other script from this library.
/// To run ATT first, see "Using it with ATT" in GDPR_ATT/README.md (wired in the Inspector, no code needed).
/// </summary>
public class GDPR : MonoBehaviour
{
    /// <summary>Raised once per launch, after consent gathering is finished (even if it failed).</summary>
    public static event Action<bool> OnConsentGathered;

    [Header("Flow")]
    [Tooltip("Start the consent flow on Start. Turn off if something else (for example the ATT prompt) calls StartConsentFlow().")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool loadNextScene = true;
    [SerializeField] private int nextSceneIndex = 1;

    [Header("Privacy Options (optional)")]
    [Tooltip("Settings button. It is only interactable when the user is required to be able to change consent.")]
    [SerializeField] private Button privacyOptionsButton;

    [Header("Testing (development builds and the Editor only)")]
    [SerializeField] private bool useDebugSettings;
    [SerializeField] private DebugGeography debugGeography = DebugGeography.EEA;
    [Tooltip("The Console prints your hashed device ID the first time Update runs.")]
    [SerializeField] private List<string> testDeviceHashedIds = new List<string>();
    [Tooltip("Clears the saved consent on start. Never ship this enabled.")]
    [SerializeField] private bool resetConsentOnStart;

    /// <summary>True when ConsentInformation says ads can be requested.</summary>
    public static bool CanRequestAds => ConsentInformation.CanRequestAds();

    private bool _started;

    private void Start()
    {
        if (privacyOptionsButton != null)
        {
            privacyOptionsButton.interactable = false;
            privacyOptionsButton.onClick.AddListener(ShowPrivacyOptions);
        }

        if (autoStart)
        {
            StartConsentFlow();
        }
    }

    /// <summary>Starts the consent flow. Runs only once, so it is safe to call from a UnityEvent.</summary>
    public void StartConsentFlow()
    {
        if (_started)
            return;

        _started = true;
        GatherConsent();
    }

    private void OnDestroy()
    {
        if (privacyOptionsButton != null)
        {
            privacyOptionsButton.onClick.RemoveListener(ShowPrivacyOptions);
        }
    }

    private void GatherConsent()
    {
        bool debug = useDebugSettings && (Application.isEditor || Debug.isDebugBuild);

        if (debug && resetConsentOnStart)
        {
            ConsentInformation.Reset();
        }

        var request = new ConsentRequestParameters
        {
            // Set to true if your game is directed at children under the age of consent.
            TagForUnderAgeOfConsent = false
        };

        if (debug)
        {
            request.ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography = debugGeography,
                TestDeviceHashedIds = testDeviceHashedIds
            };
        }

        // Callbacks can arrive on a background thread, so hop to the main thread before using Unity APIs.
        ConsentInformation.Update(request, updateError =>
            MobileAdsEventExecutor.ExecuteInUpdate(() => OnConsentInfoUpdated(updateError)));
    }

    private void OnConsentInfoUpdated(FormError updateError)
    {
        if (updateError != null)
        {
            // Not fatal: the SDK falls back to the consent saved in a previous session.
            Debug.LogError($"[GDPR] Consent info update failed: {updateError.Message}");
            FinishFlow();
            return;
        }

        UpdatePrivacyButton();

        // Shows the form only when the user is in a region that needs one and has not answered yet.
        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (formError != null)
                {
                    Debug.LogError($"[GDPR] Consent form failed: {formError.Message}");
                }

                UpdatePrivacyButton();
                FinishFlow();
            }));
    }

    private void FinishFlow()
    {
        bool canRequestAds = ConsentInformation.CanRequestAds();
        Debug.Log($"[GDPR] Consent flow finished. CanRequestAds = {canRequestAds}");

        OnConsentGathered?.Invoke(canRequestAds);

        if (loadNextScene)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }

    /// <summary>Hook this to a settings button. Reopens the form so the user can change their choice.</summary>
    public void ShowPrivacyOptions()
    {
        ConsentForm.ShowPrivacyOptionsForm(error =>
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (error != null)
                {
                    Debug.LogError($"[GDPR] Privacy options form failed: {error.Message}");
                }

                UpdatePrivacyButton();
            }));
    }

    private void UpdatePrivacyButton()
    {
        if (privacyOptionsButton == null)
            return;

        // The entry point must only be usable when a privacy form requires it.
        privacyOptionsButton.interactable =
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
    }
}
