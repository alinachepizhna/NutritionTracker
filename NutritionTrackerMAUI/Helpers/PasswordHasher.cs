using System.Security.Cryptography;
using System.Text;

namespace NutritionTrackerMAUI.Helpers
{
    public static class PasswordHasher
    {
        // Хешування пароля
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Перевірка пароля (порівняння хешів)
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            string enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }

        // Оцінка надійності (опціонально)
        public static string GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return "Слабкий";

            int score = 0;
            if (password.Length >= 8) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]")) score++;

            return score switch
            {
                >= 4 => "Сильний",
                3 => "Середній",
                _ => "Слабкий"
            };
        }
    }
}
