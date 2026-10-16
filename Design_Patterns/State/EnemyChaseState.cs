/// <summary>
/// Runs towards the target. Attacks when close enough, gives up when the target is too far.
/// </summary>
public class EnemyChaseState : IState
{
    private readonly EnemyAI _enemy;

    public EnemyChaseState(EnemyAI enemy)
    {
        _enemy = enemy;
    }

    public void Enter() { }

    public void Tick()
    {
        if (_enemy.HasLostTarget)
        {
            _enemy.Machine.ChangeState(_enemy.IdleState);
            return;
        }

        if (_enemy.InAttackRange)
        {
            _enemy.Machine.ChangeState(_enemy.AttackState);
            return;
        }

        _enemy.MoveTowards(_enemy.Target.position, _enemy.ChaseSpeed);
    }

    public void Exit() { }
}
