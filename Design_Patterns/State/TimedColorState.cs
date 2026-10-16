using UnityEngine;

/// <summary>
/// A data driven state: shows a color for a duration, then moves to the next state asset.
/// Chain three of them (Green, Yellow, Red) and you have a traffic light with no code changes.
/// </summary>
[CreateAssetMenu(menuName = "Design Patterns/Timed Color State", fileName = "NewTimedColorState")]
public class TimedColorState : ScriptableState
{
    [SerializeField] private Color color = Color.green;
    [SerializeField, Min(0.1f)] private float duration = 3f;
    [SerializeField] private ScriptableState nextState;

    public override void OnEnter(ScriptableStateRunner runner)
    {
        if (runner.Renderer != null)
            runner.Renderer.material.color = color;
    }

    public override void OnTick(ScriptableStateRunner runner)
    {
        if (runner.TimeInState >= duration)
            runner.ChangeState(nextState);
    }
}
