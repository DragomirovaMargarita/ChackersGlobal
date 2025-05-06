using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;

namespace Chackers
{
    public sealed partial class RegistrationPage : Page
    {
        private DatabaseService dbService = new DatabaseService();
        public RegistrationPage()
        {
            this.InitializeComponent();
            CheckDatabaseConnection();
            AddShadowEffect();
        }

        private void AddShadowEffect()
        {
            var compositor = ElementCompositionPreview.GetElementVisual(MainBorder).Compositor;
            var shadow = compositor.CreateDropShadow();
            shadow.Color = Windows.UI.Color.FromArgb(64, 0, 0, 0);
            shadow.BlurRadius = 10f;
            shadow.Offset = new System.Numerics.Vector3(0, 2, 0);

            var spriteVisual = compositor.CreateSpriteVisual();
            spriteVisual.Shadow = shadow;

            var visual = ElementCompositionPreview.GetElementVisual(MainBorder);
            ElementCompositionPreview.SetElementChildVisual(MainBorder, spriteVisual);
        }

        private async void CheckDatabaseConnection()
        {
            try
            {
                // Проверяем подключение к базе данных
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection("server=localhost;user=root;password=345403;database=Chackers;"))
                {
                    await conn.OpenAsync();
                    MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                    MessageBlock.Text = "Database connection is established";
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                MessageBlock.Text = "Database connection error. Check MySQL settings.";
                System.Diagnostics.Debug.WriteLine($"Database connection error: {ex.Message}");
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBlock.Text = "";
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBlock.Text = "Пожалуйста, заполните все поля.";
                return;
            }
            
            if (password.Length < 5)
            {
                MessageBlock.Text = "Пароль должен быть не короче 5 символов.";
                return;
            }

            try
            {
                bool success = dbService.Register(username, password);
                if (success)
                {
                    int? userId = dbService.Login(username, password);
                    if (userId != null)
                    {
                        MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                        MessageBlock.Text = "Регистрация успешна! Входим...";
                        await Task.Delay(800);
                        Frame.Navigate(typeof(GameModePage), userId.Value);
                    }
                    else
                    {
                        MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                        MessageBlock.Text = "Ошибка: не удалось получить userId после регистрации.";
                    }
                }
                else
                {
                    MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                    MessageBlock.Text = "Логин уже существует или ошибка подключения.";
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                MessageBlock.Text = "Произошла ошибка при регистрации.";
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBlock.Text = "";
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBlock.Text = "Пожалуйста, заполните все поля.";
                return;
            }

            try
            {
                int? userId = dbService.Login(username, password);
                if (userId != null)
                {
                    MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                    MessageBlock.Text = "Вход выполнен!";
                    await Task.Delay(500);
                    Frame.Navigate(typeof(GameModePage), userId.Value);
                }
                else
                {
                    MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                    MessageBlock.Text = "Неверный логин или пароль.";
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                MessageBlock.Text = "Произошла ошибка при входе.";
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            }
        }

        private void VsComputerButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем userId из параметров текущей страницы
            int? userId = null;
            if (Frame.BackStackDepth > 0 && Frame.BackStack[Frame.BackStackDepth - 1].Parameter is int id)
                userId = id;
            // Если не удалось получить из BackStack, пробуем из dbService (если есть логика хранения)
            // Переходим на ColorSelectionPage с userId
            if (userId != null)
                Frame.Navigate(typeof(ColorSelectionPage), userId.Value);
            else
                Frame.Navigate(typeof(ColorSelectionPage));
        }
    }
} 