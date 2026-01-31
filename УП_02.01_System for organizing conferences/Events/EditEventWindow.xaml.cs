using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Data.Entity;

namespace УП_02._01_System_for_organizing_conferences.Events
{
    public partial class EditEventWindow : Window
    {
        private int eventId;
        private МероприятияИнформационнаяБезопасность currentEvent;
        private string logoPath = "";

        public EditEventWindow(int eventId)
        {
            InitializeComponent();
            this.eventId = eventId;
            Loaded += EditEventWindow_Loaded;
        }

        private void EditEventWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
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

                    // Загрузка данных мероприятия
                    currentEvent = db.МероприятияИнформационнаяБезопасность
                        .Include(e => e.Города)
                        .Include(e => e.Направления)
                        .FirstOrDefault(e => e.ID_событие == eventId);

                    if (currentEvent == null)
                    {
                        MessageBox.Show("Мероприятие не найдено", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Заполнение полей
                    txtName.Text = currentEvent.событие;
                    txtDescription.Text = currentEvent.описание;

                    if (currentEvent.дата.HasValue)
                    {
                        dpStartDate.SelectedDate = currentEvent.дата.Value;
                        if (currentEvent.дни.HasValue)
                        {
                            dpEndDate.SelectedDate = currentEvent.дата.Value.AddDays(currentEvent.дни.Value - 1);
                        }
                    }

                    if (currentEvent.город.HasValue)
                        cmbCity.SelectedValue = currentEvent.город.Value;

                    if (currentEvent.направление_ID.HasValue)
                        cmbDirection.SelectedValue = currentEvent.направление_ID.Value;

                    // Логотип
                    if (!string.IsNullOrEmpty(currentEvent.логотип))
                    {
                        try
                        {
                            logoPath = currentEvent.логотип;
                            var bitmap = new System.Windows.Media.Imaging.BitmapImage(
                                new Uri(logoPath, UriKind.RelativeOrAbsolute));
                            imgLogo.Source = bitmap;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChangeLogo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Выберите логотип"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    logoPath = openFileDialog.FileName;
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(logoPath));
                    imgLogo.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Введите название мероприятия", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!dpStartDate.SelectedDate.HasValue || !dpEndDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите даты", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (dpEndDate.SelectedDate.Value < dpStartDate.SelectedDate.Value)
                {
                    txtDateError.Text = "Дата окончания не может быть раньше даты начала";
                    txtDateError.Visibility = Visibility.Visible;
                    return;
                }

                txtDateError.Visibility = Visibility.Collapsed;

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var eventToUpdate = db.МероприятияИнформационнаяБезопасность.Find(eventId);
                    if (eventToUpdate == null) return;

                    eventToUpdate.событие = txtName.Text;
                    eventToUpdate.дата = dpStartDate.SelectedDate.Value;
                    eventToUpdate.дни = (dpEndDate.SelectedDate.Value - dpStartDate.SelectedDate.Value).Days + 1;
                    eventToUpdate.город = (int?)cmbCity.SelectedValue;
                    eventToUpdate.направление_ID = (int?)cmbDirection.SelectedValue;
                    eventToUpdate.описание = txtDescription.Text;

                    if (!string.IsNullOrEmpty(logoPath))
                        eventToUpdate.логотип = logoPath;

                    db.SaveChanges();

                    MessageBox.Show("Мероприятие успешно обновлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}