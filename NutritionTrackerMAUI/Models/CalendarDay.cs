using Microsoft.Maui.Graphics;
using System;

namespace NutritionTrackerMAUI.Models
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public string DateText { get; set; } = string.Empty;
        public Color BackgroundColor { get; set; } = Colors.Gray;
        public string WorkoutType { get; set; } = "Відпочинок";
        public Color TextColor { get; set; } = Colors.White;
        public bool IsToday { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsExtraWorkout { get; set; } = false;
        public bool IsCustomProgramMode { get; set; } = false;
    }
}
