using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdManager : Singleton<AdManager>
{
    /// <summary>Initializes the AdMob SDK and loads ads on Start.</summary>
    public bool ADSEnabled;

    /// <summary>Overrides the ad unit IDs with Google's test IDs.</summary>
    public bool TestAdsEnabled;

    private BannerView _bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    [Header("AdMob IDs")]
    // Replace these with the real ad unit IDs before release.
    public string bannerID;
    public string interstitialID;
    public string rewardVideoID;

    public override void Awake()
    {
        base.Awake();

        if (TestAdsEnabled)
        {
            Debug.Log("[AdManager] Test ads enabled, using test IDs.");
            bannerID = "ca-app-pub-3940256099942544/6300978111";
            interstitialID = "ca-app-pub-3940256099942544/1033173712";
            rewardVideoID = "ca-app-pub-3940256099942544/5224354917";
        }
    }

    private void Start()
    {
        if (ADSEnabled)
        {
            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                LoadAdBanner();
                LoadInterstitialAd();
                // Rewarded ads are not used in this game, so none are preloaded.
            });
        }
    }

    public void RequestBannerView()
    {
        Debug.Log("[AdManager] Creating banner view");

        // Destroy the previous banner so only one exists at a time.
        if (_bannerView != null)
        {
            DestroyAdBanner();
        }

        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        _bannerView = new BannerView(bannerID, adaptiveSize, AdPosition.Bottom);
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
        if (ADSEnabled)
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                Debug.Log("[AdManager] Showing interstitial ad.");
                interstitialAd.Show();
            }
            else
            {
                Debug.LogError("[AdManager] Interstitial ad is not ready yet.");
            }
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
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("[AdManager] Interstitial ad failed to open full screen content " +
                        "with error: " + error);
        };
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

    public void ShowRewardedAd()
    {
        const string rewardMsg =
            "[AdManager] Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                // TODO: Reward the user.
                Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
            });
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
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("[AdManager] Rewarded ad failed to open full screen content " +
                        "with error: " + error);
        };
    }
}