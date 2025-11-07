using Microsoft.Maui.Graphics;
using System;

namespace NutritionTrackerMAUI.Models
{
    public class CalendarDay
    {
        // Дата
        public DateTime Date { get; set; }

        // Тип тренування
        public string WorkoutType { get; set; } = "";

        // Колір, пов'язаний з типом тренування
        public Color BackgroundColor { get; set; } = Colors.LightGray;

        // Текст для відображення в календарі (формат: день.місяць)
        public string DateText { get; set; } = string.Empty;
        // Визначає, чи цей день сьогодні
        public bool IsToday => Date.Date == DateTime.Today;
    }
}
