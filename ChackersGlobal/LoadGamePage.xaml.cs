using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using System.Collections.Generic;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Chackers
{
    public sealed partial class LoadGamePage : Page
    {
        private MediaPlayer hoverSoundPlayer;
        private int userId;

        public LoadGamePage()
        {
            this.InitializeComponent();
            InitializeHoverSound();
            AddHoverHandlers();
            LoadSavedGames();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int id)
                userId = id;
        }

        private async void InitializeHoverSound()
        {
            try
            {
                hoverSoundPlayer = new MediaPlayer();
                var soundFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/hover_sound.mp3"));
                var stream = await soundFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                hoverSoundPlayer.SetStreamSource(stream);
                hoverSoundPlayer.Volume = 0.3; // Lower volume for hover sound
                hoverSoundPlayer.PlaybackRate = 2.0; // Play sound 2 times faster
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing hover sound: {ex.Message}");
            }
        }

        private void AddHoverHandlers()
        {
            BackButton.PointerEntered += Button_PointerEntered;
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                if (hoverSoundPlayer != null)
                {
                    hoverSoundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing hover sound: {ex.Message}");
            }
        }

        private async void LoadSavedGames()
        {
            try
            {
                var savePath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "saves");
                System.Diagnostics.Debug.WriteLine($"Looking for saves in: {savePath}");

                if (!Directory.Exists(savePath))
                {
                    System.Diagnostics.Debug.WriteLine("Saves directory does not exist");
                    NoSavesText.Visibility = Visibility.Visible;
                    return;
                }

                var saveFiles = Directory.GetFiles(savePath, "*.json");
                System.Diagnostics.Debug.WriteLine($"Found {saveFiles.Length} save files");

                if (saveFiles.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No save files found");
                    NoSavesText.Visibility = Visibility.Visible;
                    return;
                }

                NoSavesText.Visibility = Visibility.Collapsed;
                SavesList.Children.Clear(); // Очищаем список перед добавлением

                foreach (var file in saveFiles.OrderByDescending(f => File.GetLastWriteTime(f)))
                {
                    System.Diagnostics.Debug.WriteLine($"Processing save file: {file}");
                    var json = await File.ReadAllTextAsync(file);
                    System.Diagnostics.Debug.WriteLine($"File content: {json}");

                    var saveData = JsonConvert.DeserializeObject<GameSaveData>(json);
                    System.Diagnostics.Debug.WriteLine($"Loaded save: {saveData.SaveName} from {saveData.SaveDate}");
                    System.Diagnostics.Debug.WriteLine($"Game mode: {(saveData.IsVsComputer ? "vs Computer" : "vs Player")}");
                    System.Diagnostics.Debug.WriteLine($"Player color: {(saveData.IsPlayerWhite ? "White" : "Black")}");

                    var button = new Button
                    {
                        Style = (Style)Resources["SaveItemStyle"]
                    };

                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Vertical
                    };

                    panel.Children.Add(new TextBlock
                    {
                        Text = saveData.SaveName,
                        FontSize = 20,
                        Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gold)
                    });

                    panel.Children.Add(new TextBlock
                    {
                        Text = $"Saved on {saveData.SaveDate:g}",
                        FontSize = 14,
                        Opacity = 0.8,
                        Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gold)
                    });

                    // Добавляем информацию о режиме игры
                    panel.Children.Add(new TextBlock
                    {
                        Text = saveData.IsVsComputer ? 
                            $"vs Computer ({(saveData.IsPlayerWhite ? "White" : "Black")})" : 
                            "vs Player",
                        FontSize = 14,
                        Opacity = 0.8,
                        Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gold)
                    });

                    button.Content = panel;
                    button.Tag = saveData;
                    button.Click += SaveButton_Click;

                    SavesList.Children.Add(button);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saves: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to load saved games: {ex.Message}",
                    CloseButtonText = "OK"
                };
                dialog.ShowAsync();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var saveData = (GameSaveData)button.Tag;
            var parameters = new GameLoadParameters { SaveData = saveData, UserId = this.userId };
            Frame.Navigate(typeof(GamePage), parameters);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GameModePage));
        }
    }

    public class SaveGameTemp
    {
        public DateTime SaveDate { get; set; }
        public string SaveName { get; set; }
        public bool IsWhiteTurn { get; set; }
        public bool IsVsComputer { get; set; }
        public bool IsPlayerWhite { get; set; }
        public List<PieceDataTemp> Pieces { get; set; }
    }

    public class PieceDataTemp
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsWhite { get; set; }
        public bool IsKing { get; set; }
    }
} 