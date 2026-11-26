using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows a BlastBoard on the screen. It owns every BlastTile and keeps them in the same cells as the board.
/// All methods only START animations and return at once. Use WaitForIdle() to wait for them to finish.
/// Removed cubes go back into a small pool, so playing never creates garbage.
/// </summary>
public class BlastBoardView : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("One color per cube type. The game uses the first 'Color Count' colors.")]
    [SerializeField] private Color[] palette =
    {
        new Color(0.92f, 0.26f, 0.26f),
        new Color(0.25f, 0.55f, 0.95f),
        new Color(0.30f, 0.78f, 0.40f),
        new Color(0.98f, 0.80f, 0.20f),
        new Color(0.65f, 0.38f, 0.90f)
    };

    [SerializeField] private float cellSize = 1f;
    [Tooltip("Below 1 leaves a small gap between cubes.")]
    [SerializeField, Range(0.5f, 1f)] private float tileScale = 0.92f;

    [Header("Animation (seconds)")]
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float fallBaseDuration = 0.1f;
    [Tooltip("Extra fall time for every cell a cube falls.")]
    [SerializeField] private float fallTimePerCell = 0.04f;

    private readonly Stack<BlastTile> _pool = new Stack<BlastTile>();
    private BlastTile[] _tiles = new BlastTile[0];
    private int _width;
    private int _height;
    private int _popping;

    public int PaletteLength => palette.Length;
    public Vector2 BoardSize => new Vector2(_width * cellSize, _height * cellSize);

    /// <summary>Clears the old cubes and builds the board. The cubes drop in from above.</summary>
    public void Build(BlastBoard board)
    {
        ReleaseAll();

        _width = board.Width;
        _height = board.Height;
        _tiles = new BlastTile[_width * _height];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int index = y * _width + x;
                _tiles[index] = CreateTile(board.Get(index), x, y, y + _height);
            }
        }
    }

    /// <summary>Screen/world position to a cell index. False when the position is outside the board.</summary>
    public bool WorldToCell(Vector3 world, out int index)
    {
        Vector3 local = world - transform.position;
        int x = Mathf.FloorToInt(local.x / cellSize + _width * 0.5f);
        int y = Mathf.FloorToInt(local.y / cellSize + _height * 0.5f);

        bool inside = x >= 0 && x < _width && y >= 0 && y < _height;
        index = inside ? y * _width + x : -1;
        return inside;
    }

    /// <summary>Pops (shrinks and removes) the cubes in the given cells.</summary>
    public void Pop(List<int> indexes)
    {
        for (int i = 0; i < indexes.Count; i++)
        {
            BlastTile tile = _tiles[indexes[i]];
            if (tile == null)
                continue;

            _tiles[indexes[i]] = null;
            _popping++;
            tile.Pop(popDuration, OnPopFinished);
        }
    }

    /// <summary>Changes one cube (for example into a bomb) with a small bounce.</summary>
    public void SetValue(int index, int value)
    {
        BlastTile tile = _tiles[index];
        if (tile == null)
            return;

        ApplyLook(tile, value);
        tile.Punch(0.2f);
    }

    /// <summary>Makes the cubes fall to the cells the board moved them to.</summary>
    public void ApplyMoves(List<BlastBoard.Move> moves)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            BlastBoard.Move move = moves[i];
            BlastTile tile = _tiles[move.From];
            if (tile == null)
                continue;

            _tiles[move.From] = null;
            _tiles[move.To] = tile;

            int toX = move.To % _width;
            int toY = move.To / _width;
            int fromY = move.From / _width;
            tile.MoveTo(CellToWorld(toX, toY), FallDuration(fromY - toY));
        }
    }

    /// <summary>Creates the new cubes above the board and lets them fall in.</summary>
    public void ApplySpawns(List<BlastBoard.Spawn> spawns)
    {
        for (int i = 0; i < spawns.Count; i++)
        {
            BlastBoard.Spawn spawn = spawns[i];
            int x = spawn.Index % _width;
            int y = spawn.Index / _width;

            _tiles[spawn.Index] = CreateTile(spawn.Value, x, y, _height + spawn.Offset);
        }
    }

    /// <summary>Updates every cube to the board values (after a shuffle).</summary>
    public void Refresh(BlastBoard board)
    {
        for (int i = 0; i < _tiles.Length; i++)
        {
            if (_tiles[i] == null)
                continue;

            ApplyLook(_tiles[i], board.Get(i));
            _tiles[i].Punch(0.25f);
        }
    }

    public void Shake(int index)
    {
        if (_tiles[index] != null)
            _tiles[index].Shake(0.25f);
    }

    /// <summary>Wait for it with: yield return view.WaitForIdle();</summary>
    public IEnumerator WaitForIdle()
    {
        while (IsBusy())
            yield return null;
    }

    private bool IsBusy()
    {
        if (_popping > 0)
            return true;

        for (int i = 0; i < _tiles.Length; i++)
        {
            if (_tiles[i] != null && _tiles[i].IsAnimating)
                return true;
        }

        return false;
    }

    private BlastTile CreateTile(int value, int x, int y, float startY)
    {
        BlastTile tile = _pool.Count > 0 ? _pool.Pop() : NewTile();
        ApplyLook(tile, value, true);

        tile.transform.position = CellToWorld(x, startY);
        if (startY != y)
            tile.MoveTo(CellToWorld(x, y), FallDuration(startY - y));

        return tile;
    }

    private BlastTile NewTile()
    {
        var go = new GameObject("Tile");
        go.transform.SetParent(transform, false);
        return go.AddComponent<BlastTile>();
    }

    private void ApplyLook(BlastTile tile, int value, bool fresh = false)
    {
        Sprite sprite = value == BlastBoard.Bomb ? BlastSprites.Bomb : BlastSprites.Cube;
        Color color = value == BlastBoard.Bomb ? Color.white : palette[value % palette.Length];

        if (fresh)
            tile.Setup(value, sprite, color, tileScale * cellSize);
        else
            tile.SetLook(value, sprite, color);
    }

    private void OnPopFinished(BlastTile tile)
    {
        _popping--;
        Recycle(tile);
    }

    private void Recycle(BlastTile tile)
    {
        tile.gameObject.SetActive(false);
        _pool.Push(tile);
    }

    private void ReleaseAll()
    {
        for (int i = 0; i < _tiles.Length; i++)
        {
            if (_tiles[i] == null)
                continue;

            Recycle(_tiles[i]);
            _tiles[i] = null;
        }
    }

    private float FallDuration(float cells) => fallBaseDuration + fallTimePerCell * cells;

    // Centers the board on this object. y can be above the board for cubes that are still falling in.
    private Vector3 CellToWorld(int x, float y)
    {
        return transform.position + new Vector3(
            (x - (_width - 1) * 0.5f) * cellSize,
            (y - (_height - 1) * 0.5f) * cellSize,
            0f);
    }
}
