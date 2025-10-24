using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;   // Ім'я
        public string LastName { get; set; } = string.Empty;    // Прізвище
        public string Email { get; set; } = string.Empty;       // Email
        public string PasswordHash { get; set; } = string.Empty; // Хеш пароля

        public User() { } // Обов'язковий порожній конструктор для SQLite
    }
}
