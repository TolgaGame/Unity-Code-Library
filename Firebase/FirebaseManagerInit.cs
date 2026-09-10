using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using System; 
using System.Collections.Generic; 

public class FirebaseManagerInit : MonoBehaviour
{

    private FirebaseApp app;
    private bool isInitialized = false;

    // Remote Config Values

    [HideInInspector] public long RemoteFailAdCount;
    [HideInInspector] public bool RemoteGameDifficulty;

    /////////////////////////////////////
  
    private void Start()
    {
        // 1. First, check dependencies (critical for Android)
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
                InitializeFirebase();
            else
                Debug.LogError($"Firebase could not be started: {dependencyStatus}");
        });

    }

    private void InitializeFirebase()
    {
        app = FirebaseApp.DefaultInstance;
        
        // Enable Analytics
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        
        isInitialized = true;
        Debug.Log("Firebase successfully initialized and ready!");

        InitializeRemoteConfig();

        // Example: Send app open event
        LogEvent("app_open_custom");
    }

    // ---------------------------------------------------------
    //  ANALYTICS METHODS
    // ---------------------------------------------------------

    /// Sends only the event name (e.g., "level_start")
    public void LogEvent(string eventName)
    {
        if (!isInitialized) 
            return;
        FirebaseAnalytics.LogEvent(eventName);
        Debug.Log($"Analytics Sent: {eventName}");
    }
    
    /// Sends an event with a string parameter (e.g., "level_end", "level_name", "Forest_1")
    public void LogEventWithParameter(string eventName, string paramName, string paramValue)
    {
        if (!isInitialized) return;
        
        Parameter[] parameters = {
            new Parameter(paramName, paramValue)
        };

        FirebaseAnalytics.LogEvent(eventName, parameters);
        Debug.Log($"Analytics Sent: {eventName} -> {paramName}:{paramValue}");
    }

    
    // ---------------------------------------------------------
    //  REMOTE CONFIG METHODS
    // ---------------------------------------------------------

    // Remote Config Initialization
    private void InitializeRemoteConfig()
    {
        // 1. Set default values (used if there is no internet)
        Dictionary<string, object> defaults = new Dictionary<string, object>();
        defaults.Add("game_difficulty", "normal");
        defaults.Add("daily_reward_coins", 100);
        defaults.Add("show_ads", true);

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(task =>
            {
                // Defaults loaded, now fetch latest data from Cloud
                Debug.Log("Remote Config: Defaults set.");
                FetchRemoteData();
            });
    }

    // Fetch data from Cloud (Fetch & Activate)
    private void FetchRemoteData()
    {
        // In development, set duration to 0 (for instant updates)
        // For production, set this to 12 hours (43200 sec)
        TimeSpan fetchTimeout = TimeSpan.Zero; 
        
        FirebaseRemoteConfig.DefaultInstance.FetchAsync(fetchTimeout)
            .ContinueWithOnMainThread(fetchTask =>
            {
                if (fetchTask.IsCanceled || fetchTask.IsFaulted)
                {
                    Debug.LogError("Remote Config: Data could not be fetched/Error.");
                    return;
                }

                // Çekme başarılı, şimdi veriyi aktifleştir (Activate)
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                    .ContinueWithOnMainThread(activateTask =>
                    {
                        Debug.Log($"Remote Config: Latest data fetched and activated. Result: {activateTask.Result}");
                                // Get the remote value from FirebaseManager (default to 3 if not set)

                        RemoteFailAdCount = GetRemoteLong("fail_ad_count");
                      //  Debug.Log("Remote fail_ad_count value: " + RemoteFailAdCount);

                        RemoteGameDifficulty = GetRemoteBool("hard_mode");
                     //   Debug.Log("Remote game_difficulty value: " + RemoteGameDifficulty);
                    });
            });
    }

    // ---------------------------------------------------------
    // DATA READING METHODS (Call these from your game)
    // ---------------------------------------------------------

    public long GetRemoteLong(string key)
    {
        return FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;
    }

    public string GetRemoteString(string key)
    {
        return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
    }

    public bool GetRemoteBool(string key)
    {
        return FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
    }

}
