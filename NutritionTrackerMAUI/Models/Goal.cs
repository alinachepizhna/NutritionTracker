using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class Goal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }        // �������� �� �����������
        public string Description { get; set; } = string.Empty;  // ���� ���
        public DateTime StartDate { get; set; }                 // �������
        public DateTime EndDate { get; set; }                   // ʳ����
        public string Strategy { get; set; } = string.Empty;     // ����. "�������", "ϳ�������", "��������"
    }
}
