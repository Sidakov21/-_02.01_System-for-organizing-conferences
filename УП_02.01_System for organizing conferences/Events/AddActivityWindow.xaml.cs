using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace УП_02._01_System_for_organizing_conferences.Events
{
    public partial class AddActivityWindow : Window
    {
        private int eventId;
        private МероприятияИнформационнаяБезопасность currentEvent;

        public AddActivityWindow(int eventId)
        {
            InitializeComponent();
            this.eventId = eventId;
            Loaded += AddActivityWindow_Loaded;
        }

        private void AddActivityWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Загрузка мероприятия
                    currentEvent = db.МероприятияИнформационнаяБезопасность
                        .FirstOrDefault(e => e.ID_событие == eventId);

                    if (currentEvent == null)
                    {
                        MessageBox.Show("Мероприятие не найдено", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Загрузка всех мероприятий для выпадающего списка
                    var events = db.МероприятияИнформационнаяБезопасность
                        .Where(e => e.дата >= DateTime.Today)
                        .OrderBy(e => e.дата)
                        .ToList();
                    cmbEvent.ItemsSource = events;
                    cmbEvent.DisplayMemberPath = "событие";
                    cmbEvent.SelectedValuePath = "ID_событие";
                    cmbEvent.SelectedValue = eventId; // Предвыбранное мероприятие

                    // Загрузка модераторов (роль 3)
                    var moderators = db.Пользователи
                        .Where(u => u.роли_ID == 3)
                        .ToList();
                    cmbModerator.ItemsSource = moderators;
                    cmbModerator.DisplayMemberPath = "фио";
                    cmbModerator.SelectedValuePath = "ID_пользователи";

                    // Заполнение дней мероприятия
                    for (int i = 1; i <= currentEvent.дни; i++)
                    {
                        cmbDay.Items.Add($"День {i}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbEvent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEvent.SelectedValue is int selectedEventId)
            {
                eventId = selectedEventId;
                LoadEventDetails();
            }
        }

        private void LoadEventDetails()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    currentEvent = db.МероприятияИнформационнаяБезопасность
                        .FirstOrDefault(e => e.ID_событие == eventId);

                    if (currentEvent != null)
                    {
                        // Обновляем список дней
                        cmbDay.Items.Clear();
                        for (int i = 1; i <= currentEvent.дни; i++)
                        {
                            cmbDay.Items.Add($"День {i}");
                        }
                        cmbDay.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbDay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAvailableTimeSlots();
            cmbStartTime.IsEnabled = cmbDay.SelectedItem != null;
        }

        private void UpdateAvailableTimeSlots()
        {
            try
            {
                if (cmbDay.SelectedIndex == -1 || currentEvent == null)
                    return;

                int selectedDay = cmbDay.SelectedIndex + 1;

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Получаем уже существующие активности для этого дня
                    var existingActivities = db.Активности
                        .Where(a => a.Мероприятие_ID == eventId && a.День == selectedDay)
                        .OrderBy(a => a.Время_начала)
                        .ToList();

                    // Генерируем доступные временные слоты
                    var availableTimes = GenerateTimeSlots(existingActivities);

                    cmbStartTime.Items.Clear();
                    foreach (var time in availableTimes)
                    {
                        cmbStartTime.Items.Add(time.ToString("HH:mm"));
                    }

                    if (cmbStartTime.Items.Count > 0)
                        cmbStartTime.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации расписания: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<DateTime> GenerateTimeSlots(List<Активности> existingActivities)
        {
            var availableSlots = new List<DateTime>();

            if (currentEvent == null || !currentEvent.дата.HasValue)
                return availableSlots;

            DateTime eventDate = currentEvent.дата.Value;
            DateTime currentDayStart = eventDate.AddDays(cmbDay.SelectedIndex);

            // Рабочий день с 9:00 до 18:00
            TimeSpan workStart = new TimeSpan(9, 0, 0);
            TimeSpan workEnd = new TimeSpan(18, 0, 0);
            TimeSpan activityDuration = TimeSpan.FromMinutes(90);
            TimeSpan breakDuration = TimeSpan.FromMinutes(15);
            TimeSpan totalSlotDuration = activityDuration + breakDuration;

            // Текущее время для проверки
            DateTime currentSlotStart = currentDayStart.Date + workStart;
            DateTime workEndTime = currentDayStart.Date + workEnd;

            while (currentSlotStart + activityDuration <= workEndTime)
            {
                bool slotAvailable = true;

                // Проверяем пересечение с существующими активностями
                foreach (var activity in existingActivities)
                {
                    DateTime activityStart = activity.Время_начала ?? DateTime.MinValue;
                    DateTime activityEnd = activityStart + activityDuration;

                    // Проверка наложения (с учетом перерыва в 15 минут)
                    if (currentSlotStart < activityEnd + breakDuration &&
                        currentSlotStart + activityDuration > activityStart - breakDuration)
                    {
                        slotAvailable = false;
                        break;
                    }
                }

                if (slotAvailable)
                {
                    availableSlots.Add(currentSlotStart);
                }

                // Переходим к следующему слоту
                currentSlotStart = currentSlotStart + totalSlotDuration;
            }

            return availableSlots;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowError("Введите название активности");
                    return;
                }

                if (cmbEvent.SelectedItem == null)
                {
                    ShowError("Выберите мероприятие");
                    return;
                }

                if (cmbDay.SelectedItem == null)
                {
                    ShowError("Выберите день");
                    return;
                }

                if (cmbStartTime.SelectedItem == null)
                {
                    ShowError("Выберите время начала");
                    return;
                }

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Создание активности
                    var activity = new Активности
                    {
                        Активность = txtName.Text,
                        описание = txtDescription.Text,
                        Мероприятие_ID = eventId,
                        День = cmbDay.SelectedIndex + 1,
                        Время_начала = currentEvent.дата.Value.AddDays(cmbDay.SelectedIndex).Date +
                                      TimeSpan.Parse(cmbStartTime.SelectedItem.ToString()),
                        Модератор_ID = cmbModerator.SelectedValue as int?
                    };

                    db.Активности.Add(activity);
                    db.SaveChanges();

                    MessageBox.Show("Активность успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}