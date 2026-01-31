using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace УП_02._01_System_for_organizing_conferences.Events
{
    public partial class EditActivityWindow : Window
    {
        private int activityId;
        private Активности currentActivity;
        private МероприятияИнформационнаяБезопасность currentEvent;

        public EditActivityWindow(int activityId)
        {
            InitializeComponent();
            this.activityId = activityId;
            Loaded += EditActivityWindow_Loaded;
        }

        private void EditActivityWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadActivityData();
        }

        private void LoadActivityData()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Загрузка активности со связанными данными
                    currentActivity = db.Активности
                        .Include(a => a.МероприятияИнформационнаяБезопасность)
                        .Include(a => a.Пользователи)
                        .Include(a => a.АктивностиЖюри)
                        .FirstOrDefault(a => a.ID_Активности == activityId);

                    if (currentActivity == null)
                    {
                        MessageBox.Show("Активность не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Загрузка мероприятия
                    currentEvent = currentActivity.МероприятияИнформационнаяБезопасность;

                    // Заполнение полей
                    txtId.Text = currentActivity.ID_Активности.ToString();
                    txtName.Text = currentActivity.Активность;
                    txtDescription.Text = currentActivity.описание;

                    // Загрузка мероприятий
                    var events = db.МероприятияИнформационнаяБезопасность.ToList();
                    cmbEvent.ItemsSource = events;
                    cmbEvent.DisplayMemberPath = "событие";
                    cmbEvent.SelectedValuePath = "ID_событие";
                    cmbEvent.SelectedValue = currentActivity.Мероприятие_ID;

                    // Загрузка модераторов
                    var moderators = db.Пользователи
                        .Where(u => u.роли_ID == 3)
                        .ToList();
                    cmbModerator.ItemsSource = moderators;
                    cmbModerator.DisplayMemberPath = "фио";
                    cmbModerator.SelectedValuePath = "ID_пользователи";
                    cmbModerator.SelectedValue = currentActivity.Модератор_ID;

                    // Заполнение дней мероприятия
                    if (currentEvent != null && currentEvent.дни.HasValue)
                    {
                        for (int i = 1; i <= currentEvent.дни; i++)
                        {
                            cmbDay.Items.Add($"День {i}");
                        }
                        cmbDay.SelectedIndex = currentActivity.День.HasValue ?
                            currentActivity.День.Value - 1 : 0;
                    }

                    // Информация о жюри
                    UpdateJuryInfo();

                    // Обновление временных слотов
                    UpdateAvailableTimeSlots();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateJuryInfo()
        {
            if (currentActivity.АктивностиЖюри != null)
            {
                txtJuryInfo.Text = "Для этой активности уже назначено жюри. " +
                    "Изменение времени и дня может быть ограничено.";
            }
            else
            {
                txtJuryInfo.Text = "Жюри не назначено.";
            }
        }

        private void CmbDay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAvailableTimeSlots();
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
                        .Where(a => a.Мероприятие_ID == currentEvent.ID_событие &&
                               a.День == selectedDay &&
                               a.ID_Активности != activityId) // Исключаем текущую активность
                        .OrderBy(a => a.Время_начала)
                        .ToList();

                    // Генерируем доступные временные слоты
                    var availableTimes = GenerateTimeSlots(existingActivities);

                    cmbStartTime.Items.Clear();
                    foreach (var time in availableTimes)
                    {
                        cmbStartTime.Items.Add(time.ToString("HH:mm"));
                    }

                    // Устанавливаем текущее время активности
                    if (currentActivity.Время_начала.HasValue)
                    {
                        string currentTime = currentActivity.Время_начала.Value.ToString("HH:mm");
                        cmbStartTime.SelectedItem = currentTime;
                    }
                    else if (cmbStartTime.Items.Count > 0)
                    {
                        cmbStartTime.SelectedIndex = 0;
                    }
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

            DateTime currentSlotStart = currentDayStart.Date + workStart;
            DateTime workEndTime = currentDayStart.Date + workEnd;

            while (currentSlotStart + activityDuration <= workEndTime)
            {
                bool slotAvailable = true;

                foreach (var activity in existingActivities)
                {
                    DateTime activityStart = activity.Время_начала ?? DateTime.MinValue;
                    DateTime activityEnd = activityStart + activityDuration;

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

                // Проверка: если назначено жюри, нельзя менять день и время
                if (currentActivity.АктивностиЖюри_ID != null &&
                    (currentActivity.День != cmbDay.SelectedIndex + 1 ||
                     !IsSameTime(currentActivity.Время_начала, cmbStartTime.SelectedItem.ToString())))
                {
                    MessageBox.Show("Невозможно изменить день или время активности, " +
                        "так как уже назначено жюри. Сначала удалите назначение жюри.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var activity = db.Активности.Find(activityId);
                    if (activity == null) return;

                    activity.Активность = txtName.Text;
                    activity.описание = txtDescription.Text;
                    activity.День = cmbDay.SelectedIndex + 1;

                    // Преобразуем время начала
                    if (currentEvent != null && currentEvent.дата.HasValue)
                    {
                        DateTime activityDate = currentEvent.дата.Value.AddDays(cmbDay.SelectedIndex);
                        DateTime startTime = activityDate.Date + TimeSpan.Parse(cmbStartTime.SelectedItem.ToString());
                        activity.Время_начала = startTime;
                    }

                    activity.Модератор_ID = cmbModerator.SelectedValue as int?;

                    db.SaveChanges();

                    MessageBox.Show("Изменения сохранены!", "Успех",
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

        private bool IsSameTime(DateTime? currentTime, string newTime)
        {
            if (!currentTime.HasValue) return false;
            return currentTime.Value.ToString("HH:mm") == newTime;
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