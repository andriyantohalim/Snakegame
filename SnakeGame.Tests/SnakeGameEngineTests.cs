using System.Drawing;
using SnakeGame.Core;
using Xunit;

namespace SnakeGame.Tests;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/// <summary>
/// A <see cref="Random"/> subclass that returns a predetermined sequence of
/// values so that food-placement calls are fully deterministic in tests.
/// </summary>
internal sealed class SequenceRandom : Random
{
    private readonly Queue<int> _values;

    public SequenceRandom(params int[] values) => _values = new Queue<int>(values);

    public override int Next(int maxValue) =>
        _values.Count > 0 ? _values.Dequeue() % maxValue : 0;
}

// ---------------------------------------------------------------------------
// Constructor / initial state
// ---------------------------------------------------------------------------

public class Constructor_Tests
{
    [Fact]
    public void Snake_Has_Three_Segments_On_Construction()
    {
        var engine = new SnakeGameEngine();
        Assert.Equal(3, engine.Snake.Count);
    }

    [Fact]
    public void Snake_Head_Is_At_Grid_Center()
    {
        var engine = new SnakeGameEngine();
        var expectedHead = new Point(SnakeGameEngine.GridWidth / 2, SnakeGameEngine.GridHeight / 2);
        Assert.Equal(expectedHead, engine.Snake[0]);
    }

    [Fact]
    public void Snake_Segments_Are_Horizontal_Going_Left()
    {
        var engine = new SnakeGameEngine();
        int y = SnakeGameEngine.GridHeight / 2;
        int headX = SnakeGameEngine.GridWidth / 2;
        Assert.Equal(new Point(headX,     y), engine.Snake[0]);
        Assert.Equal(new Point(headX - 1, y), engine.Snake[1]);
        Assert.Equal(new Point(headX - 2, y), engine.Snake[2]);
    }

    [Fact]
    public void Initial_Direction_Is_Right()
    {
        var engine = new SnakeGameEngine();
        Assert.Equal(Direction.Right, engine.Direction);
        Assert.Equal(Direction.Right, engine.NextDirection);
    }

    [Fact]
    public void Initial_Score_Is_Zero()
    {
        var engine = new SnakeGameEngine();
        Assert.Equal(0, engine.Score);
    }

    [Fact]
    public void Initial_State_Is_Not_Paused_Or_GameOver()
    {
        var engine = new SnakeGameEngine();
        Assert.False(engine.IsPaused);
        Assert.False(engine.IsGameOver);
    }

    [Fact]
    public void Initial_TimerInterval_Is_Correct()
    {
        var engine = new SnakeGameEngine();
        Assert.Equal(SnakeGameEngine.InitialTimerInterval, engine.TimerInterval);
    }

    [Fact]
    public void Food_Is_Not_On_Snake_After_Construction()
    {
        var engine = new SnakeGameEngine();
        Assert.DoesNotContain(engine.Food, engine.Snake);
    }
}

// ---------------------------------------------------------------------------
// StartNewGame
// ---------------------------------------------------------------------------

public class StartNewGame_Tests
{
    [Fact]
    public void StartNewGame_Resets_Score()
    {
        // Make the snake eat food, then restart.
        var engine = new SnakeGameEngine();
        engine.PlaceFood(new Point(SnakeGameEngine.GridWidth / 2 + 1, SnakeGameEngine.GridHeight / 2));
        engine.Tick(); // eat
        Assert.Equal(1, engine.Score);

        engine.StartNewGame();
        Assert.Equal(0, engine.Score);
    }

    [Fact]
    public void StartNewGame_Resets_Snake_Length()
    {
        var engine = new SnakeGameEngine();
        engine.PlaceFood(new Point(SnakeGameEngine.GridWidth / 2 + 1, SnakeGameEngine.GridHeight / 2));
        engine.Tick();
        Assert.Equal(4, engine.Snake.Count);

        engine.StartNewGame();
        Assert.Equal(3, engine.Snake.Count);
    }

    [Fact]
    public void StartNewGame_Clears_GameOver_State()
    {
        var engine = new SnakeGameEngine();
        // Drive snake into left wall
        engine.TrySetDirection(Direction.Left);
        for (int i = 0; i < SnakeGameEngine.GridWidth; i++) engine.Tick();
        Assert.True(engine.IsGameOver);

        engine.StartNewGame();
        Assert.False(engine.IsGameOver);
    }

