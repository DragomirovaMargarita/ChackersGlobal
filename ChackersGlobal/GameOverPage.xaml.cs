using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Chackers
{
    public sealed partial class GameOverPage : Page
    {
        private bool isVsComputer;
        private bool isWhiteWinner;
        private bool isPlayerWhite;
        private int userId;
        private int durationSeconds;
        private int points;
        private int movesCount;

        public GameOverPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is GameOverParameters parameters)
            {
                isVsComputer = parameters.IsVsComputer;
                isWhiteWinner = parameters.IsWhiteWinner;
                isPlayerWhite = parameters.IsPlayerWhite;
                userId = parameters.UserId;
                durationSeconds = parameters.DurationSeconds;
                points = parameters.Points;
                movesCount = parameters.MovesCount;
                UpdateVictoryTitle();
            }
        }

        private void UpdateVictoryTitle()
        {
            VictoryTitle.Text = isWhiteWinner ? "White Army won! ⚔️" : "Black Army won! ⚔️";
            if (isVsComputer)
            {
                bool playerWon = (isPlayerWhite && isWhiteWinner) || (!isPlayerWhite && !isWhiteWinner);
                System.Diagnostics.Debug.WriteLine($"[GameOverPage] userId={userId}, playerWon={playerWon}");
                var db = new DatabaseService();
                if (playerWon)
                    db.AddWin(userId);
                else
                    db.AddLoss(userId);
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            var parameters = new GameParameters
            {
                IsVsComputer = isVsComputer,
                IsPlayerWhite = isPlayerWhite,
                UserId = userId
            };
            Frame.Navigate(typeof(GamePage), parameters);
        }

        private void NewModeButton_Click(object sender, RoutedEventArgs e)
        {
            var parameters = new GameParameters
            {
                IsVsComputer = isVsComputer,
                IsPlayerWhite = isPlayerWhite,
                UserId = userId
            };
            Frame.Navigate(typeof(GameModePage), parameters);
        }

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var parameters = new GameParameters
            {
                IsVsComputer = isVsComputer,
                IsPlayerWhite = isPlayerWhite,
                UserId = userId
            };
            Frame.Navigate(typeof(GameModePage), parameters);
        }
    }

    public class GameOverParameters
    {
        public bool IsVsComputer { get; set; }
        public bool IsWhiteWinner { get; set; }
        public bool IsPlayerWhite { get; set; }
        public int UserId { get; set; }
        public int DurationSeconds { get; set; }
        public int Points { get; set; }
        public int MovesCount { get; set; }
    }
} 