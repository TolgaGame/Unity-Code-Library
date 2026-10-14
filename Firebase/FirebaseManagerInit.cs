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

    [HideInInspector] public long RemoteFailAdCount;
    [HideInInspector] public bool RemoteGameDifficulty;

    private void Start()
    {
        // Android requires the Google Play services dependency check before any Firebase call
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
                InitializeFirebase();
            else
                Debug.LogError($"[FirebaseManagerInit] Firebase could not be started: {dependencyStatus}");
        });
    }

    private void InitializeFirebase()
    {
        app = FirebaseApp.DefaultInstance;

        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

        isInitialized = true;
        Debug.Log("[FirebaseManagerInit] Firebase successfully initialized and ready.");

        InitializeRemoteConfig();

        LogEvent("app_open_custom");
    }

    /// <summary>Logs an analytics event that has no parameters.</summary>
    public void LogEvent(string eventName)
    {
        if (!isInitialized)
            return;
        FirebaseAnalytics.LogEvent(eventName);
        Debug.Log($"[FirebaseManagerInit] Analytics sent: {eventName}");
    }

    /// <summary>Logs an analytics event with a single string parameter.</summary>
    public void LogEventWithParameter(string eventName, string paramName, string paramValue)
    {
        if (!isInitialized) return;

        Parameter[] parameters = {
            new Parameter(paramName, paramValue)
        };

        FirebaseAnalytics.LogEvent(eventName, parameters);
        Debug.Log($"[FirebaseManagerInit] Analytics sent: {eventName} -> {paramName}:{paramValue}");
    }

    private void InitializeRemoteConfig()
    {
        // Defaults are used when the device is offline
        Dictionary<string, object> defaults = new Dictionary<string, object>();
        defaults.Add("game_difficulty", "normal");
        defaults.Add("daily_reward_coins", 100);
        defaults.Add("show_ads", true);

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log("[FirebaseManagerInit] Remote Config: defaults set.");
                FetchRemoteData();
            });
    }

    private void FetchRemoteData()
    {
        // A zero timeout forces an immediate refresh during development.
        // Use a long interval (e.g. 12 hours = 43200 s) in production.
        TimeSpan fetchTimeout = TimeSpan.Zero;

        FirebaseRemoteConfig.DefaultInstance.FetchAsync(fetchTimeout)
            .ContinueWithOnMainThread(fetchTask =>
            {
                if (fetchTask.IsCanceled || fetchTask.IsFaulted)
                {
                    Debug.LogError("[FirebaseManagerInit] Remote Config: data could not be fetched.");
                    return;
                }

                FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                    .ContinueWithOnMainThread(activateTask =>
                    {
                        Debug.Log($"[FirebaseManagerInit] Remote Config: latest data fetched and activated. Result: {activateTask.Result}");

                        RemoteFailAdCount = GetRemoteLong("fail_ad_count");

                        RemoteGameDifficulty = GetRemoteBool("hard_mode");
                    });
            });
    }

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
