using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

public class InternetCheck : MonoBehaviour
{

    public GameObject CheckPanel;

    public string testUrl = "https://www.google.com";
    public float checkInterval = 5f;

    private bool isInternetAvailable;

    /////////////////////

    private void Start()
    {
        StartCoroutine(CheckInternetConnectionRoutine());
    }

    private IEnumerator CheckInternetConnectionRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(CheckInternetConnection());
            Debug.Log("Internet: " + (isInternetAvailable ? "YES" : "NO"));
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private IEnumerator CheckInternetConnection()
    {
        UnityWebRequest request = new UnityWebRequest(testUrl);
        request.method = UnityWebRequest.kHttpVerbHEAD;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            isInternetAvailable = true;
            CheckPanel.SetActive(false);
        }
        else
        {
            isInternetAvailable = false;
            CheckPanel.SetActive(true);
        }

        request.Dispose();
    }

    public bool IsInternetAvailable()
    {
        return isInternetAvailable;
    }

}
