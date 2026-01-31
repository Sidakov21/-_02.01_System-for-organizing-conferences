using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace УП_02._01_System_for_organizing_conferences
{
    public static class CurrentUser
    {
        public static Пользователи User { get; private set; }
        public static bool IsAuthenticated => User != null;
        public static string FullName => User?.фио ?? "Гость";
        public static int? RoleId => User?.роли_ID;
        public static string RoleName => User?.Роли?.Роль ?? "Гость";
        public static int? UserId => User?.ID_пользователи;

        public static void Initialize(Пользователи user)
        {
            User = user;
        }

        public static void Logout()
        {
            User = null;
        }

        public static string GetGreeting()
        {
            if (!IsAuthenticated) return "Добро пожаловать, Гость!";

            // Извлечение имени из ФИО
            string[] nameParts = FullName.Split(' ');
            string firstName = nameParts.Length > 1 ? nameParts[1] : nameParts[0];

            // Определение времени суток
            int hour = DateTime.Now.Hour;
            string timeOfDay = hour < 12 ? "утро" : hour < 18 ? "день" : "вечер";

            return $"Доброе {timeOfDay}, {firstName}!";
        }
    }
}
