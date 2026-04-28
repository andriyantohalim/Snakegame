using SnakeGame.Core;
using System.Drawing.Drawing2D;

ApplicationConfiguration.Initialize();
Application.Run(new SnakeForm());

sealed class SnakeForm : Form
{
    private readonly SnakeGameEngine _engine = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Label _scoreLabel;
    private readonly Label _statusLabel;
    private readonly Label _helpLabel;
    private readonly Button _restartButton;
    private readonly Button _pauseButton;
    private readonly DoubleBufferedPanel _gamePanel;

    public SnakeForm()
    {
        Text = "Snake Game (.NET UI)";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(250, 252, 255);
        ClientSize = new Size(SnakeGameEngine.GridWidth * SnakeGameEngine.CellSize + 24,
                              SnakeGameEngine.GridHeight * SnakeGameEngine.CellSize + 136);

        _scoreLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 12),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 52, 82),
            Text = "Score: 0"
        };

        _statusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 40),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(46, 125, 50),
            Text = "Running"
        };

        _helpLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 64),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(85, 100, 124),
            Text = "Use arrow keys to move. Press P to pause/resume and R to restart."
        };

        _restartButton = new Button
        {
            Location = new Point(ClientSize.Width - 208, 14),
            Size = new Size(94, 30),
            Text = "Restart",
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _restartButton.Click += (_, _) => StartNewGame();

        _pauseButton = new Button
        {
            Location = new Point(ClientSize.Width - 110, 14),
            Size = new Size(94, 30),
            Text = "Pause",
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _pauseButton.Click += (_, _) => TogglePause();

        _gamePanel = new DoubleBufferedPanel
        {
            Location = new Point(12, 96),
            Size = new Size(SnakeGameEngine.GridWidth * SnakeGameEngine.CellSize,
                            SnakeGameEngine.GridHeight * SnakeGameEngine.CellSize),
            BackColor = Color.White
        };
        _gamePanel.Paint += OnGamePanelPaint;

        Controls.Add(_scoreLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_helpLabel);
        Controls.Add(_restartButton);
        Controls.Add(_pauseButton);
        Controls.Add(_gamePanel);

        _timer.Tick += (_, _) => TickGame();

        StartNewGame();
    }

    private void StartNewGame()
    {
        _engine.StartNewGame();
        _timer.Interval = _engine.TimerInterval;
        UpdateScore();
        UpdateGameStateUi("Running");
        _gamePanel.Invalidate();
        _timer.Start();
        _gamePanel.Focus();
    }

    private void TickGame()
    {
        var result = _engine.Tick();

        switch (result)
        {
            case TickResult.HitWall:
                EndGame("Game over: wall collision.");
                return;
            case TickResult.HitSelf:
                EndGame("Game over: you hit yourself.");
                return;
            case TickResult.AteFood:
                _timer.Interval = _engine.TimerInterval;
                UpdateScore();
                break;
        }

        _gamePanel.Invalidate();
    }

    private void EndGame(string message)
    {
        _timer.Stop();
        UpdateGameStateUi(message);
        _gamePanel.Invalidate();
    }

    private void TogglePause()
    {
        if (!_engine.TogglePause())
            return;

        if (_engine.IsPaused)
        {
            _timer.Stop();
            UpdateGameStateUi("Paused");
        }
        else
        {
            _timer.Start();
            UpdateGameStateUi("Running");
        }

        _gamePanel.Focus();
    }

    private void UpdateScore()
    {
        _scoreLabel.Text = $"Score: {_engine.Score}";
    }

    private void UpdateGameStateUi(string statusText)
    {
        _statusLabel.Text = statusText;
        _statusLabel.ForeColor = _engine.IsGameOver
            ? Color.FromArgb(198, 40, 40)
            : _engine.IsPaused
                ? Color.FromArgb(239, 108, 0)
                : Color.FromArgb(46, 125, 50);

        _pauseButton.Enabled = !_engine.IsGameOver;
        _pauseButton.Text = _engine.IsPaused ? "Resume" : "Pause";
    }

    // Intercept command keys before focused controls use arrows for focus navigation.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        return TryHandleGameKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
    }

    private bool TryHandleGameKey(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;

        if (keyCode == Keys.R)
        {
            StartNewGame();
            return true;
        }

        if (keyCode == Keys.P)
        {
            TogglePause();
            return true;
        }

        if (keyCode is not (Keys.Up or Keys.Down or Keys.Left or Keys.Right))
        {
            return false;
        }

        if (_engine.IsGameOver || _engine.IsPaused)
        {
            return true;
        }

        var proposed = keyCode switch
        {
            Keys.Up => Direction.Up,
            Keys.Down => Direction.Down,
            Keys.Left => Direction.Left,
            Keys.Right => Direction.Right,
            _ => throw new InvalidOperationException($"Unexpected game key: {keyCode}.")
        };

        _engine.TrySetDirection(proposed);
        return true;
    }

    private void OnGamePanelPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(242, 246, 252));

        using var gridPen = new Pen(Color.FromArgb(226, 234, 246));
        for (var x = 0; x <= SnakeGameEngine.GridWidth; x++)
        {
            g.DrawLine(gridPen, x * SnakeGameEngine.CellSize, 0,
                                x * SnakeGameEngine.CellSize, SnakeGameEngine.GridHeight * SnakeGameEngine.CellSize);
        }

        for (var y = 0; y <= SnakeGameEngine.GridHeight; y++)
        {
            g.DrawLine(gridPen, 0, y * SnakeGameEngine.CellSize,
                                SnakeGameEngine.GridWidth * SnakeGameEngine.CellSize, y * SnakeGameEngine.CellSize);
        }

        using var foodBrush = new SolidBrush(Color.FromArgb(234, 67, 53));
        var foodRect = SnakeGameEngine.CellToRect(_engine.Food);
        foodRect.Inflate(-3, -3);
        g.FillEllipse(foodBrush, foodRect);

        var snake = _engine.Snake;
        for (var i = snake.Count - 1; i >= 0; i--)
        {
            var segment = snake[i];
            var rect = SnakeGameEngine.CellToRect(segment);
            rect.Inflate(-2, -2);

            var color = i == 0 ? Color.FromArgb(46, 125, 50) : Color.FromArgb(102, 187, 106);
            using var snakeBrush = new SolidBrush(color);
            g.FillRoundedRectangle(snakeBrush, rect, 6);
        }

        using var borderPen = new Pen(Color.FromArgb(162, 184, 216), 2);
        g.DrawRectangle(borderPen, 1, 1,
            SnakeGameEngine.GridWidth * SnakeGameEngine.CellSize - 2,
            SnakeGameEngine.GridHeight * SnakeGameEngine.CellSize - 2);

        if (_engine.IsGameOver || _engine.IsPaused)
        {
            var overlayColor = _engine.IsGameOver
                ? Color.FromArgb(140, 10, 20, 35)
                : Color.FromArgb(110, 32, 52, 82);
            var heading = _engine.IsGameOver ? "Game Over" : "Paused";
            var detail = _engine.IsGameOver ? "Press Restart or R to play again." : "Press Resume or P to keep going.";

            using var overlayBrush = new SolidBrush(overlayColor);
            g.FillRectangle(overlayBrush, 0, 0, _gamePanel.Width, _gamePanel.Height);

            using var textBrush = new SolidBrush(Color.White);
            using var headingFont = new Font("Segoe UI", 16, FontStyle.Bold);
            using var detailFont = new Font("Segoe UI", 10, FontStyle.Regular);

            var headingSize = g.MeasureString(heading, headingFont);
            var detailSize = g.MeasureString(detail, detailFont);
            var centerY = _gamePanel.Height / 2f;

            g.DrawString(heading, headingFont, textBrush, (_gamePanel.Width - headingSize.Width) / 2, centerY - headingSize.Height);
            g.DrawString(detail, detailFont, textBrush, (_gamePanel.Width - detailSize.Width) / 2, centerY + 6);
        }
    }
}

sealed class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
    }
}

static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        graphics.FillPath(brush, path);
    }
}
