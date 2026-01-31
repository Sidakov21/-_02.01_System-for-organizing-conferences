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

namespace УП_02._01_System_for_organizing_conferences.Events
{
    /// <summary>
    /// Логика взаимодействия для ManageEventWindow.xaml
    /// </summary>
    public partial class ManageEventWindow : Window
    {
        private int eventId;
        private МероприятияИнформационнаяБезопасность currentEvent;

        public ManageEventWindow(int eventId)
        {
            InitializeComponent();
            this.eventId = eventId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEventData();
            LoadActivities();
            LoadParticipants();
        }

        private void LoadEventData()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    currentEvent = db.МероприятияИнформационнаяБезопасность
                        .Include(e => e.Города)
                        .FirstOrDefault(e => e.ID_событие == eventId);

                    if (currentEvent == null)
                    {
                        MessageBox.Show("Мероприятие не найдено", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Заполнение информации
                    txtEventTitle.Text = currentEvent.событие;
                    txtEventDetails.Text = $"{currentEvent.дата?.ToString("dd.MM.yyyy")} | {currentEvent.Города?.город} | {currentEvent.дни} дней";

                    txtInfoName.Text = currentEvent.событие;
                    txtInfoDate.Text = currentEvent.дата?.ToString("dd.MM.yyyy");
                    txtInfoCity.Text = currentEvent.Города?.город;
                    txtInfoDuration.Text = $"{currentEvent.дни} дней";
                    // TODO: Добавить поле описания в таблицу
                    // txtInfoDescription.Text = currentEvent.описание;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных мероприятия: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadActivities()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var activities = db.Активности
                        .Include(a => a.Пользователи)
                        .Include(a => a.АктивностиЖюри)
                        .Where(a => a.Мероприятие_ID == eventId)
                        .OrderBy(a => a.День)
                        .ThenBy(a => a.Время_начала)
                        .ToList();

                    lvActivities.ItemsSource = activities;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки активностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadParticipants()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Получаем участников, которые зарегистрированы на это мероприятие
                    var participants = db.РолиПользователей
                        .Where(rp => rp.мероприятие_ID == eventId && rp.роли_ID == 1) // 1 - участник
                        .Select(rp => rp.Пользователи)
                        .ToList();

                    dgParticipants.ItemsSource = participants;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtActivitySearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterActivities();
        }

        private void CmbDayFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            FilterActivities();
        }

        private void FilterActivities()
        {
            // Реализация фильтрации будет в LoadActivities
            LoadActivities();
        }

        private void BtnAddActivity_Click(object sender, RoutedEventArgs e)
        {
            var addActivityWindow = new AddActivityWindow(eventId);
            if (addActivityWindow.ShowDialog() == true)
            {
                LoadActivities();
            }
        }

        private void BtnEditActivity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int activityId))
            {
                var editActivityWindow = new EditActivityWindow(activityId);
                if (editActivityWindow.ShowDialog() == true)
                {
                    LoadActivities();
                }
            }
        }

        private void BtnAssignJury_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int activityId))
            {
                var assignJuryWindow = new AssignJuryWindow(activityId);
                if (assignJuryWindow.ShowDialog() == true)
                {
                    LoadActivities();
                }
            }
        }

        private void BtnDeleteActivity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int activityId))
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить эту активность?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new DB_YP_OrgConfTESTEntities())
                        {
                            var activity = db.Активности
                                .Include(a => a.АктивностиЖюри)
                                .FirstOrDefault(a => a.ID_Активности == activityId);

                            if (activity != null)
                            {
                                // Проверка: есть ли назначенное жюри
                                if (activity.АктивностиЖюри_ID != null)
                                {
                                    MessageBox.Show("Невозможно удалить активность, для которой назначено жюри.",
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                db.Активности.Remove(activity);
                                db.SaveChanges();

                                MessageBox.Show("Активность успешно удалена", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                                LoadActivities();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnEditEventInfo_Click(object sender, RoutedEventArgs e)
        {
            var editEventWindow = new EditEventWindow(eventId);
            if (editEventWindow.ShowDialog() == true)
            {
                LoadEventData();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