    [Fact]
    public void StartNewGame_Clears_Paused_State()
    {
        var engine = new SnakeGameEngine();
        engine.TogglePause();
        Assert.True(engine.IsPaused);

        engine.StartNewGame();
        Assert.False(engine.IsPaused);
    }

    [Fact]
    public void StartNewGame_Resets_Direction_To_Right()
    {
        var engine = new SnakeGameEngine();
        engine.TrySetDirection(Direction.Up);
        engine.Tick();
        Assert.Equal(Direction.Up, engine.Direction);

        engine.StartNewGame();
        Assert.Equal(Direction.Right, engine.Direction);
        Assert.Equal(Direction.Right, engine.NextDirection);
    }

    [Fact]
    public void StartNewGame_Resets_TimerInterval()
    {
        var engine = new SnakeGameEngine();
        // Eat enough food to lower the interval
        for (int i = 0; i < 10; i++)
        {
            var nextHeadX = engine.Snake[0].X + 1;
            var nextHeadY = engine.Snake[0].Y;
            if (nextHeadX < SnakeGameEngine.GridWidth)
                engine.PlaceFood(new Point(nextHeadX, nextHeadY));
            engine.Tick();
        }
        Assert.True(engine.TimerInterval < SnakeGameEngine.InitialTimerInterval);

        engine.StartNewGame();
        Assert.Equal(SnakeGameEngine.InitialTimerInterval, engine.TimerInterval);
    }

    [Fact]
    public void StartNewGame_Places_Food_Outside_Snake()
    {
        var engine = new SnakeGameEngine();
        engine.StartNewGame();
        Assert.DoesNotContain(engine.Food, engine.Snake);
    }
}

// ---------------------------------------------------------------------------
// HitsWall (static)
// ---------------------------------------------------------------------------

