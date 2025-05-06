using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Input;
using System.Data;
using MySql.Data.MySqlClient;

namespace Chackers
{
    public sealed partial class GamePage : Page
    {
        private const int SquareSize = 100;
        private GameBoard gameBoard;
        private ComputerPlayer computerPlayer;
        private Piece selectedPiece;
        private int selectedRow = -1;
        private int selectedCol = -1;
        private bool isVsComputer;
        private bool isPlayerWhite;
        private DispatcherTimer computerTimer;
        private MediaPlayer soundPlayer;
        private MediaPlayer captureSoundPlayer;
        private MediaPlayer kingSoundPlayer;
        private MediaPlayer hoverSoundPlayer;
        private int userId;
        private DateTime gameStartTime;
        private int playerPoints;
        private int totalMoves;
        private int prevWhiteCaptured = 0;
        private int prevBlackCaptured = 0;

        public GamePage()
        {
            this.InitializeComponent();
            InitializeSoundPlayers();
            InitializeHoverSound();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                base.OnNavigatedTo(e);
                if (e.Parameter is GameLoadParameters loadParams)
                {
                    this.userId = loadParams.UserId;
                    this.isVsComputer = loadParams.SaveData.IsVsComputer;
                    this.isPlayerWhite = loadParams.SaveData.IsPlayerWhite;
                    LoadSavedGame(loadParams.SaveData);
                }
                else if (e.Parameter is GameSaveData saveData)
                {
                    this.isVsComputer = saveData.IsVsComputer;
                    this.isPlayerWhite = saveData.IsPlayerWhite;
                    LoadSavedGame(saveData);
                }
                else if (e.Parameter is GameParameters parameters)
                {
                    this.isVsComputer = parameters.IsVsComputer;
                    this.isPlayerWhite = parameters.IsPlayerWhite;
                    this.userId = parameters.UserId;
                    InitializeGame();
                    UpdateTurnText();

                    if (isVsComputer && !isPlayerWhite)
                    {
                        MakeComputerMove();
                    }
                }
                else if (e.Parameter is int id)
                {
                    this.userId = id;
                }
                else if (e.Parameter is GameOverParameters gameOverParameters)
                {
                    userId = gameOverParameters.UserId;
                    // ...
                }
                else
                {
                    // Если параметры не переданы, возвращаемся на предыдущую страницу
                    Frame.GoBack();
                }
            }
            catch (Exception ex)
            {
                // Показываем сообщение об ошибке и возвращаемся назад
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"An error occurred while initializing the game: {ex.Message}",
                    CloseButtonText = "OK"
                };
                dialog.ShowAsync();
                Frame.GoBack();
            }
        }

        private void InitializeGame()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Initializing new game...");
                gameBoard = new GameBoard();
                System.Diagnostics.Debug.WriteLine("GameBoard created and initialized");
                
                if (isVsComputer)
                {
                    computerPlayer = new ComputerPlayer(gameBoard, !isPlayerWhite);
                    System.Diagnostics.Debug.WriteLine("Computer player initialized");
                }
                else
                {
                    computerPlayer = null;
                }
                
                SetupBoardUI();
                System.Diagnostics.Debug.WriteLine("Board UI setup completed");
                
                UpdateBoard();
                System.Diagnostics.Debug.WriteLine("Board updated with pieces");
                UpdateTurnText();

                gameStartTime = DateTime.Now;
                playerPoints = 0;
                totalMoves = 0;

                if (isVsComputer && !isPlayerWhite)
                {
                    MakeComputerMove();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeGame: {ex.Message}");
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"An error occurred while initializing the game: {ex.Message}",
                    CloseButtonText = "OK"
                };
                dialog.ShowAsync();
                Frame.GoBack();
            }
        }

        private void SetupBoardUI()
        {
            BoardGrid.Children.Clear();
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Button square = new Button
                    {
                        Style = (Style)Resources["BoardSquareStyle"],
                        Background = (row + col) % 2 == 0 ?
                            new SolidColorBrush(Color.FromArgb(255, 255, 206, 158)) :
                            new SolidColorBrush(Color.FromArgb(255, 139, 69, 19)),
                        Tag = new Tuple<int, int>(row, col),
                        Width = SquareSize,
                        Height = SquareSize
                    };
                    square.Click += Square_Click;
                    Grid.SetRow(square, row);
                    Grid.SetColumn(square, col);
                    BoardGrid.Children.Add(square);
                }
            }
        }

        private Ellipse CreatePiece(bool isWhite, bool isKing)
        {
            var piece = new Ellipse
            {
                Width = SquareSize - 20,
                Height = SquareSize - 20,
                Stroke = new SolidColorBrush(isWhite ? Colors.Black : Colors.White),
                StrokeThickness = 2,
                RenderTransform = new TranslateTransform()
            };

            try
            {
                var imageBrush = new ImageBrush();
                string imagePath = isWhite ? 
                    (isKing ? "ms-appx:///Assets/white_king.png" : "ms-appx:///Assets/white_piece.png") :
                    (isKing ? "ms-appx:///Assets/black_king.png" : "ms-appx:///Assets/black_piece.png");

                try
                {
                var bitmapImage = new BitmapImage(new Uri(imagePath));
                imageBrush.ImageSource = bitmapImage;
                piece.Fill = imageBrush;
            }
            catch (Exception ex)
            {
                    System.Diagnostics.Debug.WriteLine($"Error loading image {imagePath}: {ex.Message}");
                    // Fallback to solid color if image loading fails
                    piece.Fill = new SolidColorBrush(isWhite ? Colors.White : Colors.Black);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreatePiece: {ex.Message}");
                piece.Fill = new SolidColorBrush(isWhite ? Colors.White : Colors.Black);
            }

            return piece;
        }

        private void UpdateTurnText()
        {
            if (isVsComputer)
            {
                bool isWhiteTurn = gameBoard.IsWhiteTurn();
                TurnText.Text = isWhiteTurn == isPlayerWhite ? "Your Turn" : "Computer's Turn";
            }
            else
            {
                TurnText.Text = gameBoard.IsWhiteTurn() ? "White's Turn" : "Black's Turn";
            }
        }

        private async void Square_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isVsComputer && gameBoard.IsWhiteTurn() != isPlayerWhite)
                    return;

                Button clickedSquare = (Button)sender;
                var position = (Tuple<int, int>)clickedSquare.Tag;
                int row = position.Item1;
                int col = position.Item2;

                if (selectedPiece == null)
                {
                    Piece piece = gameBoard.GetBoard()[row, col];
                    if (piece != null && piece.Color == (gameBoard.IsWhiteTurn() ? PieceColor.White : PieceColor.Black))
                    {
                        selectedPiece = piece;
                        selectedRow = row;
                        selectedCol = col;
                        clickedSquare.Background = new SolidColorBrush(Colors.Yellow);
                    }
                }
                else
                {
                    if (gameBoard.IsValidMove(selectedRow, selectedCol, row, col))
                    {
                        var fromSquare = GetSquare(selectedRow, selectedCol);
                        var toSquare = GetSquare(row, col);

                        try
                        {
                            await AnimatePieceMoveWithTrail(fromSquare, toSquare);
                        }
                        catch (Exception animEx)
                        {
                            await new ContentDialog
                            {
                                Title = "Animation Error",
                                Content = $"Ошибка анимации: {animEx.Message}",
                                CloseButtonText = "OK"
                            }.ShowAsync();
                            System.Diagnostics.Debug.WriteLine($"Animation error: {animEx}");
                        }

                        try
                        {
                            Piece[,] oldBoard = (Piece[,])gameBoard.GetBoard().Clone();
                            gameBoard.MakeMove(selectedRow, selectedCol, row, col);
                            await UpdateBoard();
                            UpdateTurnText();

                            // Проверяем, был ли это ход с взятием
                            if (Math.Abs(selectedRow - row) == 2)
                            {
                                // Проигрываем звук взятия шашки
                                if (captureSoundPlayer != null)
                                {
                                    captureSoundPlayer.Pause();
                                    captureSoundPlayer.PlaybackSession.Position = TimeSpan.Zero;
                                    captureSoundPlayer.Play();
                                }
                            }

                            // Проверяем, не стала ли фигура дамкой
                            var piece = gameBoard.GetBoard()[row, col];
                            if (piece != null && piece.IsKing)
                            {
                                if (kingSoundPlayer != null)
                                {
                                    kingSoundPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(2);
                                    kingSoundPlayer.Play();
                                }
                            }
                        }
                        catch (Exception logicEx)
                        {
                            await new ContentDialog
                            {
                                Title = "Game Logic Error",
                                Content = $"Ошибка логики: {logicEx.Message}",
                                CloseButtonText = "OK"
                            }.ShowAsync();
                            System.Diagnostics.Debug.WriteLine($"Game logic error: {logicEx}");
                        }

                        if (isVsComputer && gameBoard.IsWhiteTurn() != isPlayerWhite)
                        {
                            await Task.Delay(500);
                            MakeComputerMove();
                        }

                        totalMoves++;
                        if (Math.Abs(selectedRow - row) == 2)
                            playerPoints += 3;
                        else
                            playerPoints += 1;
                    }

                    GetSquare(selectedRow, selectedCol).Background = (selectedRow + selectedCol) % 2 == 0 ?
                        new SolidColorBrush(Color.FromArgb(255, 255, 206, 158)) :
                        new SolidColorBrush(Color.FromArgb(255, 139, 69, 19));

                    selectedPiece = null;
                    selectedRow = -1;
                    selectedCol = -1;
                }
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Critical Error",
                    Content = $"Произошла ошибка: {ex.Message}",
                    CloseButtonText = "OK"
                }.ShowAsync();
                System.Diagnostics.Debug.WriteLine($"Critical error: {ex}");
            }
        }

        private void ComputerTimer_Tick(object sender, object e)
        {
            computerTimer.Stop();
            MakeComputerMove();
        }

        private async void MakeComputerMove()
        {
            try
            {
                if (computerPlayer != null)
                {
                    System.Diagnostics.Debug.WriteLine("Computer player is making a move...");
                    var move = computerPlayer.GetNextMove();
                    if (move != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Computer move: from [{move.FromRow},{move.FromCol}] to [{move.ToRow},{move.ToCol}]");
                        var fromSquare = GetSquare(move.FromRow, move.FromCol);
                        var toSquare = GetSquare(move.ToRow, move.ToCol);

                        if (fromSquare != null && toSquare != null)
                        {
                            try
                            {
                                // Анимируем перемещение фигуры
                                await AnimatePieceMoveWithTrail(fromSquare, toSquare);

                                // Делаем ход
                                if (gameBoard.MakeMove(move.FromRow, move.FromCol, move.ToRow, move.ToCol))
                                {
                                    System.Diagnostics.Debug.WriteLine("Computer move executed successfully");

                                    // Проверяем, не стала ли фигура дамкой
                                    var piece = gameBoard.GetBoard()[move.ToRow, move.ToCol];
                                    if (piece != null && piece.IsKing)
                                    {
                                        if (kingSoundPlayer != null)
                                        {
                                            kingSoundPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(2);
                                            kingSoundPlayer.Play();
                                        }
                                    }

                                    await UpdateBoard();
                                    UpdateTurnText();

                                    totalMoves++;
                                    if (Math.Abs(move.FromRow - move.ToRow) == 2)
                                    {
                                        playerPoints += 3;
                                        // Проигрываем звук взятия
                                        if (captureSoundPlayer != null)
                                        {
                                            captureSoundPlayer.Pause();
                                            captureSoundPlayer.PlaybackSession.Position = TimeSpan.Zero;
                                            captureSoundPlayer.Play();
                                        }
                                    }
                                    else
                                    {
                                        playerPoints += 1;
                                        // Проигрываем звук хода
                                        if (soundPlayer != null)
                                        {
                                            soundPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(2);
                                            soundPlayer.Play();
                                        }
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("Failed to execute computer move");
                                }
                            }
                            catch (Exception animEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Animation error: {animEx.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Invalid square references in computer move");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Computer player returned null move");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Computer player is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MakeComputerMove: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task UpdateBoard()
        {
            System.Diagnostics.Debug.WriteLine("Updating board display...");
            Piece[,] board = gameBoard.GetBoard();
            int whiteActive = 0;
            int blackActive = 0;
            int whiteCaptured = 0;
            int blackCaptured = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Button square = GetSquare(row, col);
                    var oldContent = square.Content as Ellipse;
                    square.Content = null;

                    Piece piece = board[row, col];
                    if (piece != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Piece found at [{row},{col}]: {(piece.Color == PieceColor.White ? "White" : "Black")} {(piece.IsKing ? "King" : "Regular")}");
                        var newPiece = CreatePiece(piece.Color == PieceColor.White, piece.IsKing);
                        square.Content = newPiece;

                        if (piece.Color == PieceColor.White)
                            whiteActive++;
                        else
                            blackActive++;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Board state: White active: {whiteActive}, Black active: {blackActive}");

            whiteCaptured = 12 - whiteActive;
            blackCaptured = 12 - blackActive;

            WhiteActiveCount.Text = whiteActive.ToString();
            WhiteCapturedCount.Text = whiteCaptured.ToString();
            BlackActiveCount.Text = blackActive.ToString();
            BlackCapturedCount.Text = blackCaptured.ToString();

            // Проигрываем capture_sound только если увеличилось количество захваченных шашек
            if ((whiteCaptured > prevWhiteCaptured) || (blackCaptured > prevBlackCaptured))
            {
                if (captureSoundPlayer != null)
                {
                    captureSoundPlayer.Pause();
                    captureSoundPlayer.PlaybackSession.Position = TimeSpan.Zero;
                    captureSoundPlayer.Play();
                }
            }
            prevWhiteCaptured = whiteCaptured;
            prevBlackCaptured = blackCaptured;

            if (whiteActive == 0 || blackActive == 0)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"GameOver: isVsComputer={isVsComputer}, isWhiteWinner={blackActive == 0}, isPlayerWhite={isPlayerWhite}, userId={userId}, duration={(int)(DateTime.Now - gameStartTime).TotalSeconds}, points={playerPoints}, moves={totalMoves}");
                    var gameOverParams = new GameOverParameters
                    {
                        IsVsComputer = isVsComputer,
                        IsWhiteWinner = blackActive == 0,
                        IsPlayerWhite = isPlayerWhite,
                        UserId = this.userId,
                        DurationSeconds = (int)(DateTime.Now - gameStartTime).TotalSeconds,
                        Points = playerPoints,
                        MovesCount = totalMoves
                    };
                    Frame.Navigate(typeof(GameOverPage), gameOverParams);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error navigating to GameOverPage: {ex.Message}\n{ex.StackTrace}");
                    await new ContentDialog
                    {
                        Title = "Critical Game Over Error",
                        Content = $"An error occurred while finishing the game: {ex.Message}",
                        CloseButtonText = "OK"
                    }.ShowAsync();
                }
            }
        }

        private Button GetSquare(int row, int col)
        {
            foreach (var child in BoardGrid.Children)
            {
                if (child is Button button)
                {
                    var position = button.Tag as Tuple<int, int>;
                    if (position.Item1 == row && position.Item2 == col)
                    {
                        return button;
                    }
                }
            }
            return null;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            Frame.Navigate(typeof(GameModePage));
        }

        private async void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            try 
            {
                var result = await SaveGameDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var saveName = SaveNameTextBox.Text;
                    SaveNameTextBox.Text = string.Empty;
                    
                    if (string.IsNullOrWhiteSpace(saveName))
                    {
                        saveName = $"Game_{DateTime.Now:yyyy-MM-dd_HH-mm}";
                    }

                    // Подсчитываем активные и захваченные шашки
                    int whiteActive = 0;
                    int blackActive = 0;
                    var board = gameBoard.GetBoard();
                    var capturedPieces = new List<PieceData>();
                    
                    // Сначала считаем активные шашки на доске
                    for (int row = 0; row < 8; row++)
                    {
                        for (int col = 0; col < 8; col++)
                        {
                            var piece = board[row, col];
                            if (piece != null)
                            {
                                if (piece.Color == PieceColor.White)
                                    whiteActive++;
                                else
                                    blackActive++;
                            }
                        }
                    }

                    // Добавляем информацию о захваченных шашках
                    for (int i = 0; i < 12 - whiteActive; i++)
                    {
                        capturedPieces.Add(new PieceData { IsWhite = true });
                    }
                    for (int i = 0; i < 12 - blackActive; i++)
                    {
                        capturedPieces.Add(new PieceData { IsWhite = false });
                    }

                    var saveData = new GameSaveData
                    {
                        SaveDate = DateTime.Now,
                        SaveName = saveName,
                        IsWhiteTurn = gameBoard.IsWhiteTurn(),
                        IsVsComputer = isVsComputer,
                        IsPlayerWhite = isPlayerWhite,
                        WhiteActive = whiteActive,
                        BlackActive = blackActive,
                        Pieces = new List<PieceData>(),
                        CapturedPieces = capturedPieces
                    };

                    for (int row = 0; row < 8; row++)
                    {
                        for (int col = 0; col < 8; col++)
                        {
                            var piece = board[row, col];
                            if (piece != null)
                            {
                                saveData.Pieces.Add(new PieceData
                                {
                                    Row = row,
                                    Col = col,
                                    IsWhite = piece.Color == PieceColor.White,
                                    IsKing = piece.IsKing
                                });
                            }
                        }
                    }

                    var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                    var savePath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "saves");
                    
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }

                    var fullPath = System.IO.Path.Combine(savePath, $"{saveName}.json");
                    await File.WriteAllTextAsync(fullPath, json);

                    var confirmDialog = new ContentDialog
                    {
                        Title = "Game Saved",
                        Content = $"Your game has been saved as '{saveName}'",
                        CloseButtonText = "OK"
                    };
                    await confirmDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error Saving Game",
                    Content = $"An error occurred while saving the game: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void InitializeSoundPlayers()
        {
            try
            {
                soundPlayer = new MediaPlayer();
                try
                {
                var moveSoundFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/move_sound.mp3"));
                var moveStream = await moveSoundFile.OpenAsync(FileAccessMode.Read);
                soundPlayer.SetStreamSource(moveStream);
                soundPlayer.Volume = 1.0;
                soundPlayer.IsMuted = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading move sound: {ex.Message}");
                }

                captureSoundPlayer = new MediaPlayer();
                try
                {
                var captureSoundFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/capture_sound.mp3"));
                var captureStream = await captureSoundFile.OpenAsync(FileAccessMode.Read);
                captureSoundPlayer.SetStreamSource(captureStream);
                captureSoundPlayer.Volume = 1.0;
                captureSoundPlayer.IsMuted = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading capture sound: {ex.Message}");
                }

                kingSoundPlayer = new MediaPlayer();
                try
                {
                var kingSoundFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/king_sound.mp3"));
                var kingStream = await kingSoundFile.OpenAsync(FileAccessMode.Read);
                kingSoundPlayer.SetStreamSource(kingStream);
                kingSoundPlayer.Volume = 1.0;
                kingSoundPlayer.IsMuted = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading king sound: {ex.Message}");
                    // Use capture sound as fallback for king sound
                    kingSoundPlayer = captureSoundPlayer;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing sound players: {ex.Message}");
            }
        }

        private void LoadSavedGame(GameSaveData saveData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading saved game...");
                System.Diagnostics.Debug.WriteLine($"Game mode: {(saveData.IsVsComputer ? "vs Computer" : "vs Player")}");
                System.Diagnostics.Debug.WriteLine($"Player color: {(saveData.IsPlayerWhite ? "White" : "Black")}");
                System.Diagnostics.Debug.WriteLine($"Current turn: {(saveData.IsWhiteTurn ? "White" : "Black")}");

                // Устанавливаем режим игры
                this.isVsComputer = saveData.IsVsComputer;
                this.isPlayerWhite = saveData.IsPlayerWhite;

                // Инициализируем доску
                gameBoard = new GameBoard();
                
                // Инициализируем компьютерного игрока, если это игра с компьютером
                if (isVsComputer)
                {
                    System.Diagnostics.Debug.WriteLine("Initializing computer player...");
                    computerPlayer = new ComputerPlayer(gameBoard, !isPlayerWhite);
                    System.Diagnostics.Debug.WriteLine("Computer player initialized");
                }
                else
                {
                    computerPlayer = null;
                }

                // Очищаем и инициализируем доску
                BoardGrid.Children.Clear();
                SetupBoardUI();

                // Очищаем текущее состояние доски
                var board = gameBoard.GetBoard();
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        board[row, col] = null;
                    }
                }

                // Расставляем фигуры из сохранения
                foreach (var pieceData in saveData.Pieces)
                {
                    board[pieceData.Row, pieceData.Col] = new Piece(
                        pieceData.IsWhite ? PieceColor.White : PieceColor.Black,
                        pieceData.Row,
                        pieceData.Col)
                    {
                        IsKing = pieceData.IsKing
                    };
                }

                // Устанавливаем текущий ход
                while (gameBoard.IsWhiteTurn() != saveData.IsWhiteTurn)
                {
                    gameBoard.SwitchTurn();
                }

                // Обновляем отображение
                UpdateBoard();
                UpdateTurnText();

                // Инициализируем таймер для хода компьютера, если это игра с компьютером
                if (isVsComputer)
                {
                    System.Diagnostics.Debug.WriteLine("Setting up computer timer...");
                    if (computerTimer == null)
                    {
                        computerTimer = new DispatcherTimer();
                        computerTimer.Interval = TimeSpan.FromMilliseconds(500);
                        computerTimer.Tick += ComputerTimer_Tick;
                    }

                    // Если сейчас ход компьютера, запускаем таймер
                    if (gameBoard.IsWhiteTurn() != isPlayerWhite)
                    {
                        System.Diagnostics.Debug.WriteLine("Starting computer timer...");
                        computerTimer.Start();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Computer's turn is not now, timer not started");
                    }
                }

                System.Diagnostics.Debug.WriteLine("Saved game loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved game: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void PlayButtonSound()
        {
            try
            {
                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/button_click.mp3"));
                player.Volume = 1.0;
                player.Play();
                // Освобождаем ресурсы после проигрывания
                player.MediaEnded += (s, e) => { player.Dispose(); };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing button sound: {ex.Message}");
            }
        }

        private async void AnimateScale(ScaleTransform scale)
        {
            scale.ScaleX = 1.18;
            scale.ScaleY = 1.18;
            await Task.Delay(90);
            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;
        }

        private async void InitializeHoverSound()
        {
            try
            {
                hoverSoundPlayer = new MediaPlayer();
                var soundFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/hover_sound.mp3"));
                var stream = await soundFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                hoverSoundPlayer.SetStreamSource(stream);
                hoverSoundPlayer.Volume = 0.3;
                hoverSoundPlayer.PlaybackRate = 2.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing hover sound: {ex.Message}");
            }
        }

        private async Task AnimatePieceMoveWithTrail(Button fromSquare, Button toSquare)
        {
            if (fromSquare.Content is Ellipse piece)
            {
                // Получаем центр fromSquare относительно AnimationCanvas
                var fromRect = fromSquare.TransformToVisual(AnimationCanvas).TransformBounds(new Rect(0, 0, fromSquare.ActualWidth, fromSquare.ActualHeight));
                var toRect = toSquare.TransformToVisual(AnimationCanvas).TransformBounds(new Rect(0, 0, toSquare.ActualWidth, toSquare.ActualHeight));
                var fromCenter = new Point(fromRect.Left + fromRect.Width / 2 - piece.Width / 2, fromRect.Top + fromRect.Height / 2 - piece.Height / 2);
                var toCenter = new Point(toRect.Left + toRect.Width / 2 - piece.Width / 2, toRect.Top + toRect.Height / 2 - piece.Height / 2);

                int trailCount = 5;
                double[] opacities = { 0.3, 0.24, 0.18, 0.12, 0.06 };
                int trailDelay = 50; // мс между шлейфами
                int duration = 700; // длительность анимации (медленнее)

                var animatedPieces = new List<Ellipse>();
                var storyboards = new List<Storyboard>();

                // Создаем шлейф
                for (int i = 0; i < trailCount; i++)
                {
                    var trailPiece = new Ellipse
                    {
                        Width = piece.Width,
                        Height = piece.Height,
                        Fill = piece.Fill,
                        Stroke = piece.Stroke,
                        StrokeThickness = piece.StrokeThickness,
                        Opacity = opacities[i]
                    };
                    Canvas.SetLeft(trailPiece, fromCenter.X);
                    Canvas.SetTop(trailPiece, fromCenter.Y);
                    AnimationCanvas.Children.Add(trailPiece);
                    animatedPieces.Add(trailPiece);

                    var animX = new DoubleAnimation
                    {
                        From = fromCenter.X,
                        To = toCenter.X,
                        Duration = TimeSpan.FromMilliseconds(duration),
                        BeginTime = TimeSpan.FromMilliseconds(i * trailDelay),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var animY = new DoubleAnimation
                    {
                        From = fromCenter.Y,
                        To = toCenter.Y,
                        Duration = TimeSpan.FromMilliseconds(duration),
                        BeginTime = TimeSpan.FromMilliseconds(i * trailDelay),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fade = new DoubleAnimation
                    {
                        From = opacities[i],
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(duration),
                        BeginTime = TimeSpan.FromMilliseconds(i * trailDelay)
                    };
                    var sb = new Storyboard();
                    sb.Children.Add(animX);
                    sb.Children.Add(animY);
                    sb.Children.Add(fade);
                    Storyboard.SetTarget(animX, trailPiece);
                    Storyboard.SetTargetProperty(animX, "(Canvas.Left)");
                    Storyboard.SetTarget(animY, trailPiece);
                    Storyboard.SetTargetProperty(animY, "(Canvas.Top)");
                    Storyboard.SetTarget(fade, trailPiece);
                    Storyboard.SetTargetProperty(fade, "Opacity");
                    sb.Begin();
                    storyboards.Add(sb);
                }

                // Основная анимируемая шашка
                var animatedPiece = new Ellipse
                {
                    Width = piece.Width,
                    Height = piece.Height,
                    Fill = piece.Fill,
                    Stroke = piece.Stroke,
                    StrokeThickness = piece.StrokeThickness,
                    Opacity = 0.95
                };
                Canvas.SetLeft(animatedPiece, fromCenter.X);
                Canvas.SetTop(animatedPiece, fromCenter.Y);
                AnimationCanvas.Children.Add(animatedPiece);
                fromSquare.Content = null;

                var mainAnimX = new DoubleAnimation
                {
                    From = fromCenter.X,
                    To = toCenter.X,
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                var mainAnimY = new DoubleAnimation
                {
                    From = fromCenter.Y,
                    To = toCenter.Y,
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                var sbMain = new Storyboard();
                sbMain.Children.Add(mainAnimX);
                sbMain.Children.Add(mainAnimY);
                Storyboard.SetTarget(mainAnimX, animatedPiece);
                Storyboard.SetTargetProperty(mainAnimX, "(Canvas.Left)");
                Storyboard.SetTarget(mainAnimY, animatedPiece);
                Storyboard.SetTargetProperty(mainAnimY, "(Canvas.Top)");

                // Звук: запуск с 2-й секунды
                if (soundPlayer != null && Math.Abs(fromCenter.X - toCenter.X) > 1 || Math.Abs(fromCenter.Y - toCenter.Y) > 1)
                {
                    soundPlayer.Pause();
                    soundPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(2);
                    soundPlayer.Play();
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(800);
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                soundPlayer.Pause();
                                soundPlayer.PlaybackSession.Position = TimeSpan.Zero;
                            });
                    });
                }

                sbMain.Begin();
                await Task.Delay(duration);

                // Удалить все эллипсы
                AnimationCanvas.Children.Remove(animatedPiece);
                foreach (var tr in animatedPieces)
                    AnimationCanvas.Children.Remove(tr);
            }
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(StatsPage));
        }
    }
} 