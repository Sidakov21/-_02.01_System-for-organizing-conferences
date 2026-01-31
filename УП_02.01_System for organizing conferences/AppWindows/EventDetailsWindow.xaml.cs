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

namespace УП_02._01_System_for_organizing_conferences.AppWindows
{
    /// <summary>
    /// Логика взаимодействия для EventDetailsWindow.xaml
    /// </summary>
    public partial class EventDetailsWindow : Window
    {
        private int eventId;

        public EventDetailsWindow(int eventId)
        {
            InitializeComponent();
            this.eventId = eventId;
            LoadEventDetails();
        }

        private void LoadEventDetails()
        {
            try
            {
                using (var db = new DB_YP_OrgConfTESTEntities())
                {
                    var eventItem = db.МероприятияИнформационнаяБезопасность
                        .Include(e => e.Города)
                        .Include(e => e.Направления)
                        .Include(e => e.Мероприятия)
                        .Include(e => e.Активности.Select(a => a.Пользователи))
                        .FirstOrDefault(e => e.ID_событие == eventId);

                    if (eventItem == null)
                    {
                        MessageBox.Show("Мероприятие не найдено", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Заполнение данных
                    txtEventTitle.Text = eventItem.событие;
                    txtEventDate.Text = eventItem.дата?.ToString("dd.MM.yyyy");
                    txtCity.Text = eventItem.Города?.город;
                    txtDirection.Text = eventItem.Направления?.направление;
                    txtDuration.Text = $"{eventItem.дни} дней";
                    txtEventType.Text = eventItem.Мероприятия?.мероприятие;

                    // Загрузка активностей
                    lvActivities.ItemsSource = eventItem.Активности.ToList();

                    // TODO: Загрузка логотипа (предполагаем, что есть поле для пути к изображению)
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
