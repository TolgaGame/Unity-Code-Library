using UnityEngine;

/// <summary>Changes a renderer's color. Undo restores the previous color.</summary>
public class ChangeColorCommand : ICommand
{
    private readonly Renderer _target;
    private readonly Color _newColor;

    private Color _oldColor;
    private bool _captured;

    public ChangeColorCommand(Renderer target, Color newColor)
    {
        _target = target;
        _newColor = newColor;
    }

    public void Execute()
    {
        if (_target == null)
            return;

        if (!_captured)
        {
            _oldColor = _target.material.color;
            _captured = true;
        }

        _target.material.color = _newColor;
    }

    public void Undo()
    {
        if (_target == null)
            return;

        _target.material.color = _oldColor;
    }
}
