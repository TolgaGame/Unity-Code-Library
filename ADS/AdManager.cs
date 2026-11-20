using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System;
using System.Collections;

/// <summary>
/// AdMob banner, interstitial and rewarded ads.
/// Standalone: needs only the Google Mobile Ads Unity plugin. It does not need GDPR.cs or any other script
/// from this library. It simply waits until ConsentInformation says ads may be requested.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    public bool dontDestroyOnLoad;

    /// <summary>Initializes the AdMob SDK and loads ads on Start.</summary>
    public bool ADSEnabled;

    /// <summary>Overrides the ad unit IDs with Google's test IDs.</summary>
    public bool TestAdsEnabled;

    [Tooltip("Seconds to wait for the consent flow before giving up on automatic initialization.")]
    [SerializeField] private float consentWaitTimeout = 60f;

    private BannerView _bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private bool _initialized;
    private Action _onRewardEarned;

    /// <summary>Raised after an interstitial is closed (or failed to show). Use it to continue your game flow.</summary>
    public event Action OnInterstitialFinished;

    [Header("AdMob IDs")]
    // Replace these with the real ad unit IDs before release.
    public string bannerID;
    public string interstitialID;
    public string rewardVideoID;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (TestAdsEnabled)
        {
            Debug.Log("[AdManager] Test ads enabled, using test IDs.");
#if UNITY_IOS
            bannerID = "ca-app-pub-3940256099942544/2934735716";
            interstitialID = "ca-app-pub-3940256099942544/4411468910";
            rewardVideoID = "ca-app-pub-3940256099942544/1712485313";
#else
            bannerID = "ca-app-pub-3940256099942544/6300978111";
            interstitialID = "ca-app-pub-3940256099942544/1033173712";
            rewardVideoID = "ca-app-pub-3940256099942544/5224354917";
#endif
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (ADSEnabled)
        {
            StartCoroutine(InitializeWhenConsentReady());
        }
    }

    /// <summary>Turns ads off at runtime (for example after a "Remove Ads" purchase) and removes the banner.</summary>
    public void DisableAds()
    {
        ADSEnabled = false;
        DestroyAdBanner();
    }

    // Google requires user consent first. The consent flow (GDPR.cs or your own) can finish after this
    // object starts, so check until ConsentInformation allows ad requests. Gives up after the timeout.
    private IEnumerator InitializeWhenConsentReady()
    {
        float waited = 0f;

        while (!ConsentInformation.CanRequestAds())
        {
            if (waited >= consentWaitTimeout)
            {
                Debug.LogWarning("[AdManager] Consent was not ready in time, ads were not initialized. " +
                                 "Run a consent flow (see GDPR_ATT) or call InitializeAds() later.");
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            waited += 0.5f;
        }

        InitializeAds();
    }

    /// <summary>Initializes the SDK once. Does nothing until ConsentInformation.CanRequestAds() is true.</summary>
    public void InitializeAds()
    {
        if (_initialized)
            return;

        if (!ConsentInformation.CanRequestAds())
        {
            Debug.LogWarning("[AdManager] Consent is not ready yet, ads were not initialized.");
            return;
        }

        _initialized = true;

        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // Ad callbacks arrive on a background thread, so hop to the main thread for Unity API calls.
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                LoadAdBanner();
                LoadInterstitialAd();
                // Rewarded ads are not preloaded here. Call LoadRewardedAd() when your game needs one.
            });
        });
    }

    public void RequestBannerView()
    {
        Debug.Log("[AdManager] Creating banner view");

        // Destroy the previous banner so only one exists at a time.
        if (_bannerView != null)
        {
            DestroyAdBanner();
        }

        // Anchored adaptive banner that respects the device's safe area.
        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                MobileAds.Utils.GetDeviceSafeWidth());

        _bannerView = new BannerView(bannerID, adaptiveSize, AdPosition.Bottom);
        ListenToAdEvents();
    }

    public void LoadAdBanner()
    {
        if (_bannerView == null)
        {
            RequestBannerView();
        }

        var adRequest = new AdRequest();

        Debug.Log("[AdManager] Loading banner ad.");
        _bannerView.LoadAd(adRequest);
    }

    public void DestroyAdBanner()
    {
        if (_bannerView != null)
        {
            Debug.Log("[AdManager] Destroying banner ad.");
            _bannerView.Destroy();
            _bannerView = null;
        }
    }

    public void HideBannerAd()
    {
        if (_bannerView != null)
            _bannerView.Hide();
    }

    public void ShowBannerAd()
    {
        if (_bannerView != null)
            _bannerView.Show();
    }

    private void ListenToAdEvents()
    {
        _bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("[AdManager] Banner view loaded an ad with response: "
                + _bannerView.GetResponseInfo());
        };
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("[AdManager] Banner view failed to load an ad with error: "
                + error);
        };
        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("[AdManager] Banner view paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        _bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdManager] Banner view recorded an impression.");
        };
        _bannerView.OnAdClicked += () =>
        {
            Debug.Log("[AdManager] Banner view was clicked.");
        };
        _bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdManager] Banner view full screen content opened.");
        };
        _bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdManager] Banner view full screen content closed.");
        };
    }

    public void LoadInterstitialAd()
    {
        // Destroy the previous ad before loading a new one to release its resources.
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        Debug.Log("[AdManager] Loading the interstitial ad.");

        var adRequest = new AdRequest();

        InterstitialAd.Load(interstitialID, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("[AdManager] Interstitial ad failed to load an ad " +
                                    "with error: " + error);
                    return;
                }

                Debug.Log("[AdManager] Interstitial ad loaded with response: "
                            + ad.GetResponseInfo());

                interstitialAd = ad;

                RegisterEventHandlers(interstitialAd);
            });
    }

    public void ShowAdInterstitial()
    {
        if (!ADSEnabled)
        {
            OnInterstitialFinished?.Invoke();
            return;
        }

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("[AdManager] Showing interstitial ad.");
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdManager] Interstitial ad is not ready yet.");
            // Never block the game flow because an ad is missing.
            OnInterstitialFinished?.Invoke();
            LoadInterstitialAd();
        }
    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        Debug.Log("[AdManager] Registering interstitial ad callbacks");

        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("[AdManager] Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdManager] Interstitial ad recorded an impression.");
        };
        ad.OnAdClicked += () =>
        {
            Debug.Log("[AdManager] Interstitial ad was clicked.");
        };
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdManager] Interstitial ad full screen content opened.");
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdManager] Interstitial ad full screen content closed.");
            // An ad can only be shown once, so load the next one right away.
            LoadInterstitialAd();
            RaiseInterstitialFinished();
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("[AdManager] Interstitial ad failed to open full screen content " +
                        "with error: " + error);
            LoadInterstitialAd();
            RaiseInterstitialFinished();
        };
    }

    // Ad events arrive on a background thread; game code usually touches Unity APIs, so deliver on the main thread.
    private void RaiseInterstitialFinished()
    {
        GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() => OnInterstitialFinished?.Invoke());
    }

    public bool IsRewardedAdReady()
    {
        return rewardedAd != null && rewardedAd.CanShowAd();
    }

    public void LoadRewardedAd()
    {
        // Destroy the previous ad before loading a new one to release its resources.
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        Debug.Log("[AdManager] Loading the rewarded ad.");

        var adRequest = new AdRequest();

        RewardedAd.Load(rewardVideoID, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("[AdManager] Rewarded ad failed to load an ad " +
                                    "with error: " + error);
                    return;
                }

                Debug.Log("[AdManager] Rewarded ad loaded with response: "
                            + ad.GetResponseInfo());

                rewardedAd = ad;

                RegisterEventHandlersRewarded(rewardedAd);
            });
    }

    /// <summary>Shows a rewarded ad. onRewardEarned runs only if the user watched enough to earn the reward.</summary>
    public void ShowRewardedAd(Action onRewardEarned)
    {
        const string rewardMsg =
            "[AdManager] Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            _onRewardEarned = onRewardEarned;
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    _onRewardEarned?.Invoke();
                    _onRewardEarned = null;
                });
            });
        }
        else
        {
            Debug.LogWarning("[AdManager] Rewarded ad is not ready yet.");
            LoadRewardedAd();
        }
    }

    private void RegisterEventHandlersRewarded(RewardedAd ad)
    {
        Debug.Log("[AdManager] Registering rewarded ad callbacks");

        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("[AdManager] Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdManager] Rewarded ad recorded an impression.");
        };
        ad.OnAdClicked += () =>
        {
            Debug.Log("[AdManager] Rewarded ad was clicked.");
        };
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdManager] Rewarded ad full screen content opened.");
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdManager] Rewarded ad full screen content closed.");
            LoadRewardedAd();
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("[AdManager] Rewarded ad failed to open full screen content " +
                        "with error: " + error);
            LoadRewardedAd();
        };
    }
}