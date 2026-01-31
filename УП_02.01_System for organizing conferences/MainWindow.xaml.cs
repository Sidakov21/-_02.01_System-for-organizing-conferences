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
using System.Windows.Navigation;
using System.Windows.Shapes;
using УП_02._01_System_for_organizing_conferences.AppWindows;
using System.Data.Entity;

namespace УП_02._01_System_for_organizing_conferences
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Пользователи currentUser;
        private int userRoleId;

        public MainWindow()
        {
            InitializeComponent();
            LoadUserInfo();
            LoadData();
        }

        private void LoadUserInfo()
        {
            txtUserInfo.Text = CurrentUser.GetGreeting();
            btnLoginLogout.Content = CurrentUser.IsAuthenticated ? "Выйти" : "Войти";
        }

        private void LoadData()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Загрузка направлений
                    var directions = db.Направления.ToList();
                    cmbDirection.ItemsSource = directions;
                    cmbDirection.DisplayMemberPath = "направление";
                    cmbDirection.SelectedValuePath = "ID_направление";

                    // Загрузка мероприятий
                    LoadEvents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadEvents()
        {
            using (var db = new DB_YP_OrgConfTESTEntities())
            {
                IQueryable<МероприятияИнформационнаяБезопасность> query = db.МероприятияИнформационнаяБезопасность
                    .Include(e => e.Города)
                    .Include(e => e.Направления)
                    .Where(e => e.дата >= DateTime.Today)
                    .OrderBy(e => e.дата);

                // Применение фильтров
                if (cmbDirection.SelectedValue != null)
                {
                    int directionId = (int)cmbDirection.SelectedValue;
                    query = query.Where(e => e.Направления.ID_направление == directionId);
                }

                if (dpDate.SelectedDate.HasValue)
                {
                    query = query.Where(e => e.дата == dpDate.SelectedDate);
                }

                var events = query.ToList();
                lvEvents.ItemsSource = events;
                txtEventCount.Text = $"Найдено мероприятий: {events.Count}";
            }
        }

        private void CmbDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEvents();
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEvents();
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            cmbDirection.SelectedIndex = -1;
            dpDate.SelectedDate = null;
            LoadEvents();
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int eventId))
            {
                var detailsWindow = new EventDetailsWindow(eventId);
                detailsWindow.ShowDialog();
            }
        }

        private void LvEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvEvents.SelectedItem != null)
            {
                var selectedEvent = lvEvents.SelectedItem as МероприятияИнформационнаяБезопасность;
                var detailsWindow = new EventDetailsWindow(selectedEvent.ID_событие);
                detailsWindow.ShowDialog();
                lvEvents.SelectedItem = null;
            }
        }

        private void BtnLoginLogout_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.IsAuthenticated)
            {
                // Выход
                CurrentUser.Logout();
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            else
            {
                // Вход
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
