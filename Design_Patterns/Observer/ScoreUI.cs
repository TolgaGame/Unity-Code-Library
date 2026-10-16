using TMPro;
using UnityEngine;

/// <summary>
/// Classic interface-based Observer: OBSERVER #1.
/// Shows the score on screen. ScoreManager never knows this class exists.
/// </summary>
public class ScoreUI : MonoBehaviour, IEventObserver<int>
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private void OnEnable()
    {
        var manager = ScoreManager.Instance;
        if (manager == null)
            return;

        manager.ScoreChanged.Subscribe(this);

        // Show the current value right away instead of waiting for the next change.
        OnNotify(manager.Score);
    }

    private void OnDisable()
    {
        // Always unsubscribe, otherwise the subject keeps a reference to a disabled object.
        var manager = ScoreManager.Instance;
        if (manager != null)
            manager.ScoreChanged.Unsubscribe(this);
    }

    public void OnNotify(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }
}
