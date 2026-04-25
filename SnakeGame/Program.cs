using System.Text;

const int Width = 30;
const int Height = 18;
const int InitialDelayMs = 140;
const int MinDelayMs = 70;
const int DelayStepMs = 5;

Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

while (true)
{
    PlayGame();

    Console.SetCursorPosition(0, Height + 5);
    Console.Write("Play again? Press Enter to restart or Esc to quit. ".PadRight(80));

    ConsoleKey key;
    do
    {
        key = Console.ReadKey(intercept: true).Key;
    } while (key != ConsoleKey.Enter && key != ConsoleKey.Escape);

    if (key == ConsoleKey.Escape)
    {
        break;
    }

    Console.Clear();
}

Console.CursorVisible = true;

static void PlayGame()
{
    var random = new Random();
    var snake = new List<Cell>
    {
        new(Width / 2, Height / 2),
        new(Width / 2 - 1, Height / 2),
        new(Width / 2 - 2, Height / 2)
    };

    var direction = Direction.Right;
    var food = CreateFood(random, snake);
    var score = 0;
    var delay = InitialDelayMs;
    var isGameOver = false;
    var gameOverMessage = string.Empty;

    Console.Clear();

    while (!isGameOver)
    {
        ReadInput(ref direction);

        var head = snake[0];
        var nextHead = direction switch
        {
            Direction.Up => head with { Y = head.Y - 1 },
            Direction.Down => head with { Y = head.Y + 1 },
            Direction.Left => head with { X = head.X - 1 },
            Direction.Right => head with { X = head.X + 1 },
            _ => head
        };

        if (nextHead.X < 0 || nextHead.X >= Width || nextHead.Y < 0 || nextHead.Y >= Height)
        {
            isGameOver = true;
            gameOverMessage = "You hit the wall.";
            DrawFrame(snake, food, score, gameOverMessage);
            break;
        }

        // Moving into the current tail is valid unless the snake grows this tick.
        var willGrow = nextHead == food;
        var collisionLength = willGrow ? snake.Count : snake.Count - 1;

        if (snake.Take(collisionLength).Any(part => part == nextHead))
        {
            isGameOver = true;
            gameOverMessage = "You ran into yourself.";
            DrawFrame(snake, food, score, gameOverMessage);
            break;
        }

        snake.Insert(0, nextHead);

        if (willGrow)
        {
            score++;
            delay = Math.Max(MinDelayMs, delay - DelayStepMs);
            food = CreateFood(random, snake);
        }
        else
        {
            snake.RemoveAt(snake.Count - 1);
        }

        DrawFrame(snake, food, score, string.Empty);
        Thread.Sleep(delay);
    }
}

static void DrawFrame(List<Cell> snake, Cell food, int score, string gameOverMessage)
{
    var canvas = new char[Height, Width];
    for (var y = 0; y < Height; y++)
    {
        for (var x = 0; x < Width; x++)
        {
            canvas[y, x] = ' ';
        }
    }

    for (var i = 0; i < snake.Count; i++)
    {
        var part = snake[i];
        canvas[part.Y, part.X] = i == 0 ? 'O' : 'o';
    }

    canvas[food.Y, food.X] = '@';

    var frame = new StringBuilder();
    frame.Append('┌');
    frame.Append(new string('─', Width));
    frame.AppendLine("┐");

    for (var y = 0; y < Height; y++)
    {
        frame.Append('│');
        for (var x = 0; x < Width; x++)
        {
            frame.Append(canvas[y, x]);
        }

        frame.AppendLine("│");
    }

    frame.Append('└');
    frame.Append(new string('─', Width));
    frame.AppendLine("┘");
    frame.AppendLine($"Score: {score}".PadRight(40));
    frame.AppendLine("Use arrow keys to move.".PadRight(40));
    frame.AppendLine(gameOverMessage.PadRight(40));

    Console.SetCursorPosition(0, 0);
    Console.Write(frame.ToString());
}

static Cell CreateFood(Random random, List<Cell> snake)
{
    Cell food;
    do
    {
        food = new Cell(random.Next(Width), random.Next(Height));
    } while (snake.Contains(food));

    return food;
}

static void ReadInput(ref Direction direction)
{
    while (Console.KeyAvailable)
    {
        var key = Console.ReadKey(intercept: true).Key;
        var proposedDirection = key switch
        {
            ConsoleKey.UpArrow => Direction.Up,
            ConsoleKey.DownArrow => Direction.Down,
            ConsoleKey.LeftArrow => Direction.Left,
            ConsoleKey.RightArrow => Direction.Right,
            _ => direction
        };

        if (!IsOpposite(direction, proposedDirection))
        {
            direction = proposedDirection;
        }
    }
}

static bool IsOpposite(Direction current, Direction proposed)
{
    return (current, proposed) switch
    {
        (Direction.Up, Direction.Down) => true,
        (Direction.Down, Direction.Up) => true,
        (Direction.Left, Direction.Right) => true,
        (Direction.Right, Direction.Left) => true,
        _ => false
    };
}

readonly record struct Cell(int X, int Y);

enum Direction
{
    Up,
    Down,
    Left,
    Right
}
