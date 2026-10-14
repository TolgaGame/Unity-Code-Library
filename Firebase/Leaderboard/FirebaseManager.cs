using UnityEngine;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.Database;

public class FirebaseManager : MonoBehaviour
{
    private FirebaseApp app;
    private bool isInitialized = false;

    private DatabaseReference databaseRoot;

    private void Start()
    {
        // Android requires the Google Play services dependency check before any Firebase call
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
                InitializeFirebase();
            else
                Debug.LogError($"[FirebaseManager] Firebase could not be started: {dependencyStatus}");
        });
    }

    private void InitializeFirebase()
    {
        app = FirebaseApp.DefaultInstance;

        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

        databaseRoot = FirebaseDatabase.DefaultInstance.RootReference;

        isInitialized = true;
        Debug.Log("[FirebaseManager] Firebase successfully initialized and ready.");

        LogEvent("app_open_custom");

        Locator.Instance.GameManagerInstance.SetProfileData();
    }

    #region Analytics

    /// <summary>Logs an analytics event that has no parameters.</summary>
    public void LogEvent(string eventName)
    {
        if (!isInitialized)
            return;
        FirebaseAnalytics.LogEvent(eventName);
        Debug.Log($"[FirebaseManager] Analytics sent: {eventName}");
    }

    /// <summary>Logs an analytics event with a single string parameter.</summary>
    public void LogEventWithParameter(string eventName, string paramName, string paramValue)
    {
        if (!isInitialized) return;

        Parameter[] parameters = {
            new Parameter(paramName, paramValue)
        };

        FirebaseAnalytics.LogEvent(eventName, parameters);
        Debug.Log($"[FirebaseManager] Analytics sent: {eventName} -> {paramName}:{paramValue}");
    }

    #endregion

    #region Realtime Database - Leaderboard

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string username;
        public int score;

        public LeaderboardEntry() { }

        public LeaderboardEntry(string username, int score)
        {
            this.username = username;
            this.score = score;
        }
    }

    [Header("Leaderboard Debug View")]
    [SerializeField] private int currentPlayerRankInTopList = -1;
    [SerializeField] private List<LeaderboardEntry> currentPlayerAndNeighbours = new List<LeaderboardEntry>();
    [SerializeField] private int currentPlayerNeighbourIndex = -1;
    [SerializeField] private int currentNeighbourStartRank = -1;

    public int CurrentPlayerRankInTopList => currentPlayerRankInTopList;
    public List<LeaderboardEntry> CurrentPlayerAndNeighbours => currentPlayerAndNeighbours;
    public int CurrentPlayerNeighbourIndex => currentPlayerNeighbourIndex;
    public int CurrentNeighbourStartRank => currentNeighbourStartRank;

    private string SanitizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "Player";

        // Realtime Database keys cannot contain . $ # [ ] /
        return key
            .Replace(".", "_")
            .Replace("$", "_")
            .Replace("#", "_")
            .Replace("[", "_")
            .Replace("]", "_")
            .Replace("/", "_");
    }

    public void SaveCurrentPlayerToLeaderboard()
    {
        if (!isInitialized || databaseRoot == null)
        {
            Debug.LogWarning("[FirebaseManager] Firebase not initialized, cannot save leaderboard entry.");
            return;
        }

        if (Locator.Instance == null || Locator.Instance.GameManagerInstance == null)
        {
            Debug.LogWarning("[FirebaseManager] GameManager not found via Locator, cannot save current player to leaderboard.");
            return;
        }

        string playerName = Locator.Instance.GameManagerInstance.PlayerName;
        int playerScore = Locator.Instance.GameManagerInstance.CurrentScore;

        string safeName = SanitizeKey(playerName);
        LeaderboardEntry entry = new LeaderboardEntry(safeName, playerScore);
        string json = JsonUtility.ToJson(entry);

        DatabaseReference entryRef = databaseRoot.Child("leaderboard").Child(safeName);
        entryRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[FirebaseManager] Failed to save leaderboard entry for {safeName}: {task.Exception}");
            }
            else
            {
                Debug.Log($"[FirebaseManager] Leaderboard updated for {safeName} with score {playerScore}");
            }
        });
    }

    /// <summary>Loads the player's leaderboard entry, creating it from the local score if missing.</summary>
    public void GetOrCreatePlayerProfile(string username, int localScore, System.Action<string, int> onResult, System.Action<System.Exception> onError = null)
    {
        if (onResult == null)
            return;

        if (!isInitialized || databaseRoot == null)
        {
            Debug.LogWarning("[FirebaseManager] Firebase not initialized, using local profile data.");
            onResult(username, localScore);
            return;
        }

        string safeName = SanitizeKey(username);
        DatabaseReference entryRef = databaseRoot.Child("leaderboard").Child(safeName);

        entryRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[FirebaseManager] Failed to get player profile for {safeName}: {task.Exception}");
                onError?.Invoke(task.Exception);
                onResult(username, localScore);
                return;
            }

            if (!task.IsCompleted)
            {
                onResult(username, localScore);
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (!snapshot.Exists || !snapshot.HasChildren)
            {
                // No remote entry yet: create one from the local score
                LeaderboardEntry newEntry = new LeaderboardEntry(safeName, localScore);
                string json = JsonUtility.ToJson(newEntry);

                entryRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsFaulted)
                    {
                        Debug.LogError($"[FirebaseManager] Failed to create player profile for {safeName}: {setTask.Exception}");
                        onError?.Invoke(setTask.Exception);
                        onResult(username, localScore);
                    }
                    else
                    {
                        Debug.Log($"[FirebaseManager] Player profile created for {safeName} with score {localScore}");
                        onResult(safeName, localScore);
                    }
                });
            }
            else
            {
                // Remote entry exists: prefer its values over the local ones
                string remoteUsername = safeName;
                int remoteScore = localScore;

                if (snapshot.HasChild("username") && snapshot.Child("username").Value != null)
                {
                    remoteUsername = snapshot.Child("username").Value.ToString();
                }

                if (snapshot.HasChild("score") && snapshot.Child("score").Value != null)
                {
                    int parsed;
                    if (int.TryParse(snapshot.Child("score").Value.ToString(), out parsed))
                    {
                        remoteScore = parsed;
                    }
                }

                Debug.Log($"[FirebaseManager] Player profile loaded from DB for {safeName}: {remoteUsername} - {remoteScore}");
                onResult(remoteUsername, remoteScore);
            }
        });
    }

    /// <summary>Updates only the username field of the current player's entry; the key stays unchanged.</summary>
    public void ChangeCurrentPlayerUsername(string newUsername, System.Action<bool> onSuccess = null, System.Action<System.Exception> onError = null)
    {
        if (!isInitialized || databaseRoot == null)
        {
            Debug.LogWarning("[FirebaseManager] Firebase not initialized, cannot change username.");
            onSuccess?.Invoke(false);
            return;
        }

        if (Locator.Instance == null || Locator.Instance.GameManagerInstance == null)
        {
            Debug.LogWarning("[FirebaseManager] GameManager not found via Locator, cannot change username.");
            onSuccess?.Invoke(false);
            return;
        }

        string oldUsername = Locator.Instance.GameManagerInstance.PlayerName;
        string key = SanitizeKey(oldUsername);

        DatabaseReference entryRef = databaseRoot.Child("leaderboard").Child(key);
        entryRef.Child("username").SetValueAsync(newUsername).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[FirebaseManager] Failed to update username for key '{key}': {task.Exception}");
                onError?.Invoke(task.Exception);
                onSuccess?.Invoke(false);
            }
            else
            {
                Locator.Instance.SaveSystemInstance.SavePlayerName(newUsername);
                Debug.Log($"[FirebaseManager] Username updated for existing key '{key}': {oldUsername} -> {newUsername}");
                onSuccess?.Invoke(true);
            }
        });
    }

    /// <summary>Reads the highest <paramref name="limit"/> scores; children arrive in ascending score order.</summary>
    public void GetTopLeaderboard(int limit, System.Action<DataSnapshot> onSuccess, System.Action<System.Exception> onError = null)
    {
        if (!isInitialized || databaseRoot == null)
        {
            Debug.LogWarning("[FirebaseManager] Firebase not initialized, cannot read leaderboard.");
            return;
        }

        databaseRoot
            .Child("leaderboard")
            .OrderByChild("score")
            .LimitToLast(limit)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[FirebaseManager] Failed to read leaderboard: {task.Exception}");
                    onError?.Invoke(task.Exception);
                }
                else if (task.IsCompleted)
                {
                    onSuccess?.Invoke(task.Result);
                }
            });
    }

    /// <summary>Calculates the current player's rank and neighbours over the full leaderboard.</summary>
    public void RefreshCurrentPlayerRankAndNeighbours(int queryLimit = 3000, System.Action onComplete = null)
    {
        if (!isInitialized || databaseRoot == null)
        {
            Debug.LogWarning("[FirebaseManager] Firebase not initialized, cannot calculate player rank.");
            onComplete?.Invoke();
            return;
        }

        if (Locator.Instance == null || Locator.Instance.GameManagerInstance == null)
        {
            Debug.LogWarning("[FirebaseManager] GameManager not found via Locator, cannot calculate player rank.");
            onComplete?.Invoke();
            return;
        }

        string playerName = Locator.Instance.GameManagerInstance.PlayerName;
        string safeName = SanitizeKey(playerName);

        // Read the full leaderboard so the player can be ranked even when far from the top
        databaseRoot
            .Child("leaderboard")
            .OrderByChild("score")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[FirebaseManager] Error while calculating player rank: {task.Exception}");
                    onComplete?.Invoke();
                    return;
                }

                if (!task.IsCompleted)
                {
                    onComplete?.Invoke();
                    return;
                }

                DataSnapshot snapshot = task.Result;

                List<LeaderboardEntry> allEntries = new List<LeaderboardEntry>();
                currentPlayerNeighbourIndex = -1;
                currentNeighbourStartRank = -1;

                foreach (var child in snapshot.Children)
                {
                    string entryUsername = child.Child("username")?.Value != null
                        ? child.Child("username").Value.ToString()
                        : child.Key;

                    int entryScore = 0;
                    if (child.Child("score")?.Value != null)
                    {
                        int.TryParse(child.Child("score").Value.ToString(), out entryScore);
                    }

                    allEntries.Add(new LeaderboardEntry(entryUsername, entryScore));
                }

                // Highest score first (rank 1)
                allEntries.Sort((a, b) => b.score.CompareTo(a.score));

                int index = allEntries.FindIndex(e => e.username == safeName);
                if (index < 0)
                {
                    // The stored username may differ from the sanitized key
                    index = allEntries.FindIndex(e => SanitizeKey(e.username) == safeName);
                }

                if (index < 0)
                {
                    Debug.LogWarning($"[FirebaseManager] Current player '{safeName}' not found in full leaderboard ({allEntries.Count} entries).");
                    currentPlayerRankInTopList = -1;
                    currentPlayerAndNeighbours.Clear();
                    currentPlayerNeighbourIndex = -1;
                    currentNeighbourStartRank = -1;
                    onComplete?.Invoke();
                    return;
                }

                int total = allEntries.Count;
                currentPlayerRankInTopList = index + 1; // rank is 1-based

                int startIndex = Mathf.Max(0, index - 5);
                int endIndex = Mathf.Min(total - 1, index + 5);

                currentPlayerAndNeighbours.Clear();
                for (int i = startIndex; i <= endIndex; i++)
                {
                    currentPlayerAndNeighbours.Add(allEntries[i]);
                }

                currentPlayerNeighbourIndex = index - startIndex;
                if (currentPlayerNeighbourIndex < 0 || currentPlayerNeighbourIndex >= currentPlayerAndNeighbours.Count)
                {
                    currentPlayerNeighbourIndex = -1;
                }

                currentNeighbourStartRank = startIndex + 1;

                onComplete?.Invoke();
            });
    }

    #endregion
}