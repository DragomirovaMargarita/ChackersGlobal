using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Input;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.UI.Xaml.Media.Animation;
using System.Threading.Tasks;

namespace Chackers
{
    public sealed partial class ColorSelectionPage : Page
    {
        private bool isWhiteSelected = false;
        private bool isBlackSelected = false;
        private MediaPlayer hoverSoundPlayer;
        private int userId;

        public ColorSelectionPage()
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
            
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                if (hoverSoundPlayer != null)
                {
                    hoverSoundPlayer.Play();
                }

                if (sender == WhiteButton)
                {
                    WhiteDragonText.Opacity = 1;
                }
                else if (sender == BlackButton)
                {
                    BlackDragonText.Opacity = 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing hover sound: {ex.Message}");
            }
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender == WhiteButton && !isWhiteSelected)
            {
                WhiteDragonText.Opacity = 0;
            }
            else if (sender == BlackButton && !isBlackSelected)
            {
                BlackDragonText.Opacity = 0;
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

        private void PlaySelectPieceSound()
        {
            try
            {
                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/select_piece.mp3"));
                player.Volume = 1.0;
                player.Play();
                player.MediaEnded += (s, e) => { player.Dispose(); };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing select piece sound: {ex.Message}");
            }
        }

        private async void AnimateScale(ScaleTransform scale)
        {
            scale.ScaleX = 0.85;
            scale.ScaleY = 0.85;
            await Task.Delay(90);
            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;
        }

        private void WhiteButton_Click(object sender, RoutedEventArgs e)
        {
            PlaySelectPieceSound();
            AnimateScale(WhitePieceScale);
            isWhiteSelected = true;
            isBlackSelected = false;
            UpdateButtonStates();
            
           
            var whiteBrush = new ImageBrush();
            whiteBrush.ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/white_king.png"));
            WhitePieceImage.Fill = whiteBrush;

            var blackBrush = new ImageBrush();
            blackBrush.ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/black_piece.png"));
            BlackPieceImage.Fill = blackBrush;
            
            
            WhiteDragonText.Opacity = 1;
            BlackDragonText.Opacity = 0;
        }

        private void BlackButton_Click(object sender, RoutedEventArgs e)
        {
            PlaySelectPieceSound();
            AnimateScale(BlackPieceScale);
            isBlackSelected = true;
            isWhiteSelected = false;
            UpdateButtonStates();
            
            
            var blackBrush = new ImageBrush();
            blackBrush.ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/black_king.png"));
            BlackPieceImage.Fill = blackBrush;

            var whiteBrush = new ImageBrush();
            whiteBrush.ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/white_piece.png"));
            WhitePieceImage.Fill = whiteBrush;
            
            
            BlackDragonText.Opacity = 1;
            WhiteDragonText.Opacity = 0;
        }

        private void UpdateButtonStates()
        {
            
            WhiteButton.BorderBrush = isWhiteSelected ? new SolidColorBrush(Colors.Gold) : new SolidColorBrush(Colors.Transparent);
            BlackButton.BorderBrush = isBlackSelected ? new SolidColorBrush(Colors.Gold) : new SolidColorBrush(Colors.Transparent);

            
            if (isWhiteSelected || isBlackSelected)
            {
                StartGameButton.IsEnabled = true;
                StartGameButton.Opacity = 1;
                StartButtonAnimation.Begin();
            }
            else
            {
                StartGameButton.IsEnabled = false;
                StartGameButton.Opacity = 0.5;
                StartButtonAnimation.Stop();
            }
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            if (!isWhiteSelected && !isBlackSelected)
            {
                
                var dialog = new ContentDialog
                {
                    Title = "Select Color",
                    Content = "Please select a color before starting the game.",
                    CloseButtonText = "OK"
                };
                dialog.ShowAsync();
                return;
            }

            try
            {
                var parameters = new GameParameters
                {
                    IsVsComputer = true,
                    IsPlayerWhite = isWhiteSelected,
                    UserId = userId
                };
                Frame.Navigate(typeof(GamePage), parameters);
            }
            catch (Exception ex)
            {
              
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"An error occurred while starting the game: {ex.Message}",
                    CloseButtonText = "OK"
                };
                dialog.ShowAsync();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            Frame.Navigate(typeof(GameModePage));
        }

        private void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
} 