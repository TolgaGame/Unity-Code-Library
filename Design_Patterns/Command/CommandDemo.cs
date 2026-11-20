using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example context. Every public method is meant to be hooked to a UI Button (OnClick),
/// so it works with both the old and the new Input System.
/// The demo never changes the object directly: it only creates commands and hands them to the invoker.
/// </summary>
public class CommandDemo : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float stepSize = 1f;
    [SerializeField] private int maxHistory = 50;

    [Header("UI (optional)")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;

    private CommandInvoker _invoker;

    private void Awake()
    {
        _invoker = new CommandInvoker(maxHistory);
    }

    private void OnEnable()
    {
        _invoker.OnHistoryChanged += RefreshButtons;
        RefreshButtons();
    }

    private void OnDisable()
    {
        _invoker.OnHistoryChanged -= RefreshButtons;
    }

    public void MoveUp() => Move(Vector3.up);
    public void MoveDown() => Move(Vector3.down);
    public void MoveLeft() => Move(Vector3.left);
    public void MoveRight() => Move(Vector3.right);

    public void PaintRandomColor()
    {
        if (targetRenderer == null)
            return;

        Color color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
        _invoker.Execute(new ChangeColorCommand(targetRenderer, color));
    }

    public void Undo() => _invoker.Undo();
    public void Redo() => _invoker.Redo();

    private void Move(Vector3 direction)
    {
        if (target == null)
            return;

        _invoker.Execute(new MoveCommand(target, direction * stepSize));
    }

    private void RefreshButtons()
    {
        if (undoButton != null) undoButton.interactable = _invoker.CanUndo;
        if (redoButton != null) redoButton.interactable = _invoker.CanRedo;
    }
}
