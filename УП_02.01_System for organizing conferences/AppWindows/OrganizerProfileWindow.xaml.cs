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
    /// Логика взаимодействия для OrganizerProfileWindow.xaml
    /// </summary>
    public partial class OrganizerProfileWindow : Window
    {
        private Пользователи currentUser;
        private bool isPasswordChanged = false;

        public OrganizerProfileWindow()
        {
            InitializeComponent();
            Loaded += OrganizerProfileWindow_Loaded;
        }

        private void OrganizerProfileWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.IsAuthenticated)
            {
                MessageBox.Show("Пользователь не авторизован", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            LoadUserData();
            LoadCountries();
        }

        private void LoadUserData()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    currentUser = db.Пользователи
                        .Include("Страны")
                        .FirstOrDefault(u => u.ID_пользователи == CurrentUser.UserId);

                    if (currentUser == null)
                    {
                        MessageBox.Show("Пользователь не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Заполнение полей
                    txtUserId.Text = currentUser.ID_пользователи.ToString();
                    txtFullName.Text = currentUser.фио;
                    txtEmail.Text = currentUser.почта;
                    txtPhone.Text = currentUser.телефон;
                    dpBirthDate.SelectedDate = currentUser.дата_рождения;

                    // Установка пола
                    if (!string.IsNullOrEmpty(currentUser.пол))
                    {
                        foreach (ComboBoxItem item in cmbGender.Items)
                        {
                            if (item.Content.ToString() == currentUser.пол)
                            {
                                cmbGender.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Загрузка фото
                    if (!string.IsNullOrEmpty(currentUser.фото))
                    {
                        try
                        {
                            var bitmap = new System.Windows.Media.Imaging.BitmapImage(
                                new Uri(currentUser.фото, UriKind.RelativeOrAbsolute));
                            imgProfilePhoto.Source = bitmap;
                        }
                        catch
                        {
                            // Фото не загрузилось
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCountries()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var countries = db.Страны.ToList();
                    cmbCountry.ItemsSource = countries;
                    cmbCountry.DisplayMemberPath = "Название_страны";
                    cmbCountry.SelectedValuePath = "ID_Страны";

                    // Установка текущей страны пользователя
                    if (currentUser?.страны_ID != null)
                    {
                        cmbCountry.SelectedValue = currentUser.страны_ID;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки стран: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*",
                Title = "Выберите фото профиля"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(openFileDialog.FileName));
                    imgProfilePhoto.Source = bitmap;
                    currentUser.фото = openFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChkShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            if (checkbox == chkShowCurrent)
            {
                txtCurrentPassword.Visibility = Visibility.Collapsed;
                // Здесь нужно добавить TextBox для показа пароля
                // Для упрощения оставляем как есть
            }
            else if (checkbox == chkShowNew)
            {
                txtNewPassword.Visibility = Visibility.Collapsed;
            }
            else if (checkbox == chkShowConfirm)
            {
                txtConfirmPassword.Visibility = Visibility.Collapsed;
            }
        }

        private void ChkShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            if (checkbox == chkShowCurrent)
            {
                txtCurrentPassword.Visibility = Visibility.Visible;
            }
            else if (checkbox == chkShowNew)
            {
                txtNewPassword.Visibility = Visibility.Visible;
            }
            else if (checkbox == chkShowConfirm)
            {
                txtConfirmPassword.Visibility = Visibility.Visible;
            }
        }

        private bool ValidatePasswordChange()
        {
            // Если поля пароля пустые, значит пароль не меняется
            if (string.IsNullOrWhiteSpace(txtCurrentPassword.Password) &&
                string.IsNullOrWhiteSpace(txtNewPassword.Password) &&
                string.IsNullOrWhiteSpace(txtConfirmPassword.Password))
            {
                return true;
            }

            // Проверка текущего пароля
            if (txtCurrentPassword.Password != currentUser.пароль)
            {
                txtPasswordError.Text = "Текущий пароль неверен";
                txtPasswordError.Visibility = Visibility.Visible;
                return false;
            }

            // Проверка нового пароля
            if (txtNewPassword.Password.Length < 6)
            {
                txtPasswordError.Text = "Новый пароль должен содержать минимум 6 символов";
                txtPasswordError.Visibility = Visibility.Visible;
                return false;
            }

            // Проверка подтверждения
            if (txtNewPassword.Password != txtConfirmPassword.Password)
            {
                txtPasswordError.Text = "Новый пароль и подтверждение не совпадают";
                txtPasswordError.Visibility = Visibility.Visible;
                return false;
            }

            txtPasswordError.Visibility = Visibility.Collapsed;
            isPasswordChanged = true;
            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    MessageBox.Show("Введите ФИО", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Введите email", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!ValidatePasswordChange())
                {
                    return;
                }

                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var user = db.Пользователи.Find(currentUser.ID_пользователи);
                    if (user == null) return;

                    // Обновление данных
                    user.фио = txtFullName.Text;
                    user.почта = txtEmail.Text;
                    user.дата_рождения = dpBirthDate.SelectedDate;
                    user.телефон = txtPhone.Text;
                    user.пол = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();

                    if (cmbCountry.SelectedValue != null)
                    {
                        user.страны_ID = (int)cmbCountry.SelectedValue;
                    }

                    if (!string.IsNullOrEmpty(currentUser.фото))
                    {
                        user.фото = currentUser.фото;
                    }

                    // Обновление пароля если нужно
                    if (isPasswordChanged)
                    {
                        user.пароль = txtNewPassword.Password;
                    }

                    db.SaveChanges();

                    // Обновление текущего пользователя
                    CurrentUser.Initialize(user);

                    MessageBox.Show("Данные успешно сохранены", "Успех",
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
