using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Ump;
using GoogleMobileAds.Ump.Api;
using UnityEngine.SceneManagement;

public class GDPRS : MonoBehaviour
{
    ConsentForm _consentForm;

    void Start()
    {
        var debugSettings = new ConsentDebugSettings
        {
            // Simulates a device located in the EEA.
            DebugGeography = DebugGeography.EEA,
            TestDeviceHashedIds = new List<string>
            {
                "965E4A26737DF85475A353251709C315"
            }
        };

        // false: the user is not under the age of consent.
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = debugSettings,
        };

        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            UnityEngine.Debug.LogError($"[GDPRS] {error}");
            return;
        }

        if (ConsentInformation.IsConsentFormAvailable())
        {
            LoadConsentForm();
        }

        SceneManager.LoadScene(1);
    }

    void LoadConsentForm()
    {
        ConsentForm.Load(OnLoadConsentForm);
    }

    void OnLoadConsentForm(ConsentForm consentForm, FormError error)
    {
        if (error != null)
        {
            UnityEngine.Debug.LogError($"[GDPRS] {error}");
            return;
        }

        // Keep the form so it can be shown again later.
        _consentForm = consentForm;

        if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
        {
            _consentForm.Show(OnShowForm);
        }
    }

    void OnShowForm(FormError error)
    {
        if (error != null)
        {
            UnityEngine.Debug.LogError($"[GDPRS] {error}");
            return;
        }

        // Reload so the next request has a fresh form after dismissal.
        LoadConsentForm();
    }
}