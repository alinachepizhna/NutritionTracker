using Microsoft.Maui.Graphics;
using System;

namespace NutritionTrackerMAUI.Models
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public string WorkoutType { get; set; } = "";
        public Color BackgroundColor { get; set; } = Colors.LightGray;
        public string DateText { get; set; } = string.Empty;
        public bool IsToday => Date.Date == DateTime.Today;
        public Color TextColor { get; set; } = Colors.Black;
    }
}
