using SQLite;
using System;

namespace NutritionTrackerMAUI.Models
{
    public class TrainingPlan
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int GoalId { get; set; }
        public int StrategyId { get; set; }
        public DateTime Date { get; set; } // точна дата тренування
        public bool IsExtraWorkout { get; set; } = false;
        public string DayOfWeek { get; set; } = string.Empty; // Наприклад: "Понеділок"
        public string WorkoutType { get; set; } = string.Empty; // "Руки", "Ноги", "FullBody", "Відновлення"
        public string Exercises { get; set; } = string.Empty;  // Список вправ у форматі CSV
    }
}
