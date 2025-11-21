using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class HabitLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int HabitId { get; set; }

        public DateTime Date { get; set; } 
        public bool IsCompleted { get; set; }
    }
}