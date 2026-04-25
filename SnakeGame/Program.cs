using System.Drawing.Drawing2D;

ApplicationConfiguration.Initialize();
Application.Run(new SnakeForm());

enum Direction
{
    Up,
    Down,
    Left,
    Right
}

sealed class SnakeForm : Form
{
    private const int GridWidth = 24;
    private const int GridHeight = 18;
    private const int CellSize = 24;

    private readonly Random _random = new();
    private readonly List<Point> _snake = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Label _scoreLabel;
    private readonly Label _statusLabel;
    private readonly Label _helpLabel;
    private readonly Button _restartButton;
    private readonly Button _pauseButton;
    private readonly DoubleBufferedPanel _gamePanel;

    private Direction _direction = Direction.Right;
    private Direction _nextDirection = Direction.Right;
    private Point _food;
    private int _score;
    private bool _isPaused;
    private bool _isGameOver;

    public SnakeForm()
    {
        Text = "Snake Game (.NET UI)";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(250, 252, 255);
        ClientSize = new Size(GridWidth * CellSize + 24, GridHeight * CellSize + 136);

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
            Size = new Size(GridWidth * CellSize, GridHeight * CellSize),
            BackColor = Color.White
        };
        _gamePanel.Paint += OnGamePanelPaint;

        Controls.Add(_scoreLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_helpLabel);
        Controls.Add(_restartButton);
        Controls.Add(_pauseButton);
        Controls.Add(_gamePanel);

        KeyDown += OnFormKeyDown;

        _timer.Interval = 110;
        _timer.Tick += (_, _) => TickGame();

        StartNewGame();
    }

    private void StartNewGame()
    {
        _snake.Clear();
        _snake.Add(new Point(GridWidth / 2, GridHeight / 2));
        _snake.Add(new Point(GridWidth / 2 - 1, GridHeight / 2));
        _snake.Add(new Point(GridWidth / 2 - 2, GridHeight / 2));

        _direction = Direction.Right;
        _nextDirection = Direction.Right;
        _score = 0;
        _isPaused = false;
        _isGameOver = false;
        _timer.Interval = 110;

        SpawnFood();
        UpdateScore();
        UpdateGameStateUi("Running");
        _gamePanel.Invalidate();
        _timer.Start();
        _gamePanel.Focus();
    }

    private void TickGame()
    {
        if (_isGameOver || _isPaused)
        {
            return;
        }

        _direction = _nextDirection;
        var head = _snake[0];

        var nextHead = _direction switch
        {
            Direction.Up => new Point(head.X, head.Y - 1),
            Direction.Down => new Point(head.X, head.Y + 1),
            Direction.Left => new Point(head.X - 1, head.Y),
            Direction.Right => new Point(head.X + 1, head.Y),
            _ => head
        };

        if (HitsWall(nextHead))
        {
            EndGame("Game over: wall collision.");
            return;
        }

        var grows = nextHead == _food;
        var bodyCountToCheck = grows ? _snake.Count : _snake.Count - 1;
        if (_snake.Take(bodyCountToCheck).Contains(nextHead))
        {
            EndGame("Game over: you hit yourself.");
            return;
        }

        _snake.Insert(0, nextHead);

        if (grows)
        {
            _score++;
            _timer.Interval = Math.Max(65, _timer.Interval - 3);
            SpawnFood();
            UpdateScore();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }

        _gamePanel.Invalidate();
    }

    private void EndGame(string message)
    {
        _isPaused = false;
        _isGameOver = true;
        _timer.Stop();
        UpdateGameStateUi(message);
        _gamePanel.Invalidate();
    }

    private void TogglePause()
    {
        if (_isGameOver)
        {
            return;
        }

        _isPaused = !_isPaused;
        if (_isPaused)
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

    private bool HitsWall(Point point)
    {
        return point.X < 0 || point.X >= GridWidth || point.Y < 0 || point.Y >= GridHeight;
    }

    private void SpawnFood()
    {
        do
        {
            _food = new Point(_random.Next(GridWidth), _random.Next(GridHeight));
        } while (_snake.Contains(_food));
    }

    private void UpdateScore()
    {
        _scoreLabel.Text = $"Score: {_score}";
    }

    private void UpdateGameStateUi(string statusText)
    {
        _statusLabel.Text = statusText;
        _statusLabel.ForeColor = _isGameOver
            ? Color.FromArgb(198, 40, 40)
            : _isPaused
                ? Color.FromArgb(239, 108, 0)
                : Color.FromArgb(46, 125, 50);

        _pauseButton.Enabled = !_isGameOver;
        _pauseButton.Text = _isPaused ? "Resume" : "Pause";
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.R)
        {
            StartNewGame();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.P)
        {
            TogglePause();
            e.Handled = true;
            return;
        }

        if (_isGameOver || _isPaused)
        {
            return;
        }

        var proposed = e.KeyCode switch
        {
            Keys.Up => Direction.Up,
            Keys.Down => Direction.Down,
            Keys.Left => Direction.Left,
            Keys.Right => Direction.Right,
            _ => _nextDirection
        };

        if (!IsOpposite(_direction, proposed))
        {
            _nextDirection = proposed;
        }
    }

    private static bool IsOpposite(Direction current, Direction proposed)
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

    private void OnGamePanelPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(242, 246, 252));

        using var gridPen = new Pen(Color.FromArgb(226, 234, 246));
        for (var x = 0; x <= GridWidth; x++)
        {
            g.DrawLine(gridPen, x * CellSize, 0, x * CellSize, GridHeight * CellSize);
        }

        for (var y = 0; y <= GridHeight; y++)
        {
            g.DrawLine(gridPen, 0, y * CellSize, GridWidth * CellSize, y * CellSize);
        }

        using var foodBrush = new SolidBrush(Color.FromArgb(234, 67, 53));
        var foodRect = CellToRect(_food);
        foodRect.Inflate(-3, -3);
        g.FillEllipse(foodBrush, foodRect);

        for (var i = _snake.Count - 1; i >= 0; i--)
        {
            var segment = _snake[i];
            var rect = CellToRect(segment);
            rect.Inflate(-2, -2);

            var color = i == 0 ? Color.FromArgb(46, 125, 50) : Color.FromArgb(102, 187, 106);
            using var snakeBrush = new SolidBrush(color);
            g.FillRoundedRectangle(snakeBrush, rect, 6);
        }

        using var borderPen = new Pen(Color.FromArgb(162, 184, 216), 2);
        g.DrawRectangle(borderPen, 1, 1, GridWidth * CellSize - 2, GridHeight * CellSize - 2);

        if (_isGameOver || _isPaused)
        {
            var overlayColor = _isGameOver
                ? Color.FromArgb(140, 10, 20, 35)
                : Color.FromArgb(110, 32, 52, 82);
            var heading = _isGameOver ? "Game Over" : "Paused";
            var detail = _isGameOver ? "Press Restart or R to play again." : "Press Resume or P to keep going.";

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

    private static Rectangle CellToRect(Point point)
    {
        return new Rectangle(point.X * CellSize, point.Y * CellSize, CellSize, CellSize);
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
