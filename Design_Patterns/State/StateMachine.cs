using System;

/// <summary>
/// Plain C# class that holds exactly one active state and switches between states.
/// Use it by composition inside a MonoBehaviour (the "context").
/// </summary>
public class StateMachine
{
    public IState CurrentState { get; private set; }
    public IState PreviousState { get; private set; }

    /// <summary>Raised after every transition: (from, to). "from" is null for the first state.</summary>
    public event Action<IState, IState> OnStateChanged;

    private bool _isChanging;
    private IState _pending;

    public void Initialize(IState startState)
    {
        ChangeState(startState);
    }

    public void Tick()
    {
        CurrentState?.Tick();
    }

    /// <summary>
    /// Leaves the current state and enters the next one.
    /// If called from inside Enter/Exit of another transition, the request is queued
    /// and applied right after, so Enter/Exit pairs never interleave.
    /// </summary>
    public void ChangeState(IState next)
    {
        if (next == null) return;

        if (_isChanging)
        {
            _pending = next;
            return;
        }

        _isChanging = true;
        Apply(next);

        while (_pending != null)
        {
            IState queued = _pending;
            _pending = null;
            Apply(queued);
        }

        _isChanging = false;
    }

    private void Apply(IState next)
    {
        if (next == CurrentState) return;

        IState from = CurrentState;
        from?.Exit();

        PreviousState = from;
        CurrentState = next;
        CurrentState.Enter();

        OnStateChanged?.Invoke(from, next);
    }
}
