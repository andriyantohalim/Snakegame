# SnakeGame

SnakeGame is a lightweight Snake implementation built with .NET 9 and Windows Forms. It provides a simple desktop UI, keyboard controls, score tracking, pause and restart actions, and a progressively faster game loop as the score increases.

## Features

- Classic snake gameplay on a fixed grid
- Keyboard-driven movement with opposite-direction protection
- Pause and resume support with `P`
- Quick restart with `R` or the on-screen restart button
- Score tracking and increasing game speed as food is collected
- Clean WinForms interface with a custom-rendered playfield

## Requirements

- Windows
- .NET 9 SDK

## Run Locally

```powershell
dotnet run --project SnakeGame/SnakeGame.csproj
```

The application opens as a desktop window and starts a new game immediately.

## Controls

- `Up`, `Down`, `Left`, `Right`: Move the snake
- `P`: Pause or resume
- `R`: Restart the game

## Build a Release Executable

```powershell
dotnet publish SnakeGame/SnakeGame.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/publish/win-x64
```

## Project Layout

```text
SnakeGame/
  Program.cs              Application entry point, form, game loop, and rendering
  SnakeGame.csproj        Project configuration
docs/
  PROJECT_DOCUMENTATION.md
```

## Documentation

Project details, architecture notes, and maintenance guidance are available in [docs/PROJECT_DOCUMENTATION.md](docs/PROJECT_DOCUMENTATION.md).
