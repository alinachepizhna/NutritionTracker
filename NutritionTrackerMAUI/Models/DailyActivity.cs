using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class DailyActivity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int UserId { get; set; }

        public DateTime Date { get; set; } = DateTime.Today;

        // Показники
        public int Steps { get; set; } // Кроки
        public int ActiveMinutes { get; set; } // Хвилини активності
        public int SittingHours { get; set; } // Години сидіння
        public int WaterMilliliters { get; set; } // Вода (можна інтегрувати зі звичками)
    }
}