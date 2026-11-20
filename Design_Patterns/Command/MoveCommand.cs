using UnityEngine;

/// <summary>Moves a transform by a fixed offset. Undo puts it back where it was.</summary>
public class MoveCommand : ICommand
{
    private readonly Transform _target;
    private readonly Vector3 _delta;

    private Vector3 _from;
    private Vector3 _to;
    private bool _captured;

    public MoveCommand(Transform target, Vector3 delta)
    {
        _target = target;
        _delta = delta;
    }

    public void Execute()
    {
        // Unity's null check also catches objects that were destroyed after the command was stored.
        if (_target == null)
            return;

        // Capture the positions once so redo always lands on exactly the same spot.
        if (!_captured)
        {
            _from = _target.position;
            _to = _from + _delta;
            _captured = true;
        }

        _target.position = _to;
    }

    public void Undo()
    {
        if (_target == null)
            return;

        _target.position = _from;
    }
}
