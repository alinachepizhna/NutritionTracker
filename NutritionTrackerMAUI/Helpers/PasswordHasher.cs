using System.Security.Cryptography;
using System.Text;

namespace NutritionTrackerMAUI.Helpers
{
    public static class PasswordHasher
    {
        // ����� ��� ��������� ������
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // ����� ��� ������ �������� ������
        public static string GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return "�������";

            int score = 0;
            if (password.Length >= 8) score++; // ������� >= 8 �������
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")) score++; // ������ �����
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")) score++; // ���� �����
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]")) score++;  // �����
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]")) score++; // ����������

            return score switch
            {
                >= 4 => "�������",
                3 => "�������",
                _ => "�������"
            };
        }
    }
}
