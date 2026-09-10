using System;
using UnityEngine;

public class ApplovinAdsManager : Singleton<AdsManagerAPPLOVIN>
{
    public override void Awake()
    {
        base.Awake();
    }

    ///////// MAX SDK INIT
    private void Start()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) => {
        // AppLovin SDK is initialized, start loading ads
            InitializeBannerAds();
            InitializeInterstitialAds();
        };
        MaxSdk.InitializeSdk();
        MaxSdk.SetHasUserConsent(true);
    }

    /////// == AD UNITS ==

    //////////////////////////////// BANNER

#if UNITY_IOS
    string bannerAdUnitId = "2db5a6c2b7493ff3"; // IOS BANNER !!
#else // UNITY_ANDROID
    string bannerAdUnitId = "«Android-ad-unit-ID»";
#endif

    public void InitializeBannerAds()
    {
        // Banners are automatically sized to 320×50 on phones and 728×90 on tablets
        // You may call the utility method MaxSdkUtils.isTablet() to help with view sizing adjustments
        MaxSdk.CreateBanner(bannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);

        // Set background color for banners to be fully functional
        MaxSdk.SetBannerBackgroundColor(bannerAdUnitId, Color.white);

        MaxSdkCallbacks.Banner.OnAdLoadedEvent      += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent  += OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent     += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdExpandedEvent    += OnBannerAdExpandedEvent;
        MaxSdkCallbacks.Banner.OnAdCollapsedEvent   += OnBannerAdCollapsedEvent;
    }

    /// Callbacks
    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {}

    private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo) {}

    private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {}

    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {}

    private void OnBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)  {}

    private void OnBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {}

    ////////////// BANNER METHODS
    /*
        MaxSdk.ShowBanner(«ad-unit-ID»);
        MaxSdk.HideBanner(«ad-unit-ID»);
        MaxSdk.DestroyBanner(«ad-unit-ID»);
        MaxSdk.LoadBanner(«ad-unit-ID»);
    */

    ///////////////////////// INTERSTITIAL

#if UNITY_IOS
    string adUnitId = "6417e181e9d09e17"; // IOS  INTERSTITIAL !!
#else // UNITY_ANDROID
    string adUnitId = "«Android-ad-unit-ID»";
#endif

    int retryAttempt;

    public void InitializeInterstitialAds()
    {
        // Attach callback
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;

        // Load the first interstitial
        LoadInterstitial();
    }

    private void LoadInterstitial()
    {
        MaxSdk.LoadInterstitial(adUnitId);
    }

    public void ShowInterstitial()
    {
        if (MaxSdk.IsInterstitialReady(adUnitId) )
        {
            MaxSdk.ShowInterstitial(adUnitId);
        }
    }

    /// Callbacks
    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is ready for you to show. MaxSdk.IsInterstitialReady(adUnitId) now returns 'true'
        // Reset retry attempt
        retryAttempt = 0;
    }

    private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Interstitial ad failed to load
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)
        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));
        Invoke("LoadInterstitial", (float) retryDelay);
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {}

    private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        GameManager.Instance.SceneLoad();
        // Interstitial ad failed to display. AppLovin recommends that you load the next ad.
        LoadInterstitial();
    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {}

    private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        GameManager.Instance.SceneLoad();
        // Interstitial ad is hidden. Pre-load the next ad.
        LoadInterstitial();
    }
}