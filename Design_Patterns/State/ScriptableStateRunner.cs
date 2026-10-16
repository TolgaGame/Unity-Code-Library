using UnityEngine;

/// <summary>
/// CONTEXT for ScriptableObject states.
/// Holds the current state asset and the per-object data the states need (time in state, the renderer).
/// </summary>
public class ScriptableStateRunner : MonoBehaviour
{
    [SerializeField] private ScriptableState initialState;
    [SerializeField] private Renderer targetRenderer;

    public ScriptableState CurrentState { get; private set; }

    /// <summary>Seconds since the current state was entered. Reset on every transition.</summary>
    public float TimeInState { get; private set; }

    public Renderer Renderer => targetRenderer;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        if (initialState == null)
        {
            Debug.LogWarning($"{name}: no Initial State assigned.", this);
            return;
        }

        ChangeState(initialState);
    }

    private void Update()
    {
        if (CurrentState == null) return;

        TimeInState += Time.deltaTime;
        CurrentState.OnTick(this);
    }

    public void ChangeState(ScriptableState next)
    {
        if (next == null || next == CurrentState) return;

        CurrentState?.OnExit(this);

        CurrentState = next;
        TimeInState = 0f;

        CurrentState.OnEnter(this);
    }
}
