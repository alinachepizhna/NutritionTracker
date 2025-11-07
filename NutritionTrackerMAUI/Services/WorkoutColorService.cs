using Microsoft.Maui.Graphics;
using System.Collections.Generic;

namespace NutritionTrackerMAUI.Models
{
    public static class WorkoutColorService
    {
        private static readonly Dictionary<string, Color> _workoutColors = new()
        {
            { "Кардіо", Colors.Red },
            { "Силове", Colors.Blue },
            { "Йога", Colors.Green },
            { "Руки", Colors.Orange },
            { "Ноги", Colors.Purple },
            { "FullBody", Colors.CadetBlue },
            { "Відновлення", Colors.Pink }
        };

        public static IEnumerable<string> WorkoutTypes => _workoutColors.Keys;

        public static Color GetColor(string workoutType)
        {
            return _workoutColors.TryGetValue(workoutType, out var color) ? color : Colors.Gray;
        }
    }
}
