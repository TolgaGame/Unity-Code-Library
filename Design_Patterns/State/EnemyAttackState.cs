using UnityEngine;

/// <summary>
/// Faces the target and attacks on a cooldown. Goes back to Chase when the target moves away.
/// </summary>
public class EnemyAttackState : IState
{
    private readonly EnemyAI _enemy;
    private float _cooldown;

    public EnemyAttackState(EnemyAI enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _cooldown = 0f; // first hit lands immediately
    }

    public void Tick()
    {
        if (_enemy.OutOfAttackRange)
        {
            _enemy.Machine.ChangeState(_enemy.ChaseState);
            return;
        }

        _enemy.FaceTarget();

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        _cooldown = _enemy.AttackCooldown;

        // Replace with your real damage logic, e.g. target.GetComponent<PlayerHealth>().TakeDamage(10);
        Debug.Log($"{_enemy.name} attacks {_enemy.Target.name}!", _enemy);
    }

    public void Exit() { }
}
