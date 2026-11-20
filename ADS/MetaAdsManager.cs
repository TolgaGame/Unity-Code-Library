using System;
using System.Collections.Generic;
using AudienceNetwork;
using UnityEngine;

/// <summary>
/// Meta Audience Network (Facebook Audience Network) banner, interstitial and rewarded video manager.
///
/// Setup:
///   1. Import the Audience Network Unity SDK (.unitypackage from Meta) and resolve dependencies.
///   2. Create placements in Meta Monetization Manager and paste the IDs below (one per ad unit and platform).
///   3. Ask for ATT / consent first, then call InitializeAds(). Do not initialize before the user has answered.
///
/// Callback names follow Meta's official Unity docs. If your SDK version renamed one, the compiler will point to it.
/// </summary>
public class MetaAdsManager : MonoBehaviour
{
    [Header("Placement IDs (from Meta Monetization Manager)")]
    [SerializeField] private string androidBannerPlacementId = "ANDROID_BANNER_PLACEMENT_ID";
    [SerializeField] private string iOSBannerPlacementId = "IOS_BANNER_PLACEMENT_ID";
    [SerializeField] private string androidInterstitialPlacementId = "ANDROID_INTERSTITIAL_PLACEMENT_ID";
    [SerializeField] private string iOSInterstitialPlacementId = "IOS_INTERSTITIAL_PLACEMENT_ID";
    [SerializeField] private string androidRewardedPlacementId = "ANDROID_REWARDED_PLACEMENT_ID";
    [SerializeField] private string iOSRewardedPlacementId = "IOS_REWARDED_PLACEMENT_ID";

    [Header("Behaviour")]
    [SerializeField] private bool initializeOnStart;
    [SerializeField] private AdPosition bannerPosition = AdPosition.BOTTOM;

    [Header("Testing")]
    [Tooltip("Hashed device IDs that always receive test ads. The Console prints yours on the first request.")]
    [SerializeField] private List<string> testDeviceHashedIds = new List<string>();

    /// <summary>Raised when an interstitial was closed or could not be shown. Use it to continue your game flow.</summary>
    public event Action OnInterstitialFinished;

    private AdView bannerAd;
    private InterstitialAd interstitialAd;
    private RewardedVideoAd rewardedAd;
    private bool interstitialLoaded;
    private bool rewardedLoaded;
    private bool initialized;
    private Action onRewardEarned;

    private string BannerPlacementId =>
#if UNITY_IOS
        iOSBannerPlacementId;
#else
        androidBannerPlacementId;
#endif

    private string InterstitialPlacementId =>
#if UNITY_IOS
        iOSInterstitialPlacementId;
#else
        androidInterstitialPlacementId;
#endif

