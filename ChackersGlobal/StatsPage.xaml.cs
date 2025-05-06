using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Chackers;

namespace Chackers
{
    public sealed partial class StatsPage : Page
    {
        public StatsPage()
        {
            this.InitializeComponent();
            LoadStats();
        }

        private void LoadStats()
        {
            var db = new DatabaseService();
            var stats = db.GetAllUserStats(); // Должен возвращать List<UserStat> с UserName, Wins, Losses

            // Заголовки
            StatsTable.RowDefinitions.Clear();
            StatsTable.Children.Clear();
            StatsTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddStatsCell("Player", 0, 0, true);
            AddStatsCell("Wins", 0, 1, true);
            AddStatsCell("Losses", 0, 2, true);

            int row = 1;
            foreach (var stat in stats)
            {
                StatsTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddStatsCell(stat.UserName, row, 0);
                AddStatsCell(stat.Wins.ToString(), row, 1);
                AddStatsCell(stat.Losses.ToString(), row, 2);
                row++;
            }
        }

        private void AddStatsCell(string text, int row, int col, bool isHeader = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = isHeader ? 24 : 20,
                FontWeight = isHeader ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal,
                Foreground = new SolidColorBrush(isHeader ? Colors.Gold : Colors.White),
                Margin = new Thickness(8),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, col);
            StatsTable.Children.Add(tb);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}