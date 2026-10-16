using UnityEngine;

/// <summary>
/// Stands still and waits. Leaves when it sees the target or when the wait is over.
/// </summary>
public class EnemyIdleState : IState
{
    private readonly EnemyAI _enemy;
    private float _timer;

    public EnemyIdleState(EnemyAI enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _timer = 0f; // reset here, not in the constructor
    }

    public void Tick()
    {
        if (_enemy.CanSeeTarget)
        {
            _enemy.Machine.ChangeState(_enemy.ChaseState);
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= _enemy.IdleDuration && _enemy.HasWaypoints)
            _enemy.Machine.ChangeState(_enemy.PatrolState);
    }

    public void Exit() { }
}
