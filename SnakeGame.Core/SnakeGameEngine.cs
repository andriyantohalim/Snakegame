namespace SnakeGame.Core;

using System.Drawing;

/// <summary>
/// Encapsulates all snake-game logic independently of any UI framework.
/// </summary>
public sealed class SnakeGameEngine
{
    public const int GridWidth = 24;
    public const int GridHeight = 18;
    public const int CellSize = 24;
    public const int InitialTimerInterval = 110;
    public const int MinTimerInterval = 65;

    private readonly Random _random;
    private readonly List<Point> _snake = new();

    public IReadOnlyList<Point> Snake => _snake.AsReadOnly();
    public Point Food { get; private set; }
    public Direction Direction { get; private set; } = Direction.Right;
    public Direction NextDirection { get; private set; } = Direction.Right;
    public int Score { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsGameOver { get; private set; }
    public int TimerInterval { get; private set; } = InitialTimerInterval;

    public SnakeGameEngine(Random? random = null)
    {
        _random = random ?? new Random();
        ResetState();
        SpawnFood();
    }

    /// <summary>Resets all game state and places a new food item.</summary>
    public void StartNewGame()
    {
        ResetState();
        SpawnFood();
    }

    /// <summary>
    /// Advances the game by one tick.
    /// </summary>
    /// <returns>A <see cref="TickResult"/> describing what happened.</returns>
    public TickResult Tick()
    {
        if (IsGameOver || IsPaused)
            return TickResult.Skipped;

        Direction = NextDirection;
        var head = _snake[0];
        var nextHead = GetNextHead(head, Direction);

        if (HitsWall(nextHead))
        {
            IsGameOver = true;
            return TickResult.HitWall;
        }

        var grows = nextHead == Food;
        // When growing, the tail isn't removed this tick, so every segment is a collision candidate.
        var bodyCountToCheck = grows ? _snake.Count : _snake.Count - 1;
        if (_snake.Take(bodyCountToCheck).Contains(nextHead))
        {
            IsGameOver = true;
            return TickResult.HitSelf;
        }

        _snake.Insert(0, nextHead);

        if (grows)
        {
            Score++;
            TimerInterval = Math.Max(MinTimerInterval, TimerInterval - 3);
            SpawnFood();
            return TickResult.AteFood;
        }

        _snake.RemoveAt(_snake.Count - 1);
        return TickResult.Moved;
    }

    /// <summary>Toggles the paused state. Has no effect when the game is over.</summary>
    /// <returns><see langword="true"/> if the state changed; <see langword="false"/> when game is over.</returns>
    public bool TogglePause()
    {
        if (IsGameOver)
            return false;

        IsPaused = !IsPaused;
        return true;
    }

    /// <summary>
    /// Queues a new direction for the next tick.
    /// Has no effect when the game is over, paused, or the direction is opposite to the current one.
    /// </summary>
    /// <returns><see langword="true"/> if the direction was accepted.</returns>
    public bool TrySetDirection(Direction proposed)
    {
        if (IsGameOver || IsPaused)
            return false;

        if (IsOpposite(Direction, proposed))
            return false;

        NextDirection = proposed;
        return true;
    }

    /// <summary>
    /// Manually places food at <paramref name="position"/>.
    /// Useful for testing and any scenario that requires a specific food location.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="position"/> is occupied by the snake.</exception>
    public void PlaceFood(Point position)
    {
        if (_snake.Contains(position))
            throw new InvalidOperationException("Cannot place food on a snake cell.");
        Food = position;
    }

    /// <summary>Spawns food at a random position that is not occupied by the snake.</summary>
    public void SpawnFood()
    {
        Point food;
        do
        {
            food = new Point(_random.Next(GridWidth), _random.Next(GridHeight));
        } while (_snake.Contains(food));
        Food = food;
    }

    /// <returns><see langword="true"/> when <paramref name="point"/> is outside the grid boundaries.</returns>
    public static bool HitsWall(Point point) =>
        point.X < 0 || point.X >= GridWidth || point.Y < 0 || point.Y >= GridHeight;

    /// <returns><see langword="true"/> when <paramref name="proposed"/> is the exact reverse of <paramref name="current"/>.</returns>
    public static bool IsOpposite(Direction current, Direction proposed) =>
        (current, proposed) switch
        {
            (Direction.Up, Direction.Down) => true,
            (Direction.Down, Direction.Up) => true,
            (Direction.Left, Direction.Right) => true,
            (Direction.Right, Direction.Left) => true,
            _ => false
        };

    /// <returns>The grid cell that the snake head would occupy after moving one step in <paramref name="direction"/>.</returns>
    public static Point GetNextHead(Point head, Direction direction) =>
        direction switch
        {
            Direction.Up => new Point(head.X, head.Y - 1),
            Direction.Down => new Point(head.X, head.Y + 1),
            Direction.Left => new Point(head.X - 1, head.Y),
            Direction.Right => new Point(head.X + 1, head.Y),
            _ => head
        };

    /// <returns>The pixel <see cref="Rectangle"/> for the given grid <paramref name="point"/>.</returns>
    public static Rectangle CellToRect(Point point) =>
        new Rectangle(point.X * CellSize, point.Y * CellSize, CellSize, CellSize);

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void ResetState()
    {
        _snake.Clear();
        _snake.Add(new Point(GridWidth / 2, GridHeight / 2));
        _snake.Add(new Point(GridWidth / 2 - 1, GridHeight / 2));
        _snake.Add(new Point(GridWidth / 2 - 2, GridHeight / 2));

        Direction = Direction.Right;
        NextDirection = Direction.Right;
        Score = 0;
        IsPaused = false;
        IsGameOver = false;
        TimerInterval = InitialTimerInterval;
    }
}
