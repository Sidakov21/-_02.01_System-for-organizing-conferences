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
using УП_02._01_System_for_organizing_conferences.Events;

namespace УП_02._01_System_for_organizing_conferences.AppWindows
{
    /// <summary>
    /// Логика взаимодействия для OrganizerDashboardPage.xaml
    /// </summary>
    public partial class OrganizerDashboardPage : Page
    {
        public OrganizerDashboardPage()
        {
            InitializeComponent();
            Loaded += OrganizerDashboardPage_Loaded;
        }

        private void OrganizerDashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
            LoadUpcomingEvents();
        }

        private void LoadStatistics()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Общее количество мероприятий
                    var totalEvents = db.МероприятияИнформационнаяБезопасность.Count();
                    txtTotalEvents.Text = totalEvents.ToString();

                    // Количество участников (пользователей с ролью 1)
                    var totalParticipants = db.Пользователи.Count(u => u.роли_ID == 1);
                    txtTotalParticipants.Text = totalParticipants.ToString();

                    // Количество активностей
                    var totalActivities = db.Активности.Count();
                    txtTotalActivities.Text = totalActivities.ToString();

                    // Ближайшие мероприятия (в течение 30 дней) - ИСПРАВЛЕНО
                    var today = DateTime.Today;
                    var in30Days = today.AddDays(30);
                    var upcomingEvents = db.МероприятияИнформационнаяБезопасность
                        .Count(e => e.дата >= today && e.дата <= in30Days);
                    txtUpcomingEvents.Text = upcomingEvents.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUpcomingEvents()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var upcomingEvents = db.МероприятияИнформационнаяБезопасность
                        .Include(e => e.Города)
                        .Where(e => e.дата >= DateTime.Today)
                        .OrderBy(e => e.дата)
                        .Take(10)
                        .ToList();

                    lvUpcomingEvents.ItemsSource = upcomingEvents;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnManageEvent_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int eventId))
            {
                // Открыть окно управления мероприятием
                var manageWindow = new ManageEventWindow(eventId);
                manageWindow.ShowDialog();
                LoadStatistics();
                LoadUpcomingEvents();
            }
        }

        private void BtnViewAllEvents_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу мероприятий
            var organizerWindow = Window.GetWindow(this) as OrganizerWindow;
            organizerWindow?.BtnEvents_Click(sender, e);
        }
    }
}
