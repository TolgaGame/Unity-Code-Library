using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ColorCraze.Game.Common;
using ColorCraze.Core;

[Serializable]
public class DailyTask
{
    public string id;
    public string description;
    public bool isCompleted;
    public bool isClaimed;
}

//////////////////////////////////

[Serializable]
public class TaskUIRow
{
    [Header("UI Refs")]
    public Text descriptionText;
    public Image completedIcon;
    public Sprite completedSprite;
    public Sprite notCompletedSprite;
    public Sprite rewardSprite;
    public string rewardText;
    public Button claimButton;

    public void Bind(DailyTask data, Action onClaim)
    {
        if (descriptionText)
            descriptionText.text = (data.isClaimed && !string.IsNullOrEmpty(rewardText)) ? rewardText : (data.description ?? string.Empty);

        if (completedIcon)
        {
            if (data.isClaimed && rewardSprite)
                completedIcon.sprite = rewardSprite;
            else
                completedIcon.sprite = data.isCompleted ? completedSprite : notCompletedSprite;
        }

        if (claimButton)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.interactable = data.isCompleted && !data.isClaimed;
            claimButton.onClick.AddListener(() => onClaim?.Invoke());
            claimButton.gameObject.SetActive(true);
        }
    }

    public void Refresh(DailyTask data)
    {
        if (descriptionText)
            descriptionText.text = (data.isClaimed && !string.IsNullOrEmpty(rewardText)) ? rewardText : (data.description ?? string.Empty);

        if (completedIcon)
        {
            if (data.isClaimed && rewardSprite)
                completedIcon.sprite = rewardSprite;
            else
                completedIcon.sprite = data.isCompleted ? completedSprite : notCompletedSprite;
        }
       
        if (claimButton)
        {
            claimButton.interactable = data.isCompleted && !data.isClaimed;
            claimButton.gameObject.SetActive(true);
        }
    }

}

/////////////////////////////////////

public class DailyTaskManager : MonoBehaviour
{
    [Header("Popup Root")]
    public GameObject popupRoot;

    [Header("Countdown")]
    public Text countdownText;

    [Header("Task Rows")]
    public TaskUIRow[] taskRows = new TaskUIRow[4];

    [Header("Task Config")]
    [TextArea]
    public string[] defaultDescriptions = new[]
    {
        "Complete 3 levels in a row",
        "Break 10 icy cells",
        "Collect 50 red drops",
        "Use 3 different powerups in a single level"
    };

    [Header("Task Reward Desc")]
    [TextArea]
    public string[] rewardtDescriptions = new[]
    {
        "Reward: 500 Coins",
        "Reward: 1 Brush PowerUp",
        "Reward: 1 Bomb PowerUp",
        "Reward: 1 Color Bomb PowerUp"
    };

    /////////////////////////

    private readonly List<DailyTask> _tasks = new List<DailyTask>(4);
    private Coroutine _countdownRoutine;
    private DateTime _lastResetUtc;
    private const string PlayerPrefKeyLastReset = "DailyTasks_LastResetUtc";

    private string TaskCompletedKey(int i) => $"DailyTasks_Task_{i}_Completed";
    private string TaskClaimedKey(int i) => $"DailyTasks_Task_{i}_Claimed";

    ///////////////////////////////////////////////

    #region Unity Callbacks

    private void Awake()
    {
        EnsureFourRows();
        LoadOrInitialize();
        BindUI();
    }

    private void OnEnable()
    {
        StartCountdown();
        CheckTaskStatus();
    }

    private void OnDisable()
    {
        StopCountdown();
    }

    public void OpenPopup()
    {
        if (popupRoot) popupRoot.SetActive(true);
        RefreshAll();
        StartCountdown();
    }

    public void ClosePopup()
    {
        if (popupRoot) popupRoot.SetActive(false);
        StopCountdown();
    }

    #endregion

    #region Initialization and Saving

    private void EnsureFourRows()
    {
        if (taskRows == null || taskRows.Length != 4)
            taskRows = new TaskUIRow[4];
    }

