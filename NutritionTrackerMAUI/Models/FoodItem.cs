using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class FoodItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }       // Назва (напр. "Гречка варена")
        public string Category { get; set; }   // Категорія (Крупи, М'ясо...)

        // Показники на 100 г продукту
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }

        public bool IsCustom { get; set; } = false; // Чи це додав користувач вручну
    }
}