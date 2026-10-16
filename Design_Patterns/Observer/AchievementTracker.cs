using System;
using UnityEngine;

/// <summary>
/// Classic interface-based Observer: OBSERVER #2.
/// Unlocks achievements when the score passes a threshold.
/// Shows that one subject can drive many unrelated observers without any change to the subject.
/// </summary>
public class AchievementTracker : MonoBehaviour, IEventObserver<int>
{
    [Tooltip("Score values that unlock an achievement, in ascending order.")]
    [SerializeField] private int[] thresholds = { 100, 500, 1000 };

    /// <summary>Fires with the threshold that was just reached.</summary>
    public event Action<int> OnAchievementUnlocked;

    private int _nextIndex;

    private void OnEnable()
    {
        var manager = ScoreManager.Instance;
        if (manager != null)
            manager.ScoreChanged.Subscribe(this);
    }

    private void OnDisable()
    {
        var manager = ScoreManager.Instance;
        if (manager != null)
            manager.ScoreChanged.Unsubscribe(this);
    }

    public void OnNotify(int score)
    {
        // A single big jump can pass more than one threshold at once.
        while (_nextIndex < thresholds.Length && score >= thresholds[_nextIndex])
        {
            int reached = thresholds[_nextIndex];
            _nextIndex++;

            Debug.Log($"[AchievementTracker] Achievement unlocked: {reached} points");
            OnAchievementUnlocked?.Invoke(reached);
        }
    }
}
