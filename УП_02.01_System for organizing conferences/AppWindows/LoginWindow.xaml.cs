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

namespace УП_02._01_System_for_organizing_conferences.AppWindows
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUserId.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация ввода
                if (string.IsNullOrWhiteSpace(txtUserId.Text))
                {
                    ShowError("Введите ID пользователя");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    ShowError("Введите пароль");
                    return;
                }

                int userId;
                if (!int.TryParse(txtUserId.Text, out userId))
                {
                    ShowError("ID должен быть числом");
                    return;
                }

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Поиск пользователя
                    var user = db.Пользователи
                        .Include("Роли")
                        .FirstOrDefault(u => u.ID_пользователи == userId && u.пароль == txtPassword.Password);

                    if (user == null)
                    {
                        ShowError("Неверный ID или пароль");
                        return;
                    }

                    // Сохранение текущего пользователя
                    CurrentUser.Initialize(user);

                    // Открытие соответствующего окна по роли
                    OpenRoleBasedWindow(user);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            // Открытие главного окна как гость
            CurrentUser.Initialize(null);
            OpenMainWindow();
        }

        private void OpenRoleBasedWindow(Пользователи user)
        {
            switch (user.роли_ID)
            {
                case 2: // Организатор
                    OpenOrganizerWindow();
                    break;
                case 3: // Модератор
                    OpenModeratorWindow();
                    break;
                case 4: // Жюри
                    OpenJuryWindow();
                    break;
                default: // Участник и остальные
                    OpenMainWindow();
                    break;
            }
        }

        private void OpenMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void OpenOrganizerWindow()
        {
            var organizerWindow = new OrganizerWindow();
            organizerWindow.Show();
            this.Close();
        }

        private void OpenModeratorWindow()
        {
            // Реализовать позже
            MessageBox.Show("Окно модератора в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
            OpenMainWindow();
        }

        private void OpenJuryWindow()
        {
            // Реализовать позже
            MessageBox.Show("Окно жюри в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
            OpenMainWindow();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;

            // Автоматическое скрытие через 5 секунд
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, args) =>
            {
                lblError.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
    }
}
