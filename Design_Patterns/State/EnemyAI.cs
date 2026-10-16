using UnityEngine;

/// <summary>
/// CONTEXT of the State pattern (interface based version).
/// Owns the StateMachine, the shared data (target, speeds, ranges) and small helpers.
/// It contains no behavior logic itself; the states do.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform[] waypoints;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float idleDuration = 2f;

    [Header("Senses")]
    [Tooltip("Idle and Patrol switch to Chase when the target is closer than this.")]
    [SerializeField] private float sightRange = 8f;
    [Tooltip("Chase gives up when the target is farther than this. Keep it larger than Sight Range to avoid flickering.")]
    [SerializeField] private float loseSightRange = 10f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.5f;
    [Tooltip("Attack only ends when the target is farther than Attack Range + this buffer.")]
    [SerializeField] private float attackExitBuffer = 0.5f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    // The machine and the cached states. States are created once, never per transition.
    public StateMachine Machine { get; private set; }
    public EnemyIdleState IdleState { get; private set; }
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }

    // Shared data exposed to the states
    public Transform Target => target;
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float IdleDuration => idleDuration;
    public float AttackCooldown => attackCooldown;

    public bool HasWaypoints => waypoints != null && waypoints.Length > 0;
    public int WaypointCount => HasWaypoints ? waypoints.Length : 0;
    public Vector3 GetWaypoint(int index) => waypoints[index].position;

    // Conditions the states use to decide transitions
    public float DistanceToTarget => target == null ? float.MaxValue : FlatDistanceTo(target.position);
    public bool CanSeeTarget => DistanceToTarget <= sightRange;
    public bool HasLostTarget => DistanceToTarget > loseSightRange;
    public bool InAttackRange => DistanceToTarget <= attackRange;
    public bool OutOfAttackRange => DistanceToTarget > attackRange + attackExitBuffer;

    private void Awake()
    {
        Machine = new StateMachine();

        IdleState = new EnemyIdleState(this);
        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
    }

    private void OnEnable()
    {
        Machine.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        Machine.OnStateChanged -= HandleStateChanged;
    }

    private void Start()
    {
        Machine.Initialize(IdleState);
    }

    private void Update()
    {
        Machine.Tick();
    }

    private void OnValidate()
    {
        loseSightRange = Mathf.Max(loseSightRange, sightRange);
    }

    // ----- Helpers shared by the states -----

    public float FlatDistanceTo(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    public void MoveTowards(Vector3 point, float speed)
    {
        Vector3 flatPoint = new Vector3(point.x, transform.position.y, point.z);
        Vector3 direction = flatPoint - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);

        transform.position = Vector3.MoveTowards(transform.position, flatPoint, speed * Time.deltaTime);
    }

    public void FaceTarget()
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void HandleStateChanged(IState from, IState to)
    {
        if (!logStateChanges) return;

        string fromName = from == null ? "None" : from.GetType().Name;
        Debug.Log($"{name}: {fromName} -> {to.GetType().Name}", this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseSightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (!HasWaypoints) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.2f);

            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next != null)
                Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
