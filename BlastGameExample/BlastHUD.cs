using UnityEngine;

/// <summary>
/// Score, moves and the win / lose screen. Uses OnGUI, so it needs no Canvas, fonts or prefabs.
/// In a real game, replace it with your own uGUI / UI Toolkit screen and read the values
/// from BlastGameController (Score, MovesLeft, TargetScore, State, OnGameFinished).
/// </summary>
public class BlastHUD : MonoBehaviour
{
    private BlastGameController _game;
    private GUIStyle _left;
    private GUIStyle _right;
    private GUIStyle _title;
    private GUIStyle _hint;

    // BlastGameController adds this component, so both are always on the same GameObject.
    private void Awake()
    {
        _game = GetComponent<BlastGameController>();
    }

    private void OnGUI()
    {
        // Only text is drawn, so there is nothing to do for the other IMGUI events.
        if (_game == null || Event.current.type != EventType.Repaint)
            return;

        int unit = Mathf.Max(14, Mathf.RoundToInt(Mathf.Min(Screen.width * 0.06f, Screen.height * 0.035f)));
        EnsureStyles(unit);

        // Screen.safeArea starts at the bottom-left, IMGUI starts at the top-left.
        Rect safe = Screen.safeArea;
        float top = Screen.height - safe.yMax + unit * 0.5f;
        float barHeight = unit * 1.6f;

        GUI.Label(new Rect(safe.xMin + unit, top, safe.width * 0.6f, barHeight),
            $"Score  {_game.Score} / {_game.TargetScore}", _left);
        GUI.Label(new Rect(safe.xMax - unit - safe.width * 0.35f, top, safe.width * 0.35f, barHeight),
            $"Moves  {_game.MovesLeft}", _right);

        if (_game.State == BlastGameController.GameState.Won || _game.State == BlastGameController.GameState.Lost)
            DrawResult(unit);
    }

    private void DrawResult(int unit)
    {
        bool won = _game.State == BlastGameController.GameState.Won;

        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float centerY = Screen.height * 0.5f;
        GUI.Label(new Rect(0f, centerY - unit * 3f, Screen.width, unit * 2.5f),
            won ? "LEVEL COMPLETE!" : "OUT OF MOVES", _title);
        GUI.Label(new Rect(0f, centerY, Screen.width, unit * 1.5f), $"Score  {_game.Score}", _hint);
        GUI.Label(new Rect(0f, centerY + unit * 1.8f, Screen.width, unit * 1.5f), "Tap to play again", _hint);
    }

    // Styles can only be created inside OnGUI. They are created once, only the font size follows the screen.
    private void EnsureStyles(int unit)
    {
        if (_left == null)
        {
            _left = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _right = new GUIStyle(_left) { alignment = TextAnchor.MiddleRight };
            _title = new GUIStyle(_left) { alignment = TextAnchor.MiddleCenter };
            _hint = new GUIStyle(_left) { alignment = TextAnchor.MiddleCenter };
            _left.normal.textColor = _right.normal.textColor = _title.normal.textColor = _hint.normal.textColor = Color.white;
        }

        _left.fontSize = _right.fontSize = _hint.fontSize = unit;
        _title.fontSize = Mathf.RoundToInt(unit * 1.8f);
    }
}
