using SQLite;
using System;

namespace NutritionTrackerMAUI.Models
{
    public class FoodLogEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public string MealType { get; set; } // Сніданок, Обід, Вечеря, Перекус
        public string Name { get; set; }     // Назва продукту
        public double Calories { get; set; }
        public double Protein { get; set; }  // Білки
        public double Fat { get; set; }      // Жири
        public double Carbs { get; set; }    // Вуглеводи
    }
}