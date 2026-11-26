using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The game loop of the blast example: input, rules, score, moves, win and lose.
///
/// Setup: add this component to an empty GameObject in an empty scene and press Play.
/// It adds BlastBoardView and BlastHUD by itself, and creates / fits the camera.
///
/// How a turn works:
///   tap a group of 2+ same colored cubes -> they pop -> cubes above fall -> new cubes drop in
///   -> check win / lose -> reshuffle if there is no move left.
/// A group of "Bomb Threshold" or more cubes leaves a bomb where you tapped. Tapping a bomb clears 3x3.
/// </summary>
[RequireComponent(typeof(BlastBoardView))]
[RequireComponent(typeof(BlastHUD))]
public class BlastGameController : MonoBehaviour
{
    public enum GameState
    {
        /// <summary>Animations are running. Taps are ignored.</summary>
        Busy,
        /// <summary>Waiting for the player to tap.</summary>
        Playing,
        Won,
        Lost
    }

    [Header("Board")]
    [SerializeField, Min(2)] private int width = 8;
    [SerializeField, Min(2)] private int height = 8;
    [Tooltip("How many different colors are used (2 to the number of colors in BlastBoardView).")]
    [SerializeField, Min(2)] private int colorCount = 5;
    [Tooltip("0 = a different board every time. Any other number = the same board every time.")]
    [SerializeField] private int seed;

    [Header("Rules")]
    [SerializeField, Min(1)] private int startMoves = 20;
    [SerializeField, Min(1)] private int targetScore = 1500;
    [Tooltip("A group this big or bigger leaves a bomb. 0 turns bombs off.")]
    [SerializeField, Min(0)] private int bombThreshold = 5;
    [Tooltip("A group of n cubes gives n * n * this many points.")]
    [SerializeField, Min(1)] private int pointsPerTile = 5;

    [Header("Camera")]
    [SerializeField] private bool fitCamera = true;
    [SerializeField] private Color backgroundColor = new Color(0.11f, 0.13f, 0.2f);
    [Tooltip("Empty space around the board, in cells.")]
    [SerializeField] private float cameraMargin = 0.6f;
    [Tooltip("Extra space above the board for the score text, in cells.")]
    [SerializeField] private float topSpace = 1.2f;

    public int Score { get; private set; }
    public int MovesLeft { get; private set; }
    public int TargetScore => targetScore;
    public GameState State { get; private set; } = GameState.Busy;

    /// <summary>Raised once when a game ends. true = the target score was reached.</summary>
    public event Action<bool> OnGameFinished;

    private BlastBoard _board;
    private BlastBoardView _view;
    private Camera _camera;
    private int _screenWidth;
    private int _screenHeight;
    private float _finishTime;

    // Reused every turn so playing does not create garbage.
    private readonly List<int> _group = new List<int>();
    private readonly List<BlastBoard.Move> _moves = new List<BlastBoard.Move>();
    private readonly List<BlastBoard.Spawn> _spawns = new List<BlastBoard.Spawn>();

    private void Awake()
    {
        _view = GetComponent<BlastBoardView>();
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (_camera == null)
            return; // StartGame did not finish (for example the palette is invalid).

        if (fitCamera && (Screen.width != _screenWidth || Screen.height != _screenHeight))
            FitCamera();

        if (!BlastInput.TryGetPress(out Vector2 screenPosition))
            return;

        if (State == GameState.Playing)
        {
            Vector3 world = _camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(_camera.transform.position.z)));

            if (_view.WorldToCell(world, out int index))
                StartCoroutine(PlayMove(index));
        }
        else if ((State == GameState.Won || State == GameState.Lost) && Time.unscaledTime - _finishTime > 0.5f)
        {
            // The short delay stops the last tap of the game from restarting it by accident.
            StartGame();
        }
    }

    /// <summary>Starts a new game. Also used by the "tap to play again" screen.</summary>
    public void StartGame()
    {
        StopAllCoroutines();

        if (_view.PaletteLength < 2)
        {
            Debug.LogError("[BlastGameController] BlastBoardView needs at least 2 colors in its Palette.");
            return;
        }

        colorCount = Mathf.Clamp(colorCount, 2, _view.PaletteLength);

        _board = new BlastBoard(width, height, colorCount, seed != 0 ? seed : Environment.TickCount);
        _board.Fill();

        Score = 0;
        MovesLeft = startMoves;
        State = GameState.Busy;

        _view.Build(_board);

        if (_camera == null)
            _camera = GetOrCreateCamera();
        if (fitCamera)
            FitCamera();

        StartCoroutine(WaitThenPlay());
    }

    private IEnumerator WaitThenPlay()
    {
        yield return _view.WaitForIdle();
        State = GameState.Playing;
    }

    private IEnumerator PlayMove(int index)
    {
        int x = _board.X(index);
        int y = _board.Y(index);

        if (!_board.FindGroup(x, y, _group))
        {
            // A single cube is not a move. It costs nothing, the cube just wiggles.
            _view.Shake(index);
            yield break;
        }

        State = GameState.Busy;
        MovesLeft--;

        bool wasBomb = _board.Get(index) == BlastBoard.Bomb;
        int removedCount = _group.Count;
        Score += removedCount * removedCount * pointsPerTile;

        // A big group keeps the tapped cube and turns it into a bomb.
        bool makeBomb = !wasBomb && bombThreshold > 0 && removedCount >= bombThreshold;
        if (makeBomb)
            _group.Remove(index);

        _board.Clear(_group);
        _view.Pop(_group);

        if (makeBomb)
        {
            _board.Set(index, BlastBoard.Bomb);
            _view.SetValue(index, BlastBoard.Bomb);
        }

        yield return _view.WaitForIdle();

        _board.Collapse(_moves);
        _view.ApplyMoves(_moves);
        _board.Refill(_spawns);
        _view.ApplySpawns(_spawns);

        yield return _view.WaitForIdle();

        if (Score >= targetScore)
        {
            Finish(true);
            yield break;
        }

        if (MovesLeft <= 0)
        {
            Finish(false);
            yield break;
        }

        // No move left (rare): mix the cubes instead of leaving the player stuck.
        if (!_board.HasMove())
        {
            _board.Shuffle();
            _view.Refresh(_board);
            yield return _view.WaitForIdle();
        }

        State = GameState.Playing;
    }

    private void Finish(bool won)
    {
        State = won ? GameState.Won : GameState.Lost;
        _finishTime = Time.unscaledTime;
        OnGameFinished?.Invoke(won);
    }

    private Camera GetOrCreateCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
            return cam;

        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>(); // Without one, Unity prints "no audio listeners" every frame.
        return go.AddComponent<Camera>();
    }

    // Makes the whole board (plus margin and score space) fit any screen shape.
    private void FitCamera()
    {
        _screenWidth = Screen.width;
        _screenHeight = Screen.height;

        Vector2 board = _view.BoardSize;
        float totalHeight = board.y + cameraMargin * 2f + topSpace;
        float totalWidth = board.x + cameraMargin * 2f;

        _camera.orthographic = true;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = backgroundColor;
        _camera.orthographicSize = Mathf.Max(totalHeight * 0.5f, totalWidth * 0.5f / _camera.aspect);

        // Move the camera up a little, so the board sits lower and the score has room on top.
        Vector3 position = _view.transform.position;
        _camera.transform.position = new Vector3(position.x, position.y + topSpace * 0.5f, -10f);
    }
}
