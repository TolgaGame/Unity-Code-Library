using System;
using System.Collections.Generic;

/// <summary>
/// Runs commands and remembers them so they can be undone and redone.
/// A plain C# class: own it by composition inside any MonoBehaviour.
///
/// _history:  [A][B][C][D]
///                    ^ _cursor (next free slot). Undo moves it left, Redo moves it right.
/// Executing a new command while the cursor is not at the end throws away the "future" commands.
/// </summary>
public class CommandInvoker
{
    private readonly List<ICommand> _history = new List<ICommand>();
    private readonly int _maxHistory;
    private int _cursor;

    /// <summary>Raised after every Execute, Undo, Redo and Clear. Good for enabling/disabling buttons.</summary>
    public event Action OnHistoryChanged;

    public bool CanUndo => _cursor > 0;
    public bool CanRedo => _cursor < _history.Count;
    public int Count => _history.Count;

    /// <param name="maxHistory">Oldest commands are dropped beyond this size so memory stays bounded.</param>
    public CommandInvoker(int maxHistory = 100)
    {
        _maxHistory = Math.Max(1, maxHistory);
    }

    public void Execute(ICommand command)
    {
        if (command == null)
            return;

        command.Execute();

        // A new action invalidates everything that was undone before it.
        if (_cursor < _history.Count)
        {
            _history.RemoveRange(_cursor, _history.Count - _cursor);
        }

        _history.Add(command);
        _cursor++;

        if (_history.Count > _maxHistory)
        {
            _history.RemoveAt(0);
            _cursor--;
        }

        OnHistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo)
            return;

        _cursor--;
        _history[_cursor].Undo();
        OnHistoryChanged?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo)
            return;

        _history[_cursor].Execute();
        _cursor++;
        OnHistoryChanged?.Invoke();
    }

    public void Clear()
    {
        _history.Clear();
        _cursor = 0;
        OnHistoryChanged?.Invoke();
    }
}
