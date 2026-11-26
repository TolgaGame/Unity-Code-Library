using System;
using System.Collections.Generic;

/// <summary>
/// The rules of a Toon Blast style game, with no Unity code in it (plain C#).
/// The board only stores numbers. It never moves or draws anything, so it is easy to test and reuse.
///
/// Cell values:  0..ColorCount-1 = a colored cube,  Empty = nothing,  Bomb = a bomb cube.
/// Cells are stored in one array. Index = y * Width + x, and y = 0 is the BOTTOM row.
/// </summary>
public class BlastBoard
{
    public const int Empty = -1;
    public const int Bomb = -2;

    /// <summary>A cube that fell from one cell to another (cell indexes).</summary>
    public struct Move
    {
        public int From;
        public int To;
    }

    /// <summary>A new cube created at the top of a column.</summary>
    public struct Spawn
    {
        public int Index;
        public int Value;
        /// <summary>0 for the lowest new cube of the column, 1 for the one above it, and so on.</summary>
        public int Offset;
    }

    public int Width { get; }
    public int Height { get; }
    public int ColorCount { get; }

    private readonly int[] _cells;
    private readonly Random _random;

    // Reused by FindGroup so a tap does not allocate memory.
    private readonly int[] _visitMark;
    private readonly List<int> _stack = new List<int>();
    private int _visitStamp;

    public BlastBoard(int width, int height, int colorCount, int seed)
    {
        Width = Math.Max(2, width);
        Height = Math.Max(2, height);
        ColorCount = Math.Max(2, colorCount);

        _cells = new int[Width * Height];
        _visitMark = new int[Width * Height];
        _random = new Random(seed);
    }

    public int Index(int x, int y) => y * Width + x;
    public int X(int index) => index % Width;
    public int Y(int index) => index / Width;
    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public int Get(int index) => _cells[index];
    public void Set(int index, int value) => _cells[index] = value;

    /// <summary>Fills the whole board with random colors. The result always has at least one possible move.</summary>
    public void Fill()
    {
        for (int i = 0; i < _cells.Length; i++)
            _cells[i] = RandomColor();

        EnsureMove();
    }

    /// <summary>
    /// Finds what a tap on (x, y) would remove and puts the cell indexes in result.
    /// Colored cube: all connected cubes of the same color (up, down, left, right). Needs 2 or more.
    /// Bomb: the 3x3 area around it. Returns false when the tap does nothing.
    /// </summary>
    public bool FindGroup(int x, int y, List<int> result)
    {
        result.Clear();

        if (!InBounds(x, y))
            return false;

        int start = Index(x, y);
        int value = _cells[start];

        if (value == Empty)
            return false;

        if (value == Bomb)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if (InBounds(nx, ny) && _cells[Index(nx, ny)] != Empty)
                        result.Add(Index(nx, ny));
                }
            }

            return true;
        }

        // Flood fill. A new stamp means "not visited yet" without clearing the whole array.
        _visitStamp++;
        _stack.Clear();
        _stack.Add(start);
        _visitMark[start] = _visitStamp;

        while (_stack.Count > 0)
        {
            int current = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            result.Add(current);

            int cx = X(current);
            int cy = Y(current);

            TryVisit(cx + 1, cy, value);
            TryVisit(cx - 1, cy, value);
            TryVisit(cx, cy + 1, value);
            TryVisit(cx, cy - 1, value);
        }

        return result.Count >= 2;
    }

    private void TryVisit(int x, int y, int value)
    {
        if (!InBounds(x, y))
            return;

        int index = Index(x, y);
        if (_visitMark[index] == _visitStamp || _cells[index] != value)
            return;

        _visitMark[index] = _visitStamp;
        _stack.Add(index);
    }

    /// <summary>Empties the given cells.</summary>
    public void Clear(List<int> indexes)
    {
        for (int i = 0; i < indexes.Count; i++)
            _cells[indexes[i]] = Empty;
    }

    /// <summary>Gravity: cubes fall down to fill the empty cells. Fills moves with what changed.</summary>
    public void Collapse(List<Move> moves)
    {
        moves.Clear();

        for (int x = 0; x < Width; x++)
        {
            int write = 0;

            // Bottom to top, so a cube always falls into a cell that is already free.
            for (int y = 0; y < Height; y++)
            {
                int value = _cells[Index(x, y)];
                if (value == Empty)
                    continue;

                if (y != write)
                {
                    _cells[Index(x, write)] = value;
                    _cells[Index(x, y)] = Empty;
                    moves.Add(new Move { From = Index(x, y), To = Index(x, write) });
                }

                write++;
            }
        }
    }

    /// <summary>Fills every empty cell with a new random color. Fills spawns with what was created.</summary>
    public void Refill(List<Spawn> spawns)
    {
        spawns.Clear();

        for (int x = 0; x < Width; x++)
        {
            int offset = 0;

            for (int y = 0; y < Height; y++)
            {
                int index = Index(x, y);
                if (_cells[index] != Empty)
                    continue;

                int value = RandomColor();
                _cells[index] = value;
                spawns.Add(new Spawn { Index = index, Value = value, Offset = offset++ });
            }
        }
    }

    /// <summary>True when the player can still do something (a bomb, or two touching cubes of one color).</summary>
    public bool HasMove()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int value = _cells[Index(x, y)];

                if (value == Bomb)
                    return true;

                if (value == Empty)
                    continue;

                if (x + 1 < Width && _cells[Index(x + 1, y)] == value)
                    return true;

                if (y + 1 < Height && _cells[Index(x, y + 1)] == value)
                    return true;
            }
        }

        return false;
    }

    /// <summary>Mixes the cubes that are on the board until a move exists. Use it when HasMove() is false.</summary>
    public void Shuffle()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            // Fisher-Yates shuffle of the values. The board is full when this is called.
            for (int i = _cells.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int temp = _cells[i];
                _cells[i] = _cells[j];
                _cells[j] = temp;
            }

            if (HasMove())
                return;
        }

        EnsureMove();
    }

    // Last resort: copy a color to its right neighbor so a pair is guaranteed.
    private void EnsureMove()
    {
        if (HasMove())
            return;

        int value = _cells[0] >= 0 ? _cells[0] : 0;
        _cells[0] = value;
        _cells[1] = value;
    }

    private int RandomColor() => _random.Next(ColorCount);
}
