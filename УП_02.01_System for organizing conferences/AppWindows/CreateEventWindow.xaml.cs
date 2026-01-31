using Microsoft.Win32;
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
    /// Логика взаимодействия для CreateEventWindow.xaml
    /// </summary>
    public partial class CreateEventWindow : Window
    {
        private string eventLogoPath = "";

        public CreateEventWindow()
        {
            InitializeComponent();
            Loaded += CreateEventWindow_Loaded;
        }

        private void CreateEventWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            UpdateScheduleInfo();
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

                    // Загрузка городов
                    var cities = db.Города.ToList();
                    cmbCity.ItemsSource = cities;
                    cmbCity.DisplayMemberPath = "город";
                    cmbCity.SelectedValuePath = "ID_город";

                    // Загрузка типов мероприятий
                    var eventTypes = db.Мероприятия.ToList();
                    cmbEventType.ItemsSource = eventTypes;
                    cmbEventType.DisplayMemberPath = "мероприятие";
                    cmbEventType.SelectedValuePath = "ID_мероприятия";

                    // Установка дат по умолчанию
                    dpStartDate.SelectedDate = DateTime.Today.AddDays(7);
                    dpEndDate.SelectedDate = DateTime.Today.AddDays(8);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateScheduleInfo()
        {
            if (dpStartDate.SelectedDate.HasValue && dpEndDate.SelectedDate.HasValue)
            {
                var startDate = dpStartDate.SelectedDate.Value;
                var endDate = dpEndDate.SelectedDate.Value;

                if (endDate < startDate)
                {
                    txtDateError.Text = "Дата окончания не может быть раньше даты начала";
                    txtDateError.Visibility = Visibility.Visible;
                    return;
                }

                txtDateError.Visibility = Visibility.Collapsed;
                var days = (endDate - startDate).Days + 1;
                txtDuration.Text = days.ToString();

                // Расчет расписания
                if (chkAutoSchedule.IsChecked == true)
                {
                    int activityCount = CalculateActivityCount(days);
                    txtScheduleInfo.Text = $"Будет создано примерно {activityCount} временных интервалов " +
                                          $"для активностей (90 мин + 15 мин перерыв)";
                }
            }
        }

        private int CalculateActivityCount(int days)
        {
            // Рассчитываем количество активностей в день
            // Предполагаем рабочий день с 9:00 до 18:00, активность 90 мин + 15 мин перерыв
            int hoursPerDay = 9; // 9-18
            int activityDuration = 105; // 90 + 15 минут
            int activitiesPerDay = (hoursPerDay * 60) / activityDuration;

            return activitiesPerDay * days;
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateScheduleInfo();
        }

        private void BtnSelectLogo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*",
                Title = "Выберите логотип мероприятия"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    eventLogoPath = openFileDialog.FileName;
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(eventLogoPath));
                    imgEventLogo.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtEventName.Text))
                {
                    MessageBox.Show("Введите название мероприятия", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cmbDirection.SelectedItem == null)
                {
                    MessageBox.Show("Выберите направление", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cmbCity.SelectedItem == null)
                {
                    MessageBox.Show("Выберите город", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cmbEventType.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип мероприятия", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!dpStartDate.SelectedDate.HasValue || !dpEndDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите даты начала и окончания", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (dpEndDate.SelectedDate.Value < dpStartDate.SelectedDate.Value)
                {
                    MessageBox.Show("Дата окончания не может быть раньше даты начала", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Создание мероприятия
                    var newEvent = new МероприятияИнформационнаяБезопасность
                    {
                        событие = txtEventName.Text,
                        дата = dpStartDate.SelectedDate.Value,
                        дни = int.Parse(txtDuration.Text),
                        город = (int)cmbCity.SelectedValue,
                        мероприятия_ID = (int)cmbEventType.SelectedValue,
                        // TODO: Добавить поле для направления в таблице или создать связь
                        // направление_ID = (int)cmbDirection.SelectedValue
                    };

                    db.МероприятияИнформационнаяБезопасность.Add(newEvent);
                    db.SaveChanges();

                    // Создание временной сетки активностей если нужно
                    if (chkAutoSchedule.IsChecked == true)
                    {
                        CreateActivityTimeSlots(newEvent.ID_событие);
                    }

                    MessageBox.Show("Мероприятие успешно создано!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания мероприятия: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateActivityTimeSlots(int eventId)
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var eventItem = db.МероприятияИнформационнаяБезопасность.Find(eventId);
                    if (eventItem == null) return;

                    DateTime currentDate = eventItem.дата.Value;
                    DateTime endDate = currentDate.AddDays(eventItem.дни.Value - 1);

                    // Настройки времени
                    TimeSpan dayStart = TimeSpan.Parse((cmbDayStart.SelectedItem as ComboBoxItem)?.Content.ToString());
                    TimeSpan dayEnd = TimeSpan.Parse((cmbDayEnd.SelectedItem as ComboBoxItem)?.Content.ToString());
                    TimeSpan activityDuration = TimeSpan.FromMinutes(90);
                    TimeSpan breakDuration = TimeSpan.FromMinutes(15);

                    int dayNumber = 1;
                    while (currentDate <= endDate)
                    {
                        TimeSpan currentTime = dayStart;

                        while (currentTime + activityDuration <= dayEnd)
                        {
                            var activity = new Активности
                            {
                                Активность = $"Активность {dayNumber}.{(int)((currentTime - dayStart).TotalMinutes / 105) + 1}",
                                Мероприятие_ID = eventId,
                                День = dayNumber,
                                Время_начала = currentDate.Date + currentTime
                            };

                            db.Активности.Add(activity);

                            // Переход к следующему временному интервалу
                            currentTime = currentTime.Add(activityDuration).Add(breakDuration);
                        }

                        currentDate = currentDate.AddDays(1);
                        dayNumber++;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания временной сетки: {ex.Message}", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
