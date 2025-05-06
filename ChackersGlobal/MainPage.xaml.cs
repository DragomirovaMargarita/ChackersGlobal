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

namespace Chackers
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int SquareSize = 120;
        private GameBoard gameBoard;
        private ComputerPlayer computerPlayer;
        private Piece selectedPiece;
        private int selectedRow = -1;
        private int selectedCol = -1;
        private bool isVsComputer;
        private bool isPlayerWhite = true; // По умолчанию игрок играет белыми
        private DispatcherTimer computerTimer;
        private MediaPlayer soundPlayer;
        private MediaPlayer captureSoundPlayer;
        private MediaPlayer kingSoundPlayer;

        public MainPage()
        {
            this.InitializeComponent();
            InitializeSoundPlayers();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is GameSaveData saveData)
            {
                LoadSavedGame(saveData);
            }
            else if (e.Parameter is GameParameters parameters)
            {
                this.isVsComputer = parameters.IsVsComputer;
                this.isPlayerWhite = parameters.IsPlayerWhite;
                InitializeGame();
                UpdateTurnText();

                if (isVsComputer && !isPlayerWhite)
                {
                    MakeComputerMove();
                }
            }
        }

        private void InitializeGame()
        {
            gameBoard = new GameBoard();
            if (isVsComputer)
            {
                computerPlayer = new ComputerPlayer(gameBoard, !isPlayerWhite);
            }
            BoardGrid.Children.Clear();
            InitializeBoard();
            UpdateBoard();
        }

        private void InitializeBoard()
        {
            // Очищаем доску
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            // Добавляем определения строк и столбцов
            for (int i = 0; i < 8; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            }

            // Создаем клетки
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Проверяем, существует ли уже кнопка в этой позиции
                    Button existingSquare = GetSquare(row, col);
                    if (existingSquare != null)
                    {
                        BoardGrid.Children.Remove(existingSquare);
                    }

                    Button square = new Button
                    {
                        Style = (Style)Resources["BoardSquareStyle"],
                        Background = (row + col) % 2 == 0 ? 
                            new SolidColorBrush(Color.FromArgb(255, 255, 206, 158)) : // Light square
                            new SolidColorBrush(Color.FromArgb(255, 209, 139, 71)),   // Dark square
                        Tag = new Tuple<int, int>(row, col)
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
                Width = SquareSize - 10,
                Height = SquareSize - 10
            };

            try
            {
                var imageBrush = new ImageBrush();
                string imagePath;

                if (isWhite)
                {
                    imagePath = isKing ? "ms-appx:///Assets/white_king.png" : "ms-appx:///Assets/white_piece.png";
                }
                else
                {
                    imagePath = isKing ? "ms-appx:///Assets/black_king.png" : "ms-appx:///Assets/black_piece.png";
                }

                var bitmapImage = new BitmapImage(new Uri(imagePath));
                imageBrush.ImageSource = bitmapImage;
                piece.Fill = imageBrush;

                // Добавляем отладочную информацию
                System.Diagnostics.Debug.WriteLine($"Creating piece: {imagePath}, IsWhite: {isWhite}, IsKing: {isKing}");
            }
            catch (Exception ex)
            {
                // В случае ошибки загрузки изображения, используем цвет вместо изображения
                piece.Fill = new SolidColorBrush(isWhite ? Colors.White : Colors.Black);
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
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
                    if (gameBoard.MakeMove(selectedRow, selectedCol, row, col))
                    {
                        UpdateBoard();
                        UpdateTurnText();

                        if (isVsComputer && gameBoard.IsWhiteTurn() != isPlayerWhite)
                        {
                            await Task.Delay(500);
                            MakeComputerMove();
                        }
                    }

                    GetSquare(selectedRow, selectedCol).Background = (selectedRow + selectedCol) % 2 == 0 ?
                        new SolidColorBrush(Color.FromArgb(255, 255, 206, 158)) :
                        new SolidColorBrush(Color.FromArgb(255, 209, 139, 71));

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
            if (computerPlayer != null)
            {
                var move = computerPlayer.GetNextMove();
                if (move != null)
                {
                    var fromSquare = GetSquare(move.FromRow, move.FromCol);
                    var toSquare = GetSquare(move.ToRow, move.ToCol);
                    
                    try
                    {
                        // Анимируем ход компьютера
                        await AnimatePieceMove(fromSquare, toSquare);
                        
                        // Делаем ход
                        gameBoard.MakeMove(move.FromRow, move.FromCol, move.ToRow, move.ToCol);
                        UpdateBoard();
                        UpdateTurnText();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in computer move: {ex.Message}");
                        // В случае ошибки анимации, просто делаем ход без анимации
                        gameBoard.MakeMove(move.FromRow, move.FromCol, move.ToRow, move.ToCol);
                        UpdateBoard();
                        UpdateTurnText();
                    }
                }
            }
        }

        private async Task AnimatePieceMove(Button fromSquare, Button toSquare)
        {
            if (fromSquare.Content is Ellipse piece)
            {
                // Создаем копию фигуры для анимации
                var animatedPiece = new Ellipse
                {
                    Width = piece.Width,
                    Height = piece.Height,
                    Fill = piece.Fill
                };

                // Добавляем фигуру на Canvas для анимации
                Canvas.SetLeft(animatedPiece, Canvas.GetLeft(fromSquare));
                Canvas.SetTop(animatedPiece, Canvas.GetTop(fromSquare));
                AnimationCanvas.Children.Add(animatedPiece);

                // Скрываем оригинальную фигуру
                fromSquare.Content = null;

                // Анимируем перемещение
                var animation = new DoubleAnimation
                {
                    From = Canvas.GetLeft(fromSquare),
                    To = Canvas.GetLeft(toSquare),
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                var animation2 = new DoubleAnimation
                {
                    From = Canvas.GetTop(fromSquare),
                    To = Canvas.GetTop(toSquare),
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                var storyboard = new Storyboard();
                storyboard.Children.Add(animation);
                storyboard.Children.Add(animation2);

                Storyboard.SetTarget(animation, animatedPiece);
                Storyboard.SetTargetProperty(animation, "(Canvas.Left)");
                Storyboard.SetTarget(animation2, animatedPiece);
                Storyboard.SetTargetProperty(animation2, "(Canvas.Top)");

                storyboard.Begin();
                await Task.Delay(300); // Ждем завершения анимации

                // Удаляем анимированную фигуру
                AnimationCanvas.Children.Remove(animatedPiece);
            }
        }

        private void UpdateBoard()
        {
            Piece[,] board = gameBoard.GetBoard();
            int whiteActive = 0;
            int blackActive = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Button square = GetSquare(row, col);
                    square.Content = null;

                    Piece piece = board[row, col];
                    if (piece != null)
                    {
                        var newPiece = CreatePiece(piece.Color == PieceColor.White, piece.IsKing);
                        square.Content = newPiece;

                        if (piece.Color == PieceColor.White)
                            whiteActive++;
                        else
                            blackActive++;
                    }
                }
            }

            WhiteActiveCount.Text = whiteActive.ToString();
            WhiteCapturedCount.Text = (12 - whiteActive).ToString();
            BlackActiveCount.Text = blackActive.ToString();
            BlackCapturedCount.Text = (12 - blackActive).ToString();

            // Победа
            if (whiteActive == 0 || blackActive == 0)
            {
                var parameters = new GameOverParameters
                {
                    IsVsComputer = isVsComputer,
                    IsWhiteWinner = blackActive == 0,
                    IsPlayerWhite = isPlayerWhite
                };
                Frame.Navigate(typeof(GameOverPage), parameters);
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
            Frame.Navigate(typeof(GameModePage));
        }

        private async void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                var result = await SaveGameDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var saveName = SaveNameTextBox.Text;
                    SaveNameTextBox.Text = string.Empty; // Очищаем поле ввода
                    
                    if (string.IsNullOrWhiteSpace(saveName))
                    {
                        saveName = $"Game_{DateTime.Now:yyyy-MM-dd_HH-mm}";
                    }

                    var saveData = new GameSaveData
                    {
                        SaveDate = DateTime.Now,
                        SaveName = saveName,
                        IsWhiteTurn = gameBoard.IsWhiteTurn(),
                        IsVsComputer = isVsComputer,
                        IsPlayerWhite = isPlayerWhite,
                        Pieces = new List<PieceData>()
                    };

                    // Сохраняем все фигуры
                    var board = gameBoard.GetBoard();
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

                    // Сохраняем в JSON файл
                    var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                    var savePath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "saves");
                    
                    // Добавляем логирование
                    System.Diagnostics.Debug.WriteLine($"Attempting to save to directory: {savePath}");
                    System.Diagnostics.Debug.WriteLine($"Game mode: {(isVsComputer ? "vs Computer" : "vs Player")}");
                    System.Diagnostics.Debug.WriteLine($"Player color: {(isPlayerWhite ? "White" : "Black")}");
                    
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                        System.Diagnostics.Debug.WriteLine("Created saves directory");
                    }

                    var fullPath = System.IO.Path.Combine(savePath, $"{saveName}.json");
                    System.Diagnostics.Debug.WriteLine($"Saving to file: {fullPath}");
                    
                    await File.WriteAllTextAsync(fullPath, json);
                    System.Diagnostics.Debug.WriteLine("File saved successfully");
                    System.Diagnostics.Debug.WriteLine($"Save content: {json}");

                    // Показываем подтверждение
                    var confirmDialog = new ContentDialog
                    {
                        Title = "Game Saved",
                        Content = $"Your game has been saved as '{saveName}'\nLocation: {fullPath}",
                        CloseButtonText = "OK"
                    };
                    await confirmDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving game: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                var errorDialog = new ContentDialog
                {
                    Title = "Error Saving Game",
                    Content = $"An error occurred while saving the game: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();
            }
        }

        private void LoadSavedGame(GameSaveData saveData)
        {
            // Устанавливаем режим игры
            this.isVsComputer = saveData.IsVsComputer;
            this.isPlayerWhite = saveData.IsPlayerWhite;

            // Инициализируем доску
            gameBoard = new GameBoard();
            
            // Инициализируем компьютерного игрока, если это игра с компьютером
            if (isVsComputer)
            {
                computerPlayer = new ComputerPlayer(gameBoard, !isPlayerWhite);
            }
            else
            {
                computerPlayer = null;
            }

            // Очищаем и инициализируем доску
            BoardGrid.Children.Clear();
            InitializeBoard();

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
                if (computerTimer == null)
                {
                    computerTimer = new DispatcherTimer();
                    computerTimer.Interval = TimeSpan.FromMilliseconds(500);
                    computerTimer.Tick += ComputerTimer_Tick;
                }

                // Если сейчас ход компьютера, запускаем таймер
                if (gameBoard.IsWhiteTurn() != isPlayerWhite)
                {
                    computerTimer.Start();
                }
            }
        }

        private async void InitializeSoundPlayers()
        {
            try
            {
                // Инициализация звука перемещения
                soundPlayer = new MediaPlayer();
                var moveSoundFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/move_sound.mp3"));
                var moveStream = await moveSoundFile.OpenAsync(FileAccessMode.Read);
                soundPlayer.SetStreamSource(moveStream);
                soundPlayer.Volume = 1.0;
                soundPlayer.IsMuted = false;
                System.Diagnostics.Debug.WriteLine("Move sound initialized successfully");

                // Инициализация звука захвата
                captureSoundPlayer = new MediaPlayer();
                var captureSoundFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/capture_sound.mp3"));
                var captureStream = await captureSoundFile.OpenAsync(FileAccessMode.Read);
                captureSoundPlayer.SetStreamSource(captureStream);
                captureSoundPlayer.Volume = 1.0;
                captureSoundPlayer.IsMuted = false;
                System.Diagnostics.Debug.WriteLine("Capture sound initialized successfully");

                // Инициализация звука превращения в дамку
                kingSoundPlayer = new MediaPlayer();
                var kingSoundFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/king_sound.mp3"));
                var kingStream = await kingSoundFile.OpenAsync(FileAccessMode.Read);
                kingSoundPlayer.SetStreamSource(kingStream);
                kingSoundPlayer.Volume = 1.0;
                kingSoundPlayer.IsMuted = false;
                System.Diagnostics.Debug.WriteLine("King promotion sound initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing sound players: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void WhiteButton_Click(object sender, RoutedEventArgs e)
        {
            isPlayerWhite = true;
            WhiteDragonText.Opacity = 1;
            BlackDragonText.Opacity = 0;
            StartGameButton.IsEnabled = true;
            StartGameButton.Opacity = 1;
            StartButtonAnimation.Begin();
        }

        private void BlackButton_Click(object sender, RoutedEventArgs e)
        {
            isPlayerWhite = false;
            BlackDragonText.Opacity = 1;
            WhiteDragonText.Opacity = 0;
            StartGameButton.IsEnabled = true;
            StartGameButton.Opacity = 1;
            StartButtonAnimation.Begin();
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            // Останавливаем анимацию кнопки
            StartButtonAnimation.Stop();
            
            // Создаем параметры для игры
            var parameters = new GameParameters
            {
                IsVsComputer = false, // Игра с другом
                IsPlayerWhite = isPlayerWhite
            };

            // Переходим на страницу игры
            Frame.Navigate(typeof(GamePage), parameters);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся на предыдущую страницу
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                // Если нет предыдущей страницы, переходим на главную
                Frame.Navigate(typeof(GameModePage));
            }
        }
    }
}
