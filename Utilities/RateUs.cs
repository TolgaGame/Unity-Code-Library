// Requires the StoreKit framework to be added in Xcode.
using UnityEngine;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class RateUs : MonoBehaviour
{
    public GameObject rateUsPanel;

    [Header("Store Info")]
    public string androidPackageName = "";

    bool reviewRequested = false;

    public void Rate()
    {
        Debug.Log("[RateUs] Rate called");

#if UNITY_IOS
        ShowIOSRate();
#elif UNITY_ANDROID
        rateUsPanel.SetActive(true);
#else
        Debug.Log("[RateUs] Rating only works on real iOS / Android devices.");
#endif
    }

#if UNITY_IOS
    private void ShowIOSRate()
    {
        Locator.Instance.SaveSystemInstance.SetRateUsPanelShown(true);
        if (reviewRequested == false)
        {
            bool popupShown = Device.RequestStoreReview();
            if (popupShown)
            {
                // iOS gives no way to know whether the user actually left a review or dismissed the popup.
                reviewRequested = true;
            }
            else
            {
                // Reset so the request can be retried later.
                Debug.Log("[RateUs] iOS version is too low or StoreKit framework was not linked.");
                reviewRequested = false;
            }
        }
    }
#endif

    public void ShowAndroidRate()
    {
        Locator.Instance.SaveSystemInstance.SetRateUsPanelShown(true);

        string packageName = string.IsNullOrEmpty(androidPackageName)
            ? Application.identifier
            : androidPackageName;

        // https is accepted on every device, unlike the market:// scheme.
        string url = "https://play.google.com/store/apps/details?id=" + packageName;
        Application.OpenURL(url);
    }
}