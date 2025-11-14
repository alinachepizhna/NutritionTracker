using SQLite;
namespace NutritionTrackerMAUI.Models
{
    public class WorkoutProgram
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> DailyWorkouts { get; set; } = new List<string>();
        public bool IsLocked { get; set; } = false;
    }
}
