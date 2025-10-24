using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;   // ��'�
        public string LastName { get; set; } = string.Empty;    // �������
        public string Email { get; set; } = string.Empty;       // Email
        public string PasswordHash { get; set; } = string.Empty; // ��� ������

        public User() { } // ����'������� ������� ����������� ��� SQLite
    }
}
