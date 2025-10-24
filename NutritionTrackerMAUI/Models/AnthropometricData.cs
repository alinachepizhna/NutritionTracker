using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class AnthropometricData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }  // Зв’язок із користувачем

        public double Height { get; set; } // Зріст
        public double Weight { get; set; } // Вага
        public int Age { get; set; }       // Вік
        public string Gender { get; set; } = string.Empty; // Стать
        public string MeasurementSystem { get; set; } = "Metric"; // "Metric" або "Imperial"

        public AnthropometricData() { }
    }
}
