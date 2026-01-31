using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace УП_02._01_System_for_organizing_conferences.Events
{
    public partial class AssignJuryWindow : Window
    {
        private int activityId;
        private Активности currentActivity;
        private List<Пользователи> availableJury;

        public AssignJuryWindow(int activityId)
        {
            InitializeComponent();
            this.activityId = activityId;
            Loaded += AssignJuryWindow_Loaded;
        }

        private void AssignJuryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadActivityData();
            LoadJuryList();
        }

        private void LoadActivityData()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    currentActivity = db.Активности
                        .Include(a => a.МероприятияИнформационнаяБезопасность)
                        .FirstOrDefault(a => a.ID_Активности == activityId);

                    if (currentActivity == null)
                    {
                        MessageBox.Show("Активность не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    txtActivityInfo.Text = $"Активность: {currentActivity.Активность} | " +
                        $"Мероприятие: {currentActivity.МероприятияИнформационнаяБезопасность?.событие}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadJuryList()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Загрузка всех жюри (роль 4)
                    availableJury = db.Пользователи
                        .Where(u => u.роли_ID == 4)
                        .OrderBy(u => u.фио)
                        .ToList();

                    // Загрузка уже назначенного жюри
                    var existingJury = db.АктивностиЖюри
                        .FirstOrDefault(j => j.ID_АктивностьЖюри == currentActivity.АктивностиЖюри_ID);

                    // Настройка ComboBox'ов
                    SetupComboBox(cmbJury1, availableJury, existingJury?.Жюри_1);
                    SetupComboBox(cmbJury2, availableJury, existingJury?.Жюри_2);
                    SetupComboBox(cmbJury3, availableJury, existingJury?.Жюри_3);
                    SetupComboBox(cmbJury4, availableJury, existingJury?.Жюри_4);
                    SetupComboBox(cmbJury5, availableJury, existingJury?.Жюри_5);

                    // Если жюри уже назначены, блокируем чекбоксы
                    if (existingJury != null)
                    {
                        chkJury2.IsChecked = existingJury.Жюри_2 == null;
                        chkJury3.IsChecked = existingJury.Жюри_3 == null;
                        chkJury4.IsChecked = existingJury.Жюри_4 == null;
                        chkJury5.IsChecked = existingJury.Жюри_5 == null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка жюри: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupComboBox(ComboBox comboBox, List<Пользователи> juryList, int? selectedId)
        {
            comboBox.ItemsSource = juryList;
            comboBox.DisplayMemberPath = "фио";
            comboBox.SelectedValuePath = "ID_пользователи";

            if (selectedId.HasValue)
            {
                comboBox.SelectedValue = selectedId.Value;
            }
        }

        private void CmbJury_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateJuryAssignments();
        }

        private void ChkJury_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            ComboBox relatedComboBox = null;

            if (checkbox == chkJury2) relatedComboBox = cmbJury2;
            else if (checkbox == chkJury3) relatedComboBox = cmbJury3;
            else if (checkbox == chkJury4) relatedComboBox = cmbJury4;
            else if (checkbox == chkJury5) relatedComboBox = cmbJury5;

            if (relatedComboBox != null)
            {
                relatedComboBox.IsEnabled = false;
                relatedComboBox.SelectedIndex = -1;
            }
        }

        private void ChkJury_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            ComboBox relatedComboBox = null;

            if (checkbox == chkJury2) relatedComboBox = cmbJury2;
            else if (checkbox == chkJury3) relatedComboBox = cmbJury3;
            else if (checkbox == chkJury4) relatedComboBox = cmbJury4;
            else if (checkbox == chkJury5) relatedComboBox = cmbJury5;

            if (relatedComboBox != null)
            {
                relatedComboBox.IsEnabled = true;
            }
        }

        private bool ValidateJuryAssignments()
        {
            try
            {
                List<int?> selectedJuryIds = new List<int?>
                {
                    cmbJury1.SelectedValue as int?,
                    chkJury2.IsChecked == true ? null : cmbJury2.SelectedValue as int?,
                    chkJury3.IsChecked == true ? null : cmbJury3.SelectedValue as int?,
                    chkJury4.IsChecked == true ? null : cmbJury4.SelectedValue as int?,
                    chkJury5.IsChecked == true ? null : cmbJury5.SelectedValue as int?
                };

                // Проверка обязательного жюри 1
                if (!selectedJuryIds[0].HasValue)
                {
                    ShowError("Необходимо назначить хотя бы одного члена жюри (Жюри 1)");
                    return false;
                }

                // Проверка на дублирование
                var assignedIds = selectedJuryIds
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                if (assignedIds.Count != assignedIds.Distinct().Count())
                {
                    ShowError("Один человек не может быть назначен дважды. Проверьте список жюри.");
                    return false;
                }

                // Проверка: жюри не может быть модератором этой активности
                if (currentActivity.Модератор_ID.HasValue &&
                    assignedIds.Contains(currentActivity.Модератор_ID.Value))
                {
                    ShowError("Модератор активности не может быть членом жюри для этой же активности.");
                    return false;
                }

                txtError.Visibility = Visibility.Collapsed;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateJuryAssignments())
                    return;

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    // Проверяем, существует ли уже запись жюри для этой активности
                    var activityJury = db.АктивностиЖюри
                        .FirstOrDefault(j => j.ID_АктивностьЖюри == currentActivity.АктивностиЖюри_ID);

                    if (activityJury == null)
                    {
                        activityJury = new АктивностиЖюри();
                        db.АктивностиЖюри.Add(activityJury);
                    }

                    // Заполняем данные
                    activityJury.Активность = currentActivity.Активность;
                    activityJury.Жюри_1 = cmbJury1.SelectedValue as int?;
                    activityJury.Жюри_2 = chkJury2.IsChecked == true ? null : cmbJury2.SelectedValue as int?;
                    activityJury.Жюри_3 = chkJury3.IsChecked == true ? null : cmbJury3.SelectedValue as int?;
                    activityJury.Жюри_4 = chkJury4.IsChecked == true ? null : cmbJury4.SelectedValue as int?;
                    activityJury.Жюри_5 = chkJury5.IsChecked == true ? null : cmbJury5.SelectedValue as int?;

                    db.SaveChanges();

                    // Обновляем ссылку в активности
                    var activity = db.Активности.Find(activityId);
                    if (activity != null)
                    {
                        activity.АктивностиЖюри_ID = activityJury.ID_АктивностьЖюри;
                        db.SaveChanges();
                    }

                    MessageBox.Show("Жюри успешно назначено!", "Успех",
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