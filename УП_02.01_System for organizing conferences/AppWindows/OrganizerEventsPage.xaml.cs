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
    /// Логика взаимодействия для OrganizerEventsPage.xaml
    /// </summary>
    public partial class OrganizerEventsPage : Window
    {
        public OrganizerEventsPage()
        {
            InitializeComponent();
            Loaded += OrganizerEventsPage_Loaded;
        }

        private void OrganizerEventsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEvents();
        }

        private void LoadEvents()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var query = db.МероприятияИнформационнаяБезопасность
                        .Include(e => e.Города)
                        .Include(e => e.Активности)
                        .AsQueryable();

                    // Применение поиска
                    if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    {
                        string searchTerm = txtSearch.Text.ToLower();
                        query = query.Where(e =>
                            e.событие.ToLower().Contains(searchTerm) ||
                            e.Города.город.ToLower().Contains(searchTerm));
                    }

                    // Применение фильтра по дате
                    if (cmbDateFilter.SelectedItem != null)
                    {
                        string filter = (cmbDateFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

                        switch (filter)
                        {
                            case "Сегодня":
                                query = query.Where(e => e.дата == DateTime.Today);
                                break;
                            case "Завтра":
                                query = query.Where(e => e.дата == DateTime.Today.AddDays(1));
                                break;
                            case "Эта неделя":
                                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
                                var endOfWeek = startOfWeek.AddDays(6);
                                query = query.Where(e => e.дата >= startOfWeek && e.дата <= endOfWeek);
                                break;
                            case "Этот месяц":
                                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                                query = query.Where(e => e.дата >= startOfMonth && e.дата <= endOfMonth);
                                break;
                            case "Прошедшие":
                                query = query.Where(e => e.дата < DateTime.Today);
                                break;
                            case "Будущие":
                                query = query.Where(e => e.дата >= DateTime.Today);
                                break;
                        }
                    }

                    // Применение сортировки
                    if (cmbSortBy.SelectedItem != null)
                    {
                        string sortBy = (cmbSortBy.SelectedItem as ComboBoxItem)?.Content.ToString();

                        switch (sortBy)
                        {
                            case "По дате (возр)":
                                query = query.OrderBy(e => e.дата);
                                break;
                            case "По дате (убыв)":
                                query = query.OrderByDescending(e => e.дата);
                                break;
                            case "По названию (А-Я)":
                                query = query.OrderBy(e => e.событие);
                                break;
                            case "По названию (Я-А)":
                                query = query.OrderByDescending(e => e.событие);
                                break;
                        }
                    }

                    var events = query.ToList();
                    lvEvents.ItemsSource = events;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadEvents();
        }

        private void CmbDateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEvents();
        }

        private void CmbSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEvents();
        }

        private void BtnCreateEvent_Click(object sender, RoutedEventArgs e)
        {
            var createEventWindow = new CreateEventWindow();
            if (createEventWindow.ShowDialog() == true)
            {
                LoadEvents();
            }
        }

        private void BtnManage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int eventId))
            {
                var manageWindow = new ManageEventWindow(eventId);
                if (manageWindow.ShowDialog() == true)
                {
                    LoadEvents();
                }
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int eventId))
            {
                var editWindow = new EditEventWindow(eventId);
                if (editWindow.ShowDialog() == true)
                {
                    LoadEvents();
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int eventId))
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить это мероприятие?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new DB_YP_OrgConfTESTEntities())
                        {
                            var eventToDelete = db.МероприятияИнформационнаяБезопасность
                                .Include(z => z.Активности)
                                .FirstOrDefault(z => z.ID_событие == eventId);

                            if (eventToDelete != null)
                            {
                                // Проверка на наличие активностей
                                if (eventToDelete.Активности.Any())
                                {
                                    MessageBox.Show("Невозможно удалить мероприятие, так как в нем есть активности. " +
                                        "Сначала удалите все активности.", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                db.МероприятияИнформационнаяБезопасность.Remove(eventToDelete);
                                db.SaveChanges();

                                MessageBox.Show("Мероприятие успешно удалено", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                                LoadEvents();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления мероприятия: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
