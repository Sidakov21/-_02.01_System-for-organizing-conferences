using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.Entity;

namespace УП_02._01_System_for_organizing_conferences.AppWindows
{
    /// <summary>
    /// Логика взаимодействия для OrganizerWindow.xaml
    /// </summary>
    public partial class OrganizerWindow : Window
    {
        public OrganizerWindow()
        {
            InitializeComponent();

            // Проверка прав доступа
            if (!CurrentUser.IsAuthenticated || CurrentUser.RoleId != 2)
            {
                MessageBox.Show("Доступ запрещен. Требуется роль организатора", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
                return;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserInfo();
            ShowDashboard();
        }

        private void LoadUserInfo()
        {
            try
            {
                // Приветствие
                txtGreeting.Text = CurrentUser.GetGreeting();
                txtUserRole.Text = $"Роль: {CurrentUser.RoleName}";

                // Загрузка фото
                if (!string.IsNullOrEmpty(CurrentUser.User.фото))
                {
                    try
                    {
                        var bitmap = new BitmapImage(new Uri(CurrentUser.User.фото, UriKind.RelativeOrAbsolute));
                        imgUserPhoto.Source = bitmap;
                    }
                    catch
                    {
                        // Если фото не загрузилось, используем заглушку
                        imgUserPhoto.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/DefaultUser.png"));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDashboard()
        {
            var dashboardPage = new OrganizerDashboardPage();
            mainFrame.Navigate(dashboardPage);
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        public void BtnEvents_Click(object sender, RoutedEventArgs e)
        {
            var eventsPage = new OrganizerEventsPage();
            mainFrame.Navigate(eventsPage);
        }

        private void BtnCreateEvent_Click(object sender, RoutedEventArgs e)
        {
            var createEventWindow = new CreateEventWindow();
            if (createEventWindow.ShowDialog() == true)
            {
                // Обновить список мероприятий если нужно
                ShowDashboard();
            }
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new OrganizerProfileWindow();
            if (profileWindow.ShowDialog() == true)
            {
                // Обновить информацию о пользователе
                LoadUserInfo();
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            CurrentUser.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
