using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace PacMan
{

    // Enum 
    public enum Direction  // Location of the characters in the game
    {
        NoMove,
        MoveUp,
        MoveDown,
        MoveRight,
        MoveLeft,
    }


    // Struct that represent the position of an entity (ghost or player)
    public struct Position
    {
        public int _row;
        public int _col;

        public Position(int row, int col)
        {
            _row = row;
            _col = col;
        }
    }

    // Struct that represent ghost, a ghost has a position, a direction, and an image(uri)
    public struct Ghost
    {
        public Position _position;
        public Direction _direction;
        public string _uri;


        public Ghost(Position position, string uri)
        {
            _direction = Direction.NoMove;
            _position = position;
            _uri = uri;
        }
    }

    // Enum that represent map on the board, any map in the board can be either a wall, food, or empty
    public enum Cell
    {
        Wall,
        Empty,
        Food,
    }

    // Base class for ghosts and pacman
    public class Entity
    {
        public Position _position;
        public Direction _direction;
        public DispatcherTimer _speed = null!;
        public Colors _color = null!;
        public Rectangle _rectangle = null!;
        public RotateTransform _rotateTransform = null!;
    }


    public partial class MainWindow : Window
    {
        // Timers, they are responsible for the game update, we make sure to stop them when game is over
        private readonly DispatcherTimer moveTimerGhosts = new DispatcherTimer();
        private readonly DispatcherTimer mainTimer = new DispatcherTimer();
        private readonly DispatcherTimer timerFinishGame = new DispatcherTimer();


        // need to shift the game a bit down so there will be area to render the text labels
        private static readonly int offSet = 30;

        // The size of a single cell on the screen
        private static readonly int cellSize = 34;

        // Short names for the cells on the map , use them so the board initialization would be shorter and more readable
        private static readonly Cell W = Cell.Wall;
        private static readonly Cell E = Cell.Empty;
        private static readonly Cell F = Cell.Food;

        // Save the board size with variables
        private static readonly int boardRows = 18;
        private static readonly int boardCols = 38;

        // The game board, converted dynamicly to ui elements that will be rendered to screen
        private readonly Cell[,] board = new Cell[18, 38] {
            { W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W ,W ,W ,W ,W ,W, W ,W ,W ,W ,W ,W ,W ,W},
            { W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F, F, F, F ,F ,F ,F ,F ,W},
            { W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F, F, F, F ,F ,F ,F ,F ,W},
            { W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, F, F ,F ,F ,F ,F ,W},
            { W, E ,E ,E ,E ,E, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W ,W ,W ,W ,W ,W, W, E ,E ,E ,E ,E ,W},
            { W, E, E, E, E, E, W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, W, E ,E ,E ,E ,E ,W},
            { W, E, E, E, E, E, W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, W, E ,E ,E ,E ,E ,W},
            { W, E, E, E, E, E, W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, W, E ,E ,E ,E ,E ,W},
            { E, E, E, E, E, E, E, E, E ,E ,E ,E, E, E ,E ,E ,E, E, E ,E ,E ,E, E, E ,E ,E, E ,E ,E ,E ,E, E, E ,E ,E ,E ,E ,E},
            { E, E, E, E, E, E, E, E, E ,E ,E ,E, E, E ,E ,E ,E, E, E ,E ,E ,E, E, E ,E ,E, E ,E ,E ,E ,E, E, E ,E ,E ,E ,E ,E},
            { W, E, E, E, E, E, W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, W, E ,E ,E ,E ,E ,W},
            { W, E, E, E, E, E, W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, W, E ,E ,E ,E ,E ,W},
            { W, E, E, E, E, E, W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, W, E ,E ,E ,E ,E ,W},
            { W, E ,E ,E ,E ,E, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W ,W ,W ,W ,W ,W, W, E ,E ,E ,E ,E ,W},
            { W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, F, F ,F ,F ,F ,F ,W},
            { W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, F, F ,F ,F ,F ,F ,W},
            { W, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F, F ,F ,F ,F ,F ,F, F, F ,F ,F ,F ,F ,W},
            { W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W, W ,W ,W ,W ,W ,W, W, W ,W ,W ,W ,W ,W},
        };

        // Variables for the path finding algorithm of the ghosts
        private static readonly uint maxDistance = 50;
        private readonly uint[,] pathFindingMatrix = new uint[18, 38];

        // The ghosts of the game
        private static Ghost pink = new Ghost(new Position(boardRows / 4 - 3, boardCols - 2), "pink.png");
        private static Ghost cyan = new Ghost(new Position(boardRows / 15 + 15, boardCols - 2), "cyan.png");
        private static Ghost orange = new Ghost(new Position(boardRows / 4 - 3, boardCols - 36), "orange.png");
        private static Ghost red = new Ghost(new Position(boardRows / 15 + 15, boardCols - 36), "red.png");
        private static Ghost brown = new Ghost(new Position(boardRows / 2 - 0, boardCols - 2), "brown.png");
        private static Ghost green = new Ghost(new Position(boardRows / 15 + 8, boardCols - 36), "green.png");
        private readonly Ghost[] ghosts = new Ghost[6] { orange, cyan, brown, pink, red, green };

        // The ui elements of the ghosts
        private readonly Rectangle[] ghostElements = new Rectangle[6];

        // Properties of the player (pacman)
        private Direction playerDirection = Direction.NoMove;
        private Position playerPosition = new Position(boardRows / 2, 19);

        // Current score
        private int score = 0;

        // Game timer (text label on top)
        private void MyLoaded(object sender, RoutedEventArgs e)
        {
            timerFinishGame.Interval = TimeSpan.FromMilliseconds(1000);
            timerFinishGame.Tick += DTimerFinishGame!;
            timerFinishGame.Start();
        }
        private int timerGame = 200;
        private void DTimerFinishGame(object sender, EventArgs e)
        {
            timerGame--;
            TimerLabel.Content = timerGame.ToString();
            if (timerGame == 0)
            {
                GameOver("Time is over,you lose the game!");
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            GameSetUp();
        }

        // Convert the board variable to ui elements, and add those ui elements to the canvas
        private void BuildBoard()
        {

            for (int i = 0; i < boardRows; i++)
            {
                for (int j = 0; j < boardCols; j++)
                {
                    int xAdd = 0, yAdd = 0;
                    int wAdd = 0, hAdd = 0;
                    if (i > 0 && board[i - 1, j] == Cell.Wall)
                    {
                        yAdd -= cellSize / 2;
                        hAdd += cellSize / 2;
                    }
                    if (i < boardRows - 1 && board[i + 1, j] == Cell.Wall)
                    {
                        hAdd += cellSize / 2;
                    }
                    if (j > 0 && board[i, j - 1] == Cell.Wall)
                    {
                        xAdd -= cellSize / 2;
                        wAdd += cellSize / 2;
                    }
                    if (j < boardCols - 1 && board[i, j + 1] == Cell.Wall)
                    {
                        wAdd += cellSize / 2;
                    }

                    if (board[i, j] == Cell.Wall)
                    {
                        Rectangle rect = new Rectangle
                        {
                            Width = cellSize / 2 + wAdd,
                            Height = cellSize / 2 + hAdd
                        };
                        Canvas.SetTop(rect, (i * cellSize) + offSet + yAdd);
                        Canvas.SetLeft(rect, (j * cellSize) + xAdd);
                        rect.Fill = new SolidColorBrush(Colors.Blue);
                        canvasGame.Children.Add(rect);
                    }
                    if (board[i, j] == Cell.Food)
                    {
                        Ellipse elli = new Ellipse
                        {
                            Width = cellSize / 4,
                            Height = cellSize / 4
                        };
                        Canvas.SetTop(elli, i * cellSize + cellSize / 4 + offSet);
                        Canvas.SetLeft(elli, j * cellSize + cellSize / 4);
                        elli.Fill = new SolidColorBrush(Colors.Gold);
                        canvasGame.Children.Add(elli);
                    }
                }
            }
        }
        // Utility method to create Image from file path
        private ImageBrush CreateImageBrush(string uri)
        {
            ImageBrush imageBrush = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(uri))
            };
            return imageBrush;
        }

        // Set up the game, create ghosts, build board, start timers...
        private void GameSetUp()
        {
            BuildBoard();
            canvasGame.Focus();
            moveTimerGhosts.Interval = TimeSpan.FromMilliseconds(220);
            moveTimerGhosts.Tick += MoveGhosts!;
            moveTimerGhosts.Start();
            mainTimer.Tick += TimerEvent!;
            mainTimer.Interval = TimeSpan.FromMilliseconds(120);
            mainTimer.Start();

            var imagesFolder = new DirectoryInfo(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Projects/ProjectPacmanGame/Characters/"));

            for (int i = 0; i < ghosts.Length; i++)
            {
                ghostElements[i] = new Rectangle
                {
                    Fill = CreateImageBrush(imagesFolder + ghosts[i]._uri),
                    Width = cellSize * .9,
                    Height = cellSize * .9
                };
                canvasGame.Children.Add(ghostElements[i]);
            }
            pacman.Fill = CreateImageBrush(imagesFolder + "pacman.gif");
        }




        // Check if an entity can walk through the row and col
        private bool IsCellEmpty(int row, int col)
        {
            return board[row, col] != Cell.Wall;
        }

        // Utility method the add a direction to a position, making sure the position does not enter a wall, and also jump to the other side of the map if needed
        private Position AddDirectionToPosition(Position position, Direction direction)
        {
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i]._direction == Direction.MoveLeft)
                {

                    ghostElements[i].RenderTransform = new RotateTransform(-1, ghostElements[i].Width / 2, ghostElements[i].Height / 2);
                }
                if (ghosts[i]._direction == Direction.MoveRight)
                {

                    ghostElements[i].RenderTransform = new RotateTransform(-1, ghostElements[i].Width / 2, ghostElements[i].Height / 2);
                }
                if (ghosts[i]._direction == Direction.MoveUp)
                {

                    ghostElements[i].RenderTransform = new RotateTransform(90, ghostElements[i].Width / 2, ghostElements[i].Height / 2);
                }
                if (ghosts[i]._direction == Direction.MoveDown)
                {

                    ghostElements[i].RenderTransform = new RotateTransform(-90, ghostElements[i].Width / 2, ghostElements[i].Height / 2);
                }

            }

            if (direction == Direction.MoveDown)
            {
                if (IsCellEmpty(position._row + 1, position._col))
                {
                    return new Position(position._row + 1, position._col);
                }
            }
            else if (direction == Direction.MoveUp)
            {
                if (IsCellEmpty(position._row - 1, position._col))
                {
                    return new Position(position._row - 1, position._col);
                }
            }
            else if (direction == Direction.MoveLeft)
            {
                if (position._col == 0)
                {
                    return new Position(position._row, boardCols - 1);
                }
                if (IsCellEmpty(position._row, position._col - 1))
                {
                    return new Position(position._row, position._col - 1);
                }
            }
            else if (direction == Direction.MoveRight)
            {
                if (position._col == boardCols - 1)
                {
                    return new Position(position._row, 0);
                }
                if (IsCellEmpty(position._row, position._col + 1))
                {
                    return new Position(position._row, position._col + 1);
                }
            }

            return new Position(position._row, position._col);
        }

        // Ghosts' path finding algorithm
        private void UpdatePathFindingMatrix(int row, int col, uint distance)
        {

            if (distance >= maxDistance || row < 0 || col < 0 || row >= boardRows || col >= boardCols)
            {
                return;
            }

            if (pathFindingMatrix[row, col] > distance && IsCellEmpty(row, col))
            {
                pathFindingMatrix[row, col] = distance;

                UpdatePathFindingMatrix(row + 1, col, distance + 1);
                UpdatePathFindingMatrix(row - 1, col, distance + 1);
                UpdatePathFindingMatrix(row, col + 1, distance + 1);
                UpdatePathFindingMatrix(row, col - 1, distance + 1);
            }
        }

        // Initialize the ghosts' path finding information
        private void UpdatePathFindingMatrix()
        {
            for (int row = 0; row < boardRows; row++)
            {
                for (int col = 0; col < boardCols; col++)
                {
                    pathFindingMatrix[row, col] = maxDistance;
                }
            }
            UpdatePathFindingMatrix(playerPosition._row, playerPosition._col, 0);
        }

        // Update the direction of the ghosts, make them walk toward the player
        private void UpdateGhostsDirection()
        {
            // Move each ghost to closest cell to player
            for (int i = 0; i < ghosts.Length; i++)
            {
                int row = ghosts[i]._position._row;
                int col = ghosts[i]._position._col;
                uint u = pathFindingMatrix[Math.Max(row - 1, 0), col];          // Up distance
                uint d = pathFindingMatrix[Math.Min(row + 1, boardRows), col]; // Down distance
                uint r = pathFindingMatrix[row, Math.Min(col + 1, boardCols)]; // Right distance
                uint l = pathFindingMatrix[row, Math.Max(col - 1, 0)];          // Left distance
                if (u < d && u < r && u < l && IsCellEmpty(row - 1, col))
                {
                    ghosts[i]._direction = Direction.MoveUp;
                }
                else if (d < r && d < l && IsCellEmpty(row + 1, col))
                {
                    ghosts[i]._direction = Direction.MoveDown;
                }
                else if (r < l && IsCellEmpty(row, col + 1))
                {
                    ghosts[i]._direction = Direction.MoveRight;
                }
                else if (IsCellEmpty(row, col - 1))
                {
                    ghosts[i]._direction = Direction.MoveLeft;
                }
            }
        }

        // Move the position of every ghost
        private void MoveGhosts(object sender, EventArgs e)
        {
            // Update ghosts direction
            UpdateGhostsDirection();

            // Move ghosts
            for (int i = 0; i < ghosts.Length; i++)
            {
                var nextPosition = AddDirectionToPosition(ghosts[i]._position, ghosts[i]._direction);

                bool canMove = true;
                for (int j = 0; j < ghosts.Length; j++)
                {
                    Position otherPosition = ghosts[j]._position;
                    if (otherPosition._row == nextPosition._row && otherPosition._col == nextPosition._col)
                    {
                        canMove = false;
                    }
                }

                if (canMove)
                {
                    ghosts[i]._position = nextPosition;
                }

                Canvas.SetLeft(ghostElements[i], ghosts[i]._position._col * cellSize + cellSize * .1);
                Canvas.SetTop(ghostElements[i], ghosts[i]._position._row * cellSize + cellSize * .1 + offSet);
            }

        }

        // Update the game, move the player and check and intersection between ghost and player
        private void TimerEvent(object sender, EventArgs e)
        {
            // Move player
            playerPosition = AddDirectionToPosition(playerPosition, playerDirection);
            UpdatePathFindingMatrix();

            Canvas.SetTop(pacman, playerPosition._row * cellSize + offSet);
            Canvas.SetLeft(pacman, playerPosition._col * cellSize);

            // Check if player touch any ghost
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i]._position._row == playerPosition._row && ghosts[i]._position._col == playerPosition._col)
                {
                    GameOver("Ghost caught you, you lost the game!");
                    return;
                }
            }

            // Eat the food in the current player position
            if (board[playerPosition._row, playerPosition._col] == Cell.Food)
            {
                board[playerPosition._row, playerPosition._col] = Cell.Empty;
                foreach (Ellipse child in canvasGame.Children.OfType<Ellipse>())
                {
                    int x = playerPosition._col * cellSize + cellSize / 4;
                    int y = playerPosition._row * cellSize + cellSize / 4 + offSet;
                    if (Canvas.GetLeft(child) == x && Canvas.GetTop(child) == y)
                    {
                        child.Visibility = Visibility.Hidden;
                    }
                }

                score += 10;

                if (score == 3600)
                {
                    GameOver("Well done, you collected all the coins, you won the game!");
                }
            }

            txtScore.Content = "Score: " + score;
        }

        // Gave over method
        private void GameOver(string message)
        {
            moveTimerGhosts.Stop();
            mainTimer.Stop();
            timerFinishGame.Stop();
            MessageBox.Show(message, "Pacman");
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        // Get input
        private void CanvasKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                playerDirection = Direction.MoveLeft;
                pacman.RenderTransform = new RotateTransform(-180, pacman.Width / 2, pacman.Height / 2);
            }
            if (e.Key == Key.Right)
            {
                playerDirection = Direction.MoveRight;
                pacman.RenderTransform = new RotateTransform(0, pacman.Width / 2, pacman.Height / 2);
            }
            if (e.Key == Key.Up)
            {
                playerDirection = Direction.MoveUp;
                pacman.RenderTransform = new RotateTransform(-90, pacman.Width / 2, pacman.Height / 2);
            }
            if (e.Key == Key.Down)
            {
                playerDirection = Direction.MoveDown;
                pacman.RenderTransform = new RotateTransform(90, pacman.Width / 2, pacman.Height / 2);
            }
        }
    }
}