using System;
using UnityEngine;

/// <summary>
/// AppLovin MAX banner + interstitial manager.
/// Standalone: needs only the AppLovin MAX Unity plugin and your SDK key set in AppLovin Integration Manager.
/// </summary>
public class ApplovinAdsManager : MonoBehaviour
{
    public static ApplovinAdsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [Header("Ad Unit IDs (from the AppLovin dashboard)")]
    [SerializeField] private string androidBannerAdUnitId = "ANDROID_BANNER_AD_UNIT_ID";
    [SerializeField] private string iOSBannerAdUnitId = "IOS_BANNER_AD_UNIT_ID";
    [SerializeField] private string androidInterstitialAdUnitId = "ANDROID_INTERSTITIAL_AD_UNIT_ID";
    [SerializeField] private string iOSInterstitialAdUnitId = "IOS_INTERSTITIAL_AD_UNIT_ID";

    [Header("Behaviour")]
    [SerializeField] private bool initializeOnStart = true;
    [Tooltip("Only used when initializeOnStart is on. Leave false until the user has agreed. " +
             "Prefer AppLovin's built-in consent flow (Integration Manager > Terms & Privacy Policy flow).")]
    [SerializeField] private bool hasUserConsent;

    /// <summary>Raised when an interstitial was closed or failed to show. Use it to continue your game flow.</summary>
    public event Action OnInterstitialFinished;

    private string BannerAdUnitId =>
#if UNITY_IOS
        iOSBannerAdUnitId;
#else
        androidBannerAdUnitId;
#endif

    private string InterstitialAdUnitId =>
#if UNITY_IOS
        iOSInterstitialAdUnitId;
#else
        androidInterstitialAdUnitId;
#endif

    private int retryAttempt;
    private bool initialized;

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeAds(hasUserConsent);
        }
    }

    /// <summary>Initializes MAX once. Call it after the user's consent choice is known.</summary>
    public void InitializeAds(bool userConsent)
    {
        if (initialized)
            return;

        initialized = true;

        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
        {
            // Ads can only be loaded once the SDK has finished initializing.
            InitializeBannerAds();
            InitializeInterstitialAds();
        };

        // Consent has to be set before the SDK is initialized, never hard-coded to true.
        MaxSdk.SetHasUserConsent(userConsent);
        MaxSdk.InitializeSdk();
    }

    #region Banner

    public void InitializeBannerAds()
    {
        // Banners are sized 320x50 on phones and 728x90 on tablets.
        MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);

        // MAX requires a background color for banners to work properly.
        MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.white);

        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;

        MaxSdk.ShowBanner(BannerAdUnitId);
    }

    public void ShowBanner() => MaxSdk.ShowBanner(BannerAdUnitId);

    public void HideBanner() => MaxSdk.HideBanner(BannerAdUnitId);

    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("[ApplovinAdsManager] Banner loaded.");
    }

    private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogWarning($"[ApplovinAdsManager] Banner failed to load: {errorInfo.Message}");
    }

    #endregion

    #region Interstitial

    public void InitializeInterstitialAds()
    {
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;

        LoadInterstitial();
    }

    private void LoadInterstitial()
    {
        MaxSdk.LoadInterstitial(InterstitialAdUnitId);
    }

    public void ShowInterstitial()
    {
        if (initialized && MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
        {
            MaxSdk.ShowInterstitial(InterstitialAdUnitId);
        }
        else
        {
            // Never block the game flow because an ad is missing.
            OnInterstitialFinished?.Invoke();
        }
    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        retryAttempt = 0;
    }

    private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // AppLovin recommends retrying with exponentially increasing delays, capped at 64 seconds.
        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));
        Invoke(nameof(LoadInterstitial), (float)retryDelay);
    }

    private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Preload the next ad, as AppLovin recommends after a display failure.
        LoadInterstitial();
        OnInterstitialFinished?.Invoke();
    }

    private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LoadInterstitial();
        OnInterstitialFinished?.Invoke();
    }

    #endregion
}