public class HitsWall_Tests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-1, -1)]
    [InlineData(SnakeGameEngine.GridWidth, 0)]
    [InlineData(0, SnakeGameEngine.GridHeight)]
    [InlineData(SnakeGameEngine.GridWidth, SnakeGameEngine.GridHeight)]
    [InlineData(SnakeGameEngine.GridWidth + 5, 5)]
    [InlineData(5, SnakeGameEngine.GridHeight + 5)]
    public void HitsWall_Returns_True_For_OutOfBounds(int x, int y)
    {
        Assert.True(SnakeGameEngine.HitsWall(new Point(x, y)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(SnakeGameEngine.GridWidth - 1, 0)]
    [InlineData(0, SnakeGameEngine.GridHeight - 1)]
    [InlineData(SnakeGameEngine.GridWidth - 1, SnakeGameEngine.GridHeight - 1)]
    [InlineData(5, 5)]
    [InlineData(SnakeGameEngine.GridWidth / 2, SnakeGameEngine.GridHeight / 2)]
    public void HitsWall_Returns_False_For_Valid_Positions(int x, int y)
    {
        Assert.False(SnakeGameEngine.HitsWall(new Point(x, y)));
    }
}

// ---------------------------------------------------------------------------
// IsOpposite (static)
// ---------------------------------------------------------------------------

public class IsOpposite_Tests
{
    [Theory]
    [InlineData(Direction.Up,    Direction.Down)]
    [InlineData(Direction.Down,  Direction.Up)]
    [InlineData(Direction.Left,  Direction.Right)]
    [InlineData(Direction.Right, Direction.Left)]
    public void IsOpposite_Returns_True_For_Opposite_Directions(Direction current, Direction proposed)
    {
        Assert.True(SnakeGameEngine.IsOpposite(current, proposed));
    }

    [Theory]
    [InlineData(Direction.Up,    Direction.Up)]
    [InlineData(Direction.Up,    Direction.Left)]
    [InlineData(Direction.Up,    Direction.Right)]
    [InlineData(Direction.Down,  Direction.Down)]
    [InlineData(Direction.Down,  Direction.Left)]
    [InlineData(Direction.Down,  Direction.Right)]
    [InlineData(Direction.Left,  Direction.Left)]
    [InlineData(Direction.Left,  Direction.Up)]
    [InlineData(Direction.Left,  Direction.Down)]
    [InlineData(Direction.Right, Direction.Right)]
    [InlineData(Direction.Right, Direction.Up)]
    [InlineData(Direction.Right, Direction.Down)]
    public void IsOpposite_Returns_False_For_NonOpposite_Directions(Direction current, Direction proposed)
    {
        Assert.False(SnakeGameEngine.IsOpposite(current, proposed));
    }
}

// ---------------------------------------------------------------------------
// GetNextHead (static)
// ---------------------------------------------------------------------------

public class GetNextHead_Tests
{
    private static readonly Point Origin = new Point(5, 5);

    [Fact]
    public void Up_Decrements_Y()
    {
        var next = SnakeGameEngine.GetNextHead(Origin, Direction.Up);
        Assert.Equal(new Point(5, 4), next);
    }

    [Fact]
    public void Down_Increments_Y()
    {
        var next = SnakeGameEngine.GetNextHead(Origin, Direction.Down);
        Assert.Equal(new Point(5, 6), next);
    }

    [Fact]
    public void Left_Decrements_X()
    {
        var next = SnakeGameEngine.GetNextHead(Origin, Direction.Left);
        Assert.Equal(new Point(4, 5), next);
    }

    [Fact]
    public void Right_Increments_X()
    {
        var next = SnakeGameEngine.GetNextHead(Origin, Direction.Right);
        Assert.Equal(new Point(6, 5), next);
    }
}

// ---------------------------------------------------------------------------
// CellToRect (static)
// ---------------------------------------------------------------------------

public class CellToRect_Tests
{
    [Fact]
    public void Origin_Cell_Maps_To_Zero_Offset()
    {
        var rect = SnakeGameEngine.CellToRect(new Point(0, 0));
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(SnakeGameEngine.CellSize, rect.Width);
        Assert.Equal(SnakeGameEngine.CellSize, rect.Height);
    }

    [Fact]
    public void Cell_Maps_To_Correct_Pixel_Coordinates()
    {
        var rect = SnakeGameEngine.CellToRect(new Point(3, 4));
        Assert.Equal(3 * SnakeGameEngine.CellSize, rect.X);
        Assert.Equal(4 * SnakeGameEngine.CellSize, rect.Y);
        Assert.Equal(SnakeGameEngine.CellSize, rect.Width);
        Assert.Equal(SnakeGameEngine.CellSize, rect.Height);
    }
}

// ---------------------------------------------------------------------------
// Tick – normal movement
// ---------------------------------------------------------------------------

public class Tick_Movement_Tests
{
    [Fact]
    public void Tick_Returns_Skipped_When_GameOver()
    {
        var engine = new SnakeGameEngine();
        // Drive into the right wall
        for (int i = 0; i < SnakeGameEngine.GridWidth; i++) engine.Tick();
        Assert.True(engine.IsGameOver);

        var result = engine.Tick();
        Assert.Equal(TickResult.Skipped, result);
    }

    [Fact]
    public void Tick_Returns_Skipped_When_Paused()
    {
        var engine = new SnakeGameEngine();
        engine.TogglePause();
        var result = engine.Tick();
        Assert.Equal(TickResult.Skipped, result);
    }

    [Fact]
    public void Tick_Returns_Moved_On_Normal_Step()
    {
        var engine = new SnakeGameEngine();
        // Make sure food is not immediately in front
        engine.PlaceFood(new Point(0, 0));
        var result = engine.Tick();
        Assert.Equal(TickResult.Moved, result);
    }

    [Fact]
    public void Tick_Moves_Head_In_Current_Direction()
    {
        var engine = new SnakeGameEngine();
        engine.PlaceFood(new Point(0, 0)); // avoid eating food
        var oldHead = engine.Snake[0];

        engine.Tick();

        var newHead = engine.Snake[0];
        Assert.Equal(oldHead.X + 1, newHead.X); // moved Right
        Assert.Equal(oldHead.Y, newHead.Y);
    }

    [Fact]
    public void Tick_Removes_Tail_So_Length_Is_Unchanged()
    {
        var engine = new SnakeGameEngine();
        engine.PlaceFood(new Point(0, 0));
        var lengthBefore = engine.Snake.Count;

        engine.Tick();

        Assert.Equal(lengthBefore, engine.Snake.Count);
    }

    [Fact]
    public void Tick_Updates_Direction_From_NextDirection()
    {
        var engine = new SnakeGameEngine();
        engine.TrySetDirection(Direction.Up);
        engine.PlaceFood(new Point(0, 0));

        engine.Tick();

        Assert.Equal(Direction.Up, engine.Direction);
    }
}

// ---------------------------------------------------------------------------
// Tick – food / scoring / speed
// ---------------------------------------------------------------------------

public class Tick_Food_Tests
{
    private static SnakeGameEngine EngineWithFoodAhead()
    {
        var engine = new SnakeGameEngine();
        // Head is at (GridWidth/2, GridHeight/2), moving Right.
        // Place food one cell to the right of the head.
        var head = engine.Snake[0];
        engine.PlaceFood(new Point(head.X + 1, head.Y));
        return engine;
    }

    [Fact]
    public void Tick_Returns_AteFood_When_Head_Reaches_Food()
    {
        var engine = EngineWithFoodAhead();
        var result = engine.Tick();
        Assert.Equal(TickResult.AteFood, result);
    }

    [Fact]
    public void Tick_Increments_Score_When_Food_Eaten()
    {
        var engine = EngineWithFoodAhead();
        engine.Tick();
        Assert.Equal(1, engine.Score);
    }

    [Fact]
    public void Tick_Grows_Snake_When_Food_Eaten()
    {
        var engine = EngineWithFoodAhead();
        var lengthBefore = engine.Snake.Count;
        engine.Tick();
        Assert.Equal(lengthBefore + 1, engine.Snake.Count);
    }

    [Fact]
    public void Tick_Decreases_TimerInterval_When_Food_Eaten()
    {
        var engine = EngineWithFoodAhead();
        var intervalBefore = engine.TimerInterval;
        engine.Tick();
        Assert.True(engine.TimerInterval < intervalBefore);
    }

    [Fact]
    public void Tick_TimerInterval_Does_Not_Go_Below_Minimum()
    {
        var engine = new SnakeGameEngine();
        // Eat food repeatedly to push interval to the floor
        for (int i = 0; i < 100; i++)
        {
            var head = engine.Snake[0];
            var nextX = head.X + 1;
            if (nextX >= SnakeGameEngine.GridWidth) break;
            engine.PlaceFood(new Point(nextX, head.Y));
            engine.Tick();
        }
        Assert.True(engine.TimerInterval >= SnakeGameEngine.MinTimerInterval);
    }

    [Fact]
    public void Tick_Spawns_New_Food_After_Eating()
    {
        var engine = EngineWithFoodAhead();
        var oldFood = engine.Food;
        engine.Tick();
        // Food must have moved (or at least be off the snake)
        Assert.DoesNotContain(engine.Food, engine.Snake);
    }

    [Fact]
    public void Score_Increases_By_One_Per_Food()
    {
        var engine = new SnakeGameEngine();
        for (int meal = 1; meal <= 3; meal++)
        {
            var head = engine.Snake[0];
            var nextX = head.X + 1;
            if (nextX >= SnakeGameEngine.GridWidth) break;
            engine.PlaceFood(new Point(nextX, head.Y));
            engine.Tick();
            Assert.Equal(meal, engine.Score);
        }
    }
}

// ---------------------------------------------------------------------------
// Tick – collision / game over
// ---------------------------------------------------------------------------

public class Tick_Collision_Tests
{
    [Fact]
    public void Tick_Returns_HitWall_On_Right_Wall()
    {
        var engine = new SnakeGameEngine();
        // Keep moving right until we hit the wall; avoid eating any food
        TickResult lastResult = TickResult.Moved;
        for (int i = 0; i < SnakeGameEngine.GridWidth; i++)
        {
            engine.PlaceFood(new Point(0, 0)); // keep food away
            lastResult = engine.Tick();
            if (lastResult == TickResult.HitWall) break;
        }
        Assert.Equal(TickResult.HitWall, lastResult);
    }

    [Fact]
    public void Tick_Returns_HitWall_On_Left_Wall()
    {
        var engine = new SnakeGameEngine();
        engine.TrySetDirection(Direction.Left);
        TickResult lastResult = TickResult.Moved;
        for (int i = 0; i < SnakeGameEngine.GridWidth; i++)
        {
            engine.PlaceFood(new Point(SnakeGameEngine.GridWidth - 1, SnakeGameEngine.GridHeight - 1));
            lastResult = engine.Tick();
            if (lastResult == TickResult.HitWall) break;
        }
        Assert.Equal(TickResult.HitWall, lastResult);
    }

    [Fact]
    public void Tick_Returns_HitWall_On_Top_Wall()
    {
        var engine = new SnakeGameEngine();
        engine.TrySetDirection(Direction.Up);
        TickResult lastResult = TickResult.Moved;
        for (int i = 0; i < SnakeGameEngine.GridHeight; i++)
        {
            engine.PlaceFood(new Point(SnakeGameEngine.GridWidth - 1, SnakeGameEngine.GridHeight - 1));
            lastResult = engine.Tick();
            if (lastResult == TickResult.HitWall) break;
        }
        Assert.Equal(TickResult.HitWall, lastResult);
    }

    [Fact]
    public void Tick_Returns_HitWall_On_Bottom_Wall()
    {
        var engine = new SnakeGameEngine();
        engine.TrySetDirection(Direction.Down);
        TickResult lastResult = TickResult.Moved;
        for (int i = 0; i < SnakeGameEngine.GridHeight; i++)
        {
            engine.PlaceFood(new Point(0, 0));
            lastResult = engine.Tick();
            if (lastResult == TickResult.HitWall) break;
        }
        Assert.Equal(TickResult.HitWall, lastResult);
    }

    [Fact]
    public void Tick_Sets_IsGameOver_On_Wall_Collision()
    {
        var engine = new SnakeGameEngine();
        for (int i = 0; i < SnakeGameEngine.GridWidth; i++)
        {
            engine.PlaceFood(new Point(0, 0));
            engine.Tick();
        }
        Assert.True(engine.IsGameOver);
    }

    [Fact]
    public void Tick_Returns_HitSelf_When_Snake_Runs_Into_Body()
    {
        // Build a U-turn scenario:
        //   Start: head → right, then go up, left, down – nose hits body.
        var engine = new SnakeGameEngine();

        // Grow the snake to have enough length for a loop
        for (int meal = 0; meal < 5; meal++)
        {
            var head = engine.Snake[0];
            var nextX = head.X + 1;
            if (nextX >= SnakeGameEngine.GridWidth) break;
            engine.PlaceFood(new Point(nextX, head.Y));
            engine.Tick();
        }

        // Now perform the U-turn: Up → Left → Down (into the body)
        engine.PlaceFood(new Point(0, 0));
        engine.TrySetDirection(Direction.Up);
        engine.Tick();

        engine.PlaceFood(new Point(0, 0));
        engine.TrySetDirection(Direction.Left);
        engine.Tick();

        // Move left until we're directly above the original body
        engine.PlaceFood(new Point(0, 0));
        engine.Tick(); // one more left step

        engine.PlaceFood(new Point(0, 0));
        engine.TrySetDirection(Direction.Down);
        var result = engine.Tick();

        Assert.Equal(TickResult.HitSelf, result);
        Assert.True(engine.IsGameOver);
    }
}

// ---------------------------------------------------------------------------
// TogglePause
// ---------------------------------------------------------------------------

public class TogglePause_Tests
{
    [Fact]
    public void TogglePause_Returns_False_When_GameOver()
    {
        var engine = new SnakeGameEngine();
        for (int i = 0; i < SnakeGameEngine.GridWidth; i++)
        {
            engine.PlaceFood(new Point(0, 0));
            engine.Tick();
        }
        Assert.True(engine.IsGameOver);

        var result = engine.TogglePause();
        Assert.False(result);
        Assert.False(engine.IsPaused);
    }

    [Fact]
    public void TogglePause_Sets_IsPaused_True_First_Call()
    {
        var engine = new SnakeGameEngine();
        var result = engine.TogglePause();
        Assert.True(result);
        Assert.True(engine.IsPaused);
    }

    [Fact]
    public void TogglePause_Sets_IsPaused_False_Second_Call()
    {
        var engine = new SnakeGameEngine();
        engine.TogglePause();
        var result = engine.TogglePause();
        Assert.True(result);
        Assert.False(engine.IsPaused);
    }

    [Fact]
    public void TogglePause_Blocks_Tick_While_Paused()
    {
        var engine = new SnakeGameEngine();
        engine.PlaceFood(new Point(0, 0));
        var headBefore = engine.Snake[0];

        engine.TogglePause();
        engine.Tick();

        Assert.Equal(headBefore, engine.Snake[0]);
    }
}

// ---------------------------------------------------------------------------
// TrySetDirection
// ---------------------------------------------------------------------------

public class TrySetDirection_Tests
{
    [Fact]
    public void TrySetDirection_Returns_False_When_GameOver()
    {
        var engine = new SnakeGameEngine();
        for (int i = 0; i < SnakeGameEngine.GridWidth; i++)
        {
            engine.PlaceFood(new Point(0, 0));
            engine.Tick();
        }
        Assert.True(engine.IsGameOver);

        var result = engine.TrySetDirection(Direction.Up);
        Assert.False(result);
    }

    [Fact]
    public void TrySetDirection_Returns_False_When_Paused()
    {
        var engine = new SnakeGameEngine();
        engine.TogglePause();
        var result = engine.TrySetDirection(Direction.Up);
        Assert.False(result);
    }

    [Theory]
    [InlineData(Direction.Right, Direction.Left)]
    [InlineData(Direction.Left,  Direction.Right)]
    [InlineData(Direction.Up,    Direction.Down)]
    [InlineData(Direction.Down,  Direction.Up)]
    public void TrySetDirection_Returns_False_For_Opposite_Direction(Direction current, Direction proposed)
    {
        // Create an engine and drive it to the specified current direction
        var engine = new SnakeGameEngine();
        // Move snake to the appropriate non-reverse direction first
        SetDirectionAndTick(engine, current);

        var result = engine.TrySetDirection(proposed);
        Assert.False(result);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    // Note: Left is opposite to Right (the initial direction) so it is excluded here.
    public void TrySetDirection_Returns_True_For_Valid_NonOpposite_Direction(Direction proposed)
    {
        // Engine starts moving Right; Up/Down/Left are all valid
        var engine = new SnakeGameEngine();
        var result = engine.TrySetDirection(proposed);
        Assert.True(result);
        Assert.Equal(proposed, engine.NextDirection);
    }

    [Fact]
    public void TrySetDirection_Same_Direction_Is_Accepted()
    {
        var engine = new SnakeGameEngine();
        var result = engine.TrySetDirection(Direction.Right); // same as current
        Assert.True(result);
    }

    [Fact]
    public void NextDirection_Is_Applied_On_Next_Tick()
    {
        var engine = new SnakeGameEngine();
        engine.TrySetDirection(Direction.Up);
        engine.PlaceFood(new Point(0, 0));
        engine.Tick();
        Assert.Equal(Direction.Up, engine.Direction);
    }

    // Helper: navigate the engine to a given direction without reversing
    private static void SetDirectionAndTick(SnakeGameEngine engine, Direction target)
    {
        // The engine starts going Right.
        // For Left we need to go Up/Down first, then Left.
        if (target == Direction.Left)
        {
            engine.TrySetDirection(Direction.Up);
            engine.PlaceFood(new Point(0, 0));
            engine.Tick();
            engine.TrySetDirection(Direction.Left);
            engine.PlaceFood(new Point(0, 0));
            engine.Tick();
        }
        else if (target != Direction.Right)
        {
            engine.TrySetDirection(target);
            engine.PlaceFood(new Point(0, 0));
            engine.Tick();
        }
        // For Right: already going Right, nothing to do.
    }
}

// ---------------------------------------------------------------------------
// SpawnFood / PlaceFood
// ---------------------------------------------------------------------------

public class Food_Tests
{
    [Fact]
    public void SpawnFood_Never_Places_Food_On_Snake()
    {
        // Run many times to exercise the "retry until free" loop
        var engine = new SnakeGameEngine(new Random(0));
        for (int i = 0; i < 500; i++)
        {
            engine.SpawnFood();
            Assert.DoesNotContain(engine.Food, engine.Snake);
        }
    }

    [Fact]
    public void PlaceFood_Sets_Food_At_Given_Position()
    {
        var engine = new SnakeGameEngine();
        var target = new Point(0, 0);
        engine.PlaceFood(target);
        Assert.Equal(target, engine.Food);
    }

    [Fact]
    public void PlaceFood_Throws_When_Position_Is_On_Snake()
    {
        var engine = new SnakeGameEngine();
        var snakeCell = engine.Snake[0];
        Assert.Throws<InvalidOperationException>(() => engine.PlaceFood(snakeCell));
    }

    [Fact]
    public void Food_Stays_Within_Grid_After_SpawnFood()
    {
        var engine = new SnakeGameEngine(new Random(42));
        for (int i = 0; i < 200; i++)
        {
            engine.SpawnFood();
            Assert.False(SnakeGameEngine.HitsWall(engine.Food),
                $"Food spawned at {engine.Food} which is outside the grid.");
        }
    }

    [Fact]
    public void SequenceRandom_Retries_Until_Food_Off_Snake()
    {
        // The snake occupies (12,9), (11,9), (10,9).
        // Provide coords that overlap the snake first, then a free cell.
        int headX = SnakeGameEngine.GridWidth / 2;    // 12
        int headY = SnakeGameEngine.GridHeight / 2;   // 9

        var seeded = new SequenceRandom(
            headX, headY,   // first candidate → on snake head → retry
            0, 0            // second candidate → free
        );
        var engine = new SnakeGameEngine(seeded);
        // After construction SpawnFood already consumed the sequence.
        // The food should be at (0,0) after the retry.
        Assert.Equal(new Point(0, 0), engine.Food);
    }
}
