using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class SelectedProgram
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public DateTime SelectedAt { get; set; } = DateTime.Now;
    }
}