    private void LoadOrInitialize()
    {
        if (PlayerPrefs.HasKey(PlayerPrefKeyLastReset))
        {
            var ticksStr = PlayerPrefs.GetString(PlayerPrefKeyLastReset, string.Empty);
            if (long.TryParse(ticksStr, out var ticks))
                _lastResetUtc = new DateTime(ticks, DateTimeKind.Utc);
            else
                _lastResetUtc = DateTime.UtcNow;
        }
        else
        {
            _lastResetUtc = DateTime.UtcNow;
            PlayerPrefs.SetString(PlayerPrefKeyLastReset, _lastResetUtc.Ticks.ToString());
        }

        if (DateTime.UtcNow - _lastResetUtc >= TimeSpan.FromHours(24))
        {
            HardResetTasks();
        }

        _tasks.Clear();
        for (int i = 0; i < 4; i++)
        {
            var t = new DailyTask
            {
                id = $"task_{i}",
                description = (defaultDescriptions != null && i < defaultDescriptions.Length)
                    ? defaultDescriptions[i]
                    : $"Task {i + 1}",
                isCompleted = PlayerPrefs.GetInt(TaskCompletedKey(i), 0) == 1,
                isClaimed = PlayerPrefs.GetInt(TaskClaimedKey(i), 0) == 1,
            };
            _tasks.Add(t);
        }
    }

    private void Save()
    {
        for (int i = 0; i < _tasks.Count; i++)
        {
            var t = _tasks[i];
            PlayerPrefs.SetInt(TaskCompletedKey(i), t.isCompleted ? 1 : 0);
            PlayerPrefs.SetInt(TaskClaimedKey(i), t.isClaimed ? 1 : 0);
        }
        PlayerPrefs.SetString(PlayerPrefKeyLastReset, _lastResetUtc.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private void BindUI()
    {
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var row = taskRows != null && i < taskRows.Length ? taskRows[i] : null;
            if (row == null) continue;
            if (rewardtDescriptions != null && idx < rewardtDescriptions.Length)
                row.rewardText = rewardtDescriptions[idx];

            row.Bind(
                _tasks[idx],
                onClaim: () => ClaimTask(idx)
            );
        }
        RefreshAll();
    }

    private void RefreshAll()
    {
        for (int i = 0; i < 4; i++)
        {
            var row = taskRows != null && i < taskRows.Length ? taskRows[i] : null;
            if (row == null) continue;
            if (i < _tasks.Count)
                row.Refresh(_tasks[i]);
        }
        UpdateCountdownText();
    }

    #endregion

    #region Countdown

    private void StartCountdown()
    {
        if (_countdownRoutine != null) return;
        _countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private void StopCountdown()
    {
        if (_countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }
    }

    private IEnumerator CountdownRoutine()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            var timeSince = DateTime.UtcNow - _lastResetUtc;
            var toNext = TimeSpan.FromHours(24) - timeSince;
            if (toNext <= TimeSpan.Zero)
            {
                HardResetTasks();
                RefreshAll();
            }
            else
            {
                UpdateCountdownText(toNext);
            }

            yield return wait;
        }
    }

    private void UpdateCountdownText()
    {
        var timeSince = DateTime.UtcNow - _lastResetUtc;
        var toNext = TimeSpan.FromHours(24) - timeSince;
        UpdateCountdownText(toNext);
    }

    private void UpdateCountdownText(TimeSpan toNext)
    {
        if (!countdownText) return;
        if (toNext < TimeSpan.Zero) toNext = TimeSpan.Zero;
        countdownText.text = $"{toNext.Hours:00}H:{toNext.Minutes:00}M";
    }

    #endregion

    #region Task Management

