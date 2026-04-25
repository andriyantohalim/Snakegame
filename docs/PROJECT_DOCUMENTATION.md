# SnakeGame Documentation

## Overview

SnakeGame is a Windows-only desktop game built with WinForms on .NET 9. The project keeps the implementation compact by placing the application entry point, UI setup, gameplay rules, and rendering logic in a single source file: `SnakeGame/Program.cs`.

## Gameplay Summary

- The snake starts near the center of the board and moves automatically.
- The player guides the snake with the arrow keys.
- Eating food increases the score and grows the snake.
- Each food pickup slightly increases the game speed.
- Hitting a wall or the snake's own body ends the game.
- The player can pause with `P` and restart with `R` or the restart button.

## Technical Stack

- Language: C#
- UI framework: Windows Forms
- Target framework: `net9.0-windows`
- Rendering approach: Custom drawing inside a double-buffered panel

## Code Structure

### `SnakeGame/Program.cs`

This file contains the entire runtime behavior of the app:

- `ApplicationConfiguration.Initialize()` and `Application.Run(new SnakeForm())` bootstrap the application.
- `Direction` defines the possible movement states.
- `SnakeForm` owns the UI, timer, game state, input handling, collision checks, scoring, and painting.
- `DoubleBufferedPanel` reduces flicker during redraws.
- `GraphicsExtensions.FillRoundedRectangle(...)` renders rounded snake segments.

### `SnakeGame/SnakeGame.csproj`

The project file configures the application as a WinForms desktop executable and enables nullable reference types and implicit usings.

## Game Loop Design

The game loop is driven by a WinForms `Timer`:

1. The timer triggers `TickGame()`.
2. The next head position is computed from the queued direction.
3. Wall and self-collision checks run before the move is finalized.
4. The snake either grows when it reaches food or drops its tail segment for a normal move.
5. The score and timer interval update after eating food.
6. The playfield is invalidated so the panel repaints with the new state.

This design keeps the runtime logic easy to follow and suitable for a small desktop game.

## Rendering Notes

The game board is drawn manually in `OnGamePanelPaint(...)`:

- A light grid is painted first.
- Food is rendered as a red ellipse.
- Snake segments are drawn as rounded rectangles.
- The snake head uses a darker green to stand out from the body.
- Pause and game-over states draw a semi-transparent overlay with status text.

Using a double-buffered panel helps the game feel smoother by preventing repaint flicker.

## Input Handling

Keyboard input is captured at the form level:

- Arrow keys update the queued movement direction.
- Opposite-direction turns are blocked to prevent instant self-collision from invalid input.
- `P` toggles pause and resume.
- `R` starts a fresh game.

## Build and Run

### Development run

```powershell
dotnet run --project SnakeGame/SnakeGame.csproj
```

### Release build

```powershell
dotnet build SnakeGame/SnakeGame.csproj -c Release
```

### Publish a self-contained executable

```powershell
dotnet publish SnakeGame/SnakeGame.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/publish/win-x64
```

## Maintenance Notes

- The project is intentionally small and easy to extend, but `Program.cs` already combines several responsibilities.
- If the game grows, the next clean refactor would be splitting gameplay state, rendering helpers, and UI controls into separate files.
- Because the target framework is `net9.0-windows`, builds and runtime behavior are tied to Windows.

## Possible Improvements

- Add a start screen and high-score persistence
- Separate game logic from presentation for easier testing
- Add sound effects and difficulty settings
- Support custom board sizes or themes
- Add unit tests around movement and collision rules
