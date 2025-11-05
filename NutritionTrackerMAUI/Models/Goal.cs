using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class Goal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; } // Прив’язка до користувача

        public string Description { get; set; } = string.Empty; // Наприклад: "Схуднення", "Набір ваги"

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int StrategyId { get; set; } // 🔗 зовнішній ключ до таблиці Strategy
    }
}