    private string RewardedPlacementId =>
#if UNITY_IOS
        iOSRewardedPlacementId;
#else
        androidRewardedPlacementId;
#endif

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeAds(true);
        }
    }

    /// <param name="advertiserTrackingEnabled">
    /// iOS only: pass true when the ATT status is Authorized, otherwise false.
    /// </param>
    public void InitializeAds(bool advertiserTrackingEnabled)
    {
        if (initialized)
            return;

        initialized = true;

        foreach (string deviceId in testDeviceHashedIds)
        {
            AdSettings.AddTestDevice(deviceId);
        }

#if UNITY_IOS
        AdSettings.SetAdvertiserTrackingEnabled(advertiserTrackingEnabled);
#endif

        AudienceNetworkAds.Initialize();

        LoadInterstitial();
        LoadRewarded();
    }

    #region Banner

    public void LoadBanner()
    {
        if (!initialized)
            return;

        if (bannerAd != null)
        {
            bannerAd.Dispose();
        }

        bannerAd = new AdView(BannerPlacementId, AdSize.BANNER_HEIGHT_50);
        bannerAd.Register(gameObject);

        bannerAd.AdViewDidLoad = () =>
        {
            Debug.Log("[MetaAdsManager] Banner loaded.");
            bannerAd.Show(bannerPosition);
        };
        bannerAd.AdViewDidFailWithError = error =>
        {
            Debug.LogWarning("[MetaAdsManager] Banner failed to load: " + error);
        };
        bannerAd.AdViewWillLogImpression = () => Debug.Log("[MetaAdsManager] Banner impression.");
        bannerAd.AdViewDidClick = () => Debug.Log("[MetaAdsManager] Banner clicked.");

        bannerAd.LoadAd();
    }

    public void HideBanner()
    {
        if (bannerAd != null)
        {
            bannerAd.Dispose();
            bannerAd = null;
        }
    }

    #endregion

    #region Interstitial

    public void LoadInterstitial()
    {
        if (!initialized)
            return;

        if (interstitialAd != null)
        {
            interstitialAd.Dispose();
        }

        interstitialLoaded = false;
        interstitialAd = new InterstitialAd(InterstitialPlacementId);
        interstitialAd.Register(gameObject);

        interstitialAd.InterstitialAdDidLoad = () =>
        {
            Debug.Log("[MetaAdsManager] Interstitial loaded.");
            interstitialLoaded = true;
        };
        interstitialAd.InterstitialAdDidFailWithError = error =>
        {
            Debug.LogWarning("[MetaAdsManager] Interstitial failed to load: " + error);
        };
        interstitialAd.InterstitialAdWillLogImpression = () => Debug.Log("[MetaAdsManager] Interstitial impression.");
        interstitialAd.InterstitialAdDidClick = () => Debug.Log("[MetaAdsManager] Interstitial clicked.");
        interstitialAd.interstitialAdDidClose = () =>
        {
            Debug.Log("[MetaAdsManager] Interstitial closed.");
            interstitialAd.Dispose();
            interstitialAd = null;

            // An ad can only be shown once, so load the next one right away.
            LoadInterstitial();
            OnInterstitialFinished?.Invoke();
        };

        interstitialAd.LoadAd();
    }

    public void ShowInterstitial()
    {
        if (initialized && interstitialLoaded && interstitialAd != null)
        {
            interstitialLoaded = false;
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[MetaAdsManager] Interstitial is not ready yet.");
            // Never block the game flow because an ad is missing.
            OnInterstitialFinished?.Invoke();
            LoadInterstitial();
        }
    }

    #endregion

    #region Rewarded

    public bool IsRewardedReady => initialized && rewardedLoaded && rewardedAd != null;

    public void LoadRewarded()
    {
        if (!initialized)
            return;

        if (rewardedAd != null)
        {
            rewardedAd.Dispose();
        }

        rewardedLoaded = false;
        rewardedAd = new RewardedVideoAd(RewardedPlacementId);
        rewardedAd.Register(gameObject);

        rewardedAd.RewardedVideoAdDidLoad = () =>
        {
            Debug.Log("[MetaAdsManager] Rewarded video loaded.");
            rewardedLoaded = true;
        };
        rewardedAd.RewardedVideoAdDidFailWithError = error =>
        {
            Debug.LogWarning("[MetaAdsManager] Rewarded video failed to load: " + error);
        };
        rewardedAd.RewardedVideoAdWillLogImpression = () => Debug.Log("[MetaAdsManager] Rewarded impression.");
        rewardedAd.RewardedVideoAdDidClick = () => Debug.Log("[MetaAdsManager] Rewarded clicked.");

        // Fires only when the whole video was watched.
        rewardedAd.RewardedVideoAdComplete = () =>
        {
            Debug.Log("[MetaAdsManager] Rewarded video completed.");
            onRewardEarned?.Invoke();
            onRewardEarned = null;
        };
        rewardedAd.RewardedVideoAdDidClose = () =>
        {
            Debug.Log("[MetaAdsManager] Rewarded video closed.");
            onRewardEarned = null;
            rewardedAd.Dispose();
            rewardedAd = null;
            LoadRewarded();
        };

        rewardedAd.LoadAd();
    }

    /// <summary>Shows a rewarded video. onRewardEarned runs only if the user watched it completely.</summary>
    public void ShowRewarded(Action onRewardEarned)
    {
        if (!IsRewardedReady)
        {
            Debug.LogWarning("[MetaAdsManager] Rewarded video is not ready yet.");
            LoadRewarded();
            return;
        }

        this.onRewardEarned = onRewardEarned;
        rewardedLoaded = false;
        rewardedAd.Show();
    }

    #endregion

    private void OnDestroy()
    {
        // Native ad objects hold platform resources and must be released explicitly.
        if (bannerAd != null) bannerAd.Dispose();
        if (interstitialAd != null) interstitialAd.Dispose();
        if (rewardedAd != null) rewardedAd.Dispose();
    }
}
