using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class Strategy
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique, NotNull]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty; // 🆕 короткий опис стратегії
    }
}
