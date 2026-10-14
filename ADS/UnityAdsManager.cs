using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Serialization;

public class UnityAdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Unity Ads Settings")]
    [SerializeField] string _androidGameId;
    [SerializeField] string _iOSGameId;
    [SerializeField] bool _testMode = true;
    private string _gameId;

    [Space]
    [Header("Interstitial Ad Settings")]
    [SerializeField] string _androidAdUnitId = "Interstitial_Android";
    [SerializeField] string _iOSAdUnitId = "Interstitial_iOS";
    string _adUnitId;

    [Space]
    [Header("Banner Ad Settings")]
    [SerializeField] BannerPosition _bannerPosition = BannerPosition.BOTTOM_CENTER;
    [FormerlySerializedAs("_androidAdUnitIdBANNER")]
    [SerializeField] string _androidBannerAdUnitId = "Banner_Android";
    [FormerlySerializedAs("_iOSAdUnitIdBANNER")]
    [SerializeField] string _iOSBannerAdUnitId = "Banner_iOS";
    string _bannerAdUnitId = null; // Stays null on unsupported platforms.

    private void Awake()
    {
        InitializeAds();

        _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSAdUnitId
            : _androidAdUnitId;
    }

    private void Start()
    {
        #if UNITY_IOS
            _bannerAdUnitId = _iOSAdUnitId;
        #elif UNITY_ANDROID
            _bannerAdUnitId = _androidAdUnitId;
        #endif

        Advertisement.Banner.SetPosition(_bannerPosition);
    }

    public void InitializeAds()
    {
        #if UNITY_IOS
            _gameId = _iOSGameId;
        #elif UNITY_ANDROID
            _gameId = _androidGameId;
        #elif UNITY_EDITOR
            _gameId = _androidGameId; // Lets the flow be tested in the Editor.
        #endif

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(_gameId, _testMode, this);
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("[UnityAdsManager] Unity Ads initialization complete.");
        LoadAd();
        LoadBanner();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"[UnityAdsManager] Unity Ads initialization failed: {error.ToString()} - {message}");
    }

    public void LoadAd()
    {
        // Ads must only be loaded after initialization has completed.
        Debug.Log("[UnityAdsManager] Loading ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void ShowAd()
    {
        // Fails if the ad content was not loaded beforehand.
        Debug.Log("[UnityAdsManager] Showing ad: " + _adUnitId);
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log($"[UnityAdsManager] Ad unit loaded: {adUnitId}");
    }

    public void OnUnityAdsFailedToLoad(string _adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"[UnityAdsManager] Error loading ad unit: {_adUnitId} - {error.ToString()} - {message}");
    }

    public void OnUnityAdsShowFailure(string _adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"[UnityAdsManager] Error showing ad unit {_adUnitId}: {error.ToString()} - {message}");
    }

    public void OnUnityAdsShowStart(string _adUnitId) { }
    public void OnUnityAdsShowClick(string _adUnitId) { }
    public void OnUnityAdsShowComplete(string _adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        GameManager.Instance.SceneLoad();
    }

    public void LoadBanner()
    {
        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        Advertisement.Banner.Load(_bannerAdUnitId, options);
    }

    void OnBannerLoaded()
    {
        Debug.Log("[UnityAdsManager] Banner loaded");
        ShowBannerAd();
    }

    void OnBannerError(string message)
    {
        Debug.Log($"[UnityAdsManager] Banner error: {message}");
    }

    private void ShowBannerAd()
    {
        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        Advertisement.Banner.Show(_bannerAdUnitId, options);
    }

    private void HideBannerAd()
    {
        Advertisement.Banner.Hide();
    }

    void OnBannerClicked() { }
    void OnBannerShown() { }
    void OnBannerHidden() { }
}