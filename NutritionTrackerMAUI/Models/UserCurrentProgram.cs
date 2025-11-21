using SQLite;
using System;

namespace NutritionTrackerMAUI.Models
{
    // Зберігає останню обрану користувачем програму для його поточної цілі.
    public class UserCurrentProgram
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GoalId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public DateTime LastSelectedDate { get; set; }
    }
}