using System.Security.Cryptography;
using System.Text;

namespace NutritionTrackerMAUI.Helpers
{
    public static class PasswordHasher
    {
        // Метод для хешування пароля
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Метод для оцінки надійності пароля
        public static string GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return "Слабкий";

            int score = 0;
            if (password.Length >= 8) score++; // Довжина >= 8 символів
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")) score++; // Велика буква
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")) score++; // Мала буква
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]")) score++;  // Цифра
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]")) score++; // Спецсимвол

            return score switch
            {
                >= 4 => "Сильний",
                3 => "Середній",
                _ => "Слабкий"
            };
        }
    }
}
