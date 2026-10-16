using UnityEngine;

/// <summary>
/// Walks to the next waypoint, then goes back to Idle for a short rest.
/// Keeps its own waypoint index, so the next patrol continues where the last one ended.
/// </summary>
public class EnemyPatrolState : IState
{
    private const float ArriveDistance = 0.1f;

    private readonly EnemyAI _enemy;
    private int _index;

    public EnemyPatrolState(EnemyAI enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        if (_enemy.HasWaypoints)
            _index %= _enemy.WaypointCount; // stay valid if the waypoint list changed
    }

    public void Tick()
    {
        if (_enemy.CanSeeTarget)
        {
            _enemy.Machine.ChangeState(_enemy.ChaseState);
            return;
        }

        if (!_enemy.HasWaypoints)
        {
            _enemy.Machine.ChangeState(_enemy.IdleState);
            return;
        }

        Vector3 waypoint = _enemy.GetWaypoint(_index);
        _enemy.MoveTowards(waypoint, _enemy.PatrolSpeed);

        if (_enemy.FlatDistanceTo(waypoint) <= ArriveDistance)
        {
            _index = (_index + 1) % _enemy.WaypointCount;
            _enemy.Machine.ChangeState(_enemy.IdleState);
        }
    }

    public void Exit() { }
}
