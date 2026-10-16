using UnityEngine;

/// <summary>
/// Classic interface-based Observer: the SUBJECT.
/// Holds the score and notifies every subscribed IEventObserver&lt;int&gt; when it changes.
/// Requires the Singleton&lt;T&gt; from the Utilities folder.
/// </summary>
public class ScoreManager : Singleton<ScoreManager>
{
    private readonly Subject<int> _scoreSubject = new Subject<int>();

    /// <summary>Observers subscribe here. They can listen, but they cannot raise notifications.</summary>
    public IEventSubject<int> ScoreChanged => _scoreSubject;

    public int Score { get; private set; }

    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        Score += amount;
        Debug.Log($"[ScoreManager] Score changed: {Score}");
        _scoreSubject.Notify(Score);
    }

    public void ResetScore()
    {
        Score = 0;
        _scoreSubject.Notify(Score);
    }

    private void OnDestroy()
    {
        _scoreSubject.Clear();
    }
}
