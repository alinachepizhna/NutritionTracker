using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class AnthropometricData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }  // ������ �� ������������

        public double Height { get; set; } // ����
        public double Weight { get; set; } // ����
        public int Age { get; set; }       // ³�
        public string Gender { get; set; } = string.Empty; // �����
        public string MeasurementSystem { get; set; } = "Metric"; // "Metric" ��� "Imperial"

        public AnthropometricData() { }
    }
}
