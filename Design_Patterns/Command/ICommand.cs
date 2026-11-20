/// <summary>
/// One action turned into an object. Because it is an object it can be stored, undone and replayed.
/// </summary>
public interface ICommand
{
    /// <summary>Does the action. Also used for redo, so it must give the same result every time.</summary>
    void Execute();

    /// <summary>Puts everything back exactly as it was before Execute.</summary>
    void Undo();
}
