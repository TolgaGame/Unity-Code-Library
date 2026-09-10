// StoreKit framework ekle XCODE
using UnityEngine;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class RateUs : MonoBehaviour
{
    public GameObject rateUsPanel;

	[Header("Store Bilgileri")]  
	public string androidPackageName = ""; 

    bool reviewRequested = false;
    
    /////////////////////////////////////

	public void Rate()
	{

    Debug.Log("Rate Us Çağrıldı");

#if UNITY_IOS
		ShowIOSRate();
#elif UNITY_ANDROID
        rateUsPanel.SetActive(true);   
#else
		Debug.Log("RateUs sadece gerçek iOS / Android cihazlarda çalışır.");
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
                // The review popup was presented to the user, set "reviewRequested" to "true" to reflect that
                // Note: there's no way to check if the user actually gave a review for the app or cancelled the popup.
                reviewRequested = true;
            }
            else
            {
                // The review popup wasn't presented. Log a message and reset "reviewRequested" so you can revisit this in the future.
                Debug.Log("iOS version is too low or StoreKit framework was not linked.");
                reviewRequested = false;
            }
        }

	}

#endif

    public void ShowAndroidRate()
    {
        Locator.Instance.SaveSystemInstance.SetRateUsPanelShown(true);

        // Paket adını al
        string packageName = string.IsNullOrEmpty(androidPackageName)
            ? Application.identifier
            : androidPackageName;

        // Play Store URL (http kullanmak her cihazda daha uyumlu)
        string url = "https://play.google.com/store/apps/details?id=" + packageName;
        Application.OpenURL(url);
    }

}