using Microsoft.Maui.Graphics;
using NutritionTrackerMAUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NutritionTrackerMAUI.Services
{
    public static class CalendarGenerator
    {
        /// <summary>
        /// Генерує календар на вказаний місяць з відображенням типів тренувань та кольорів.
        /// </summary>
        public static ObservableCollection<CalendarDay> GenerateMonthlyCalendar(
            int year,
            int month,
            List<(DateTime date, string workoutType)> workouts)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var calendar = new ObservableCollection<CalendarDay>();

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);

                // Шукаємо тренування для цієї дати
                var workout = workouts.FirstOrDefault(w => w.date.Date == date.Date);

                // Додаємо день у календар
                calendar.Add(new CalendarDay
                {
                    Date = date,
                    WorkoutType = workout.workoutType ?? "",
                    BackgroundColor = workout.workoutType != null
                                      ? WorkoutColorService.GetColor(workout.workoutType) // Колір по типу тренування
                                      : Colors.LightGray
                });
            }

            return calendar;
        }
    }
}
