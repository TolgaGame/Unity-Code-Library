/// <summary>
/// Contract every state implements (interface based version).
/// Enter  : runs once when the state becomes active.
/// Tick   : runs every frame while the state is active.
/// Exit   : runs once when the state is left.
/// </summary>
public interface IState
{
    void Enter();
    void Tick();
    void Exit();
}