    private void HardResetTasks()
    {
        _lastResetUtc = DateTime.UtcNow;
        for (int i = 0; i < 4; i++)
        {
            DailyTask t;
            if (i < _tasks.Count)
            {
                t = _tasks[i];
                t.isCompleted = false;
                t.isClaimed = false;
            }
            else
            {
                t = new DailyTask
                {
                    id = $"task_{i}",
                    description = (defaultDescriptions != null && i < defaultDescriptions.Length)
                        ? defaultDescriptions[i]
                        : $"Task {i + 1}",
                    isCompleted = false,
                    isClaimed = false
                };
                _tasks.Add(t);
            }
        }
        PlayerPrefs.SetInt("TaskCompleted1", 0);
        PlayerPrefs.SetInt("TaskCompleted2", 0);
        PlayerPrefs.SetInt("TaskCompleted3", 0);
        PlayerPrefs.SetInt("TaskCompleted4", 0);
        Save();
    }

    private void CheckTaskStatus()
    {
        int task1Completed = PlayerPrefs.GetInt("TaskCompleted1");
        int task2Completed = PlayerPrefs.GetInt("TaskCompleted2");
        int task3Completed = PlayerPrefs.GetInt("TaskCompleted3");
        int task4Completed = PlayerPrefs.GetInt("TaskCompleted4");

        if (task1Completed == 1)
            SetTaskCompleted(0, true);
        if (task2Completed == 1)
            SetTaskCompleted(1, true);
        if (task3Completed == 1)
            SetTaskCompleted(2, true);
        if (task4Completed == 1)
            SetTaskCompleted(3, true);

    }

    #endregion

    #region Reward Claiming

    private void ClaimTask(int index)
    {
        if (index < 0 || index >= _tasks.Count)
            return;

        var t = _tasks[index];

        if (!t.isCompleted || t.isClaimed)
            return;

        AwardReward(index);

        t.isClaimed = true;
        Save();
        if (taskRows != null && index < taskRows.Length && taskRows[index] != null)
        {
            taskRows[index].Refresh(t);
            var row = taskRows[index];
            if (row.descriptionText && !string.IsNullOrEmpty(row.rewardText))
                row.descriptionText.text = row.rewardText;
            if (row.completedIcon && row.rewardSprite)
                row.completedIcon.sprite = row.rewardSprite;
        }
    }

    private void AwardReward(int index)
    {
        Debug.Log($"DailyTaskManager: Reward granted for task {index}.");
        if (index == 0)
        {
            PuzzleMatchManager.instance.coinsSystem.BuyCoins(500);
            Debug.Log("Reward: 500 Coins");
        }
        else if (index == 1)
        {
            var playerPrefsKey = "num_boosters_0";
            int currentValue = PuzzleMatchManager.instance.gameConfig.ingameBoosterAmount[BoosterType.PaintBrush] - 2;
            int rewardValue = currentValue;

            PlayerPrefs.SetInt(playerPrefsKey, rewardValue);
            Debug.Log("Reward: 1 Brush PowerUp");
        }
        else if (index == 2)
        {
            var playerPrefsKey = "num_boosters_3";
            int currentValue = PuzzleMatchManager.instance.gameConfig.ingameBoosterAmount[BoosterType.Bomb];
            int rewardValue = currentValue;

            PlayerPrefs.SetInt(playerPrefsKey, rewardValue);
            Debug.Log("Reward: 1 bomb PowerUp");
        }
        else if (index == 3)
        {
            var playerPrefsKey = "num_boosters_3";
            int currentValue = PuzzleMatchManager.instance.gameConfig.ingameBoosterAmount[BoosterType.ColorBomb];
            int rewardValue = currentValue;

            PlayerPrefs.SetInt(playerPrefsKey, rewardValue);
            Debug.Log("Reward: 1 color bomb PowerUp");
        }
        SoundManager.instance.PlaySound("AwardPopup");
    }

    public void SetTaskCompleted(int index, bool completed)
    {
        if (index < 0 || index >= _tasks.Count)
            return;

        var t = _tasks[index];

        if (t.isClaimed)
            return;

        t.isCompleted = completed;

        Save();

        if (taskRows != null && index < taskRows.Length && taskRows[index] != null)
        {
            taskRows[index].Refresh(t);
        }

        BindUI();
    }

    #endregion

}