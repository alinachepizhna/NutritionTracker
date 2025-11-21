using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class Habit
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int UserId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; } 
        public string Icon { get; set; }

        public int FrequencyType { get; set; }
        public string TargetDays { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}