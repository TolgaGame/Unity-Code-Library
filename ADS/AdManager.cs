using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdManager : Singleton<AdManager> {

	public bool ADSEnabled; /// Enabled Admob Ads Init
	public bool TestAdsEnabled; /// Test Admob Ads Init and Enabled

	//// AD UNIT INSTANCE
	private BannerView _bannerView;
	private InterstitialAd interstitialAd;
	private RewardedAd rewardedAd;

	[Header("ADMOB IDS")]
    //These IDs have to be changed to the actual app and ad IDs!!!
	public string bannerID;
	public string interstitialID;
	public string rewardVideoID;

	////////////////////////////////////////////

	public override void Awake()
	{
		base.Awake();

		/// TEST ADS ID ADMOB
		if (TestAdsEnabled)
		{
			/// TEST ADS ID ADMOB GENERAL
			Debug.Log("TEST ADS ENABLES USING TEST IDS");
			bannerID = "ca-app-pub-3940256099942544/6300978111";
			interstitialID = "ca-app-pub-3940256099942544/1033173712";
			rewardVideoID = "ca-app-pub-3940256099942544/5224354917";
		}
	}

	private void Start()
	{
		if (ADSEnabled)
		{
			// Initialize the Google Mobile Ads SDK.
			MobileAds.Initialize((InitializationStatus initStatus) =>
			{
				// This callback is called once the MobileAds SDK is initialized.
				LoadAdBanner();
				LoadInterstitialAd();
				//LoadRewardedAd(); This game not using any rewarded
			});
		}
    }

	///////////////////////////////// BANNER METHODS

	/// Banner Ads
	public void RequestBannerView()
	{
		Debug.Log("Creating banner view");

		// If we already have a banner, destroy the old one.
		if (_bannerView != null)
		{
			DestroyAdBanner();
		}

		/// Adaptive Banner is Active
		AdSize adaptiveSize =
			AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        _bannerView = new BannerView(bannerID, adaptiveSize, AdPosition.Bottom);
	}

	/// The banner view and loads a banner ad.
	public void LoadAdBanner()
	{
		//  an instance of a banner view first.
		if(_bannerView == null)
		{
			RequestBannerView();
		}

		// our request used to load the ad.
		var adRequest = new AdRequest();

		// send the request to load the ad.
		Debug.Log("Loading banner ad.");
		_bannerView.LoadAd(adRequest);
	}

	/// Destroys the ad.
	public void DestroyAdBanner()
	{
		if (_bannerView != null)
		{
			Debug.Log("Destroying banner ad.");
			_bannerView.Destroy();
			_bannerView = null;
		}
	}

	public void HideBannerAd() {
		if (_bannerView != null)
			_bannerView.Hide();
	}

	public void ShowBannerAd() {
		if (_bannerView != null)
			_bannerView.Show();
	}
	
	/// listen to events the banner may raise.
	private void ListenToAdEvents()
	{
		// Raised when an ad is loaded into the banner view.
		_bannerView.OnBannerAdLoaded += () =>
		{
			Debug.Log("Banner view loaded an ad with response : "
				+ _bannerView.GetResponseInfo());
		};
		// Raised when an ad fails to load into the banner view.
		_bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
		{
			Debug.LogError("Banner view failed to load an ad with error : "
				+ error);
		};
		// Raised when the ad is estimated to have earned money.
		_bannerView.OnAdPaid += (AdValue adValue) =>
		{
			Debug.Log(String.Format("Banner view paid {0} {1}.",
				adValue.Value,
				adValue.CurrencyCode));
		};
		// Raised when an impression is recorded for an ad.
		_bannerView.OnAdImpressionRecorded += () =>
		{
			Debug.Log("Banner view recorded an impression.");
		};
		// Raised when a click is recorded for an ad.
		_bannerView.OnAdClicked += () =>
		{
			Debug.Log("Banner view was clicked.");
		};
		// Raised when an ad opened full screen content.
		_bannerView.OnAdFullScreenContentOpened += () =>
		{
			Debug.Log("Banner view full screen content opened.");
		};
		// Raised when the ad closed full screen content.
		_bannerView.OnAdFullScreenContentClosed += () =>
		{
			Debug.Log("Banner view full screen content closed.");
		};
	}

	/////////////////////// INTERSTIAL METHODS

	/// Loads the interstitial ad.
	public void LoadInterstitialAd()
	{
		// Clean up the old ad before loading a new one.
		if (interstitialAd != null)
		{
			interstitialAd.Destroy();
			interstitialAd = null;
		}

		Debug.Log("Loading the interstitial ad.");

		// create our request used to load the ad.
		var adRequest = new AdRequest();

		// send the request to load the ad.
		InterstitialAd.Load(interstitialID, adRequest,
			(InterstitialAd ad, LoadAdError error) =>
			{
				// if error is not null, the load request failed.
				if (error != null || ad == null)
				{
					Debug.LogError("interstitial ad failed to load an ad " +
									"with error : " + error);
					return;
				}

				Debug.Log("Interstitial ad loaded with response : "
							+ ad.GetResponseInfo());

				interstitialAd = ad;

				///////// Callbacks
				RegisterEventHandlers(interstitialAd);
			});
	}

	/// Shows the interstitial ad.
	public void ShowAdInterstitial() {
		if (ADSEnabled) {
			if (interstitialAd != null && interstitialAd.CanShowAd())
			{
				Debug.Log("Showing interstitial ad.");
				interstitialAd.Show();
			}
			else
			{
				//	GameManager.Instance.SceneLoad(); 
				Debug.LogError("Interstitial ad is not ready yet.");
				// 	LoadInterstitialAd();
			}
		} else {
			//GameManager.Instance.SceneLoad();
		}

	}

	/// Interstitial Ads Callback
	private void RegisterEventHandlers(InterstitialAd ad)
	{
		Debug.Log("Interstitial Ads Callback Resgister");

		// Raised when the ad is estimated to have earned money.
		ad.OnAdPaid += (AdValue adValue) =>
		{
			Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
				adValue.Value,
				adValue.CurrencyCode));
		};
		// Raised when an impression is recorded for an ad.
		ad.OnAdImpressionRecorded += () =>
		{
			Debug.Log("Interstitial ad recorded an impression.");
		};
		// Raised when a click is recorded for an ad.
		ad.OnAdClicked += () =>
		{
			Debug.Log("Interstitial ad was clicked.");
		};
		// Raised when an ad opened full screen content.
		ad.OnAdFullScreenContentOpened += () =>
		{
			Debug.Log("Interstitial ad full screen content opened.");
		};
		// Raised when the ad closed full screen content.
		ad.OnAdFullScreenContentClosed += () =>
		{
			Debug.Log("Interstitial ad full screen content closed.");
			//GameManager.Instance.SceneLoad();
			// LoadInterstitialAd(); // IF DONT DESTROY LOAD TYPE CONFIG
		};
		// Raised when the ad failed to open full screen content.
		ad.OnAdFullScreenContentFailed += (AdError error) =>
		{
			Debug.LogError("Interstitial ad failed to open full screen content " +
						"with error : " + error);

			//GameManager.Instance.SceneLoad();
		};
	}

	//////////////// REWARDED METHODS

	/// Loads the rewarded ad.
	public void LoadRewardedAd()
	{
		// Clean up the old ad before loading a new one.
		if (rewardedAd != null)
		{
			rewardedAd.Destroy();
			rewardedAd = null;
		}

		Debug.Log("Loading the rewarded ad.");

		// create our request used to load the ad.
		var adRequest = new AdRequest();

		// send the request to load the ad.
		RewardedAd.Load(rewardVideoID, adRequest,
			(RewardedAd ad, LoadAdError error) =>
			{
				// if error is not null, the load request failed.
				if (error != null || ad == null)
				{
					Debug.LogError("Rewarded ad failed to load an ad " +
									"with error : " + error);
					return;
				}

				Debug.Log("Rewarded ad loaded with response : "
							+ ad.GetResponseInfo());

				rewardedAd = ad;

				/////// Callback Register
				RegisterEventHandlersRewarded(rewardedAd);
			});
	}

  	public void ShowRewardedAd()
	{
		const string rewardMsg =
			"Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

		if (rewardedAd != null && rewardedAd.CanShowAd())
		{
			rewardedAd.Show((Reward reward) =>
			{
				// TODO: Reward the user.
				Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
			});
		}
	}

	/// Rewarded Ads Callback
	private void RegisterEventHandlersRewarded(RewardedAd ad)
	{
		Debug.Log("Rewarded Ads Callback Resgister");

		// Raised when the ad is estimated to have earned money.
		ad.OnAdPaid += (AdValue adValue) =>
		{
			Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
				adValue.Value,
				adValue.CurrencyCode));
		};
		// Raised when an impression is recorded for an ad.
		ad.OnAdImpressionRecorded += () =>
		{
			Debug.Log("Rewarded ad recorded an impression.");
		};
		// Raised when a click is recorded for an ad.
		ad.OnAdClicked += () =>
		{
			Debug.Log("Rewarded ad was clicked.");
		};
		// Raised when an ad opened full screen content.
		ad.OnAdFullScreenContentOpened += () =>
		{
			Debug.Log("Rewarded ad full screen content opened.");
		};
		// Raised when the ad closed full screen content.
		ad.OnAdFullScreenContentClosed += () =>
		{
			Debug.Log("Rewarded ad full screen content closed.");
			//LoadRewardedAd(); // IF DONT DESTROY LOAD TYPE CONFIG
		};
		// Raised when the ad failed to open full screen content.
		ad.OnAdFullScreenContentFailed += (AdError error) =>
		{
			Debug.LogError("Rewarded ad failed to open full screen content " +
						"with error : " + error);
		};
	}

}