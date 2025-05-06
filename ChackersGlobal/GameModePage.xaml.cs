using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.UI.Xaml.Input;

namespace Chackers
{
    public sealed partial class GameModePage : Page
    {
        private MediaPlayer hoverSoundPlayer;
        private int userId;

        public GameModePage()
        {
            this.InitializeComponent();
            InitializeHoverSound();
            AddHoverHandlers();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int id)
                userId = id;
            else if (e.Parameter is GameParameters parameters)
                userId = parameters.UserId;
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
            VsComputerButton.PointerEntered += Button_PointerEntered;
            VsPlayerButton.PointerEntered += Button_PointerEntered;
            LoadGameButton.PointerEntered += Button_PointerEntered;
            ExitButton.PointerEntered += Button_PointerEntered;
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

        private void PlayButtonSound()
        {
            try
            {
                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/button_click.mp3"));
                player.Volume = 1.0;
                player.Play();
                player.MediaEnded += (s, e) => { player.Dispose(); };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing button sound: {ex.Message}");
            }
        }

        private void VsComputerButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            Frame.Navigate(typeof(ColorSelectionPage), userId);
        }

        private void VsPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            var parameters = new GameParameters
            {
                IsVsComputer = false,
                IsPlayerWhite = true, // не важно, оба игрока — люди
                UserId = userId
            };
            Frame.Navigate(typeof(GamePage), parameters);
        }

        private void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            Frame.Navigate(typeof(LoadGamePage), userId);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            Application.Current.Exit();
        }

        private async void ViewPlayersButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            var db = new DatabaseService();
            var users = db.GetAllUsers();
            string message = users.Count > 0 ? string.Join("\n", users) : "No players found.";
            var dialog = new ContentDialog
            {
                Title = "Players",
                Content = message,
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }

        private async void MyStatsButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            var db = new DatabaseService();
            var stats = db.GetUserStats(userId);
            string message = $"Won: {stats.Wins}\nLoss: {stats.Losses}";
            var dialog = new ContentDialog
            {
                Title = "Your statistics",
                Content = message,
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(StatsPage));
        }
    }
} 