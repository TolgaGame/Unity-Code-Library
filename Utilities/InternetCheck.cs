using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InternetCheck : MonoBehaviour
{
    [Tooltip("Shown while there is no internet connection. Optional.")]
    public GameObject CheckPanel;

    public string testUrl = "https://www.google.com";
    public float checkInterval = 5f;

    [Tooltip("Seconds to wait for the test request before counting it as offline.")]
    public int requestTimeout = 5;

    /// <summary>Raised only when the connection state changes (true = online).</summary>
    public event Action<bool> OnConnectionChanged;

    private bool isInternetAvailable;
    private bool hasResult;

    private void Start()
    {
        StartCoroutine(CheckInternetConnectionRoutine());
    }

    private IEnumerator CheckInternetConnectionRoutine()
    {
        while (true)
        {
            yield return CheckInternetConnection();
            // Realtime keeps checking even when the game is paused with Time.timeScale = 0.
            yield return new WaitForSecondsRealtime(checkInterval);
        }
    }

    private IEnumerator CheckInternetConnection()
    {
        bool online;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            online = false;
        }
        else
        {
            using (UnityWebRequest request = UnityWebRequest.Head(testUrl))
            {
                request.timeout = requestTimeout;
                yield return request.SendWebRequest();
                online = request.result == UnityWebRequest.Result.Success;
            }
        }

        SetState(online);
    }

    private void SetState(bool online)
    {
        if (CheckPanel != null)
        {
            CheckPanel.SetActive(!online);
        }

        if (hasResult && online == isInternetAvailable)
            return;

        hasResult = true;
        isInternetAvailable = online;
        Debug.Log("[InternetCheck] Internet: " + (online ? "YES" : "NO"));
        OnConnectionChanged?.Invoke(online);
    }

    public bool IsInternetAvailable()
    {
        return isInternetAvailable;
    }
}
