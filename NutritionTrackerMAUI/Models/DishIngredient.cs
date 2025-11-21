using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class DishIngredient
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int DishId { get; set; }      // До якої страви належить
        public int FoodItemId { get; set; }  // ID продукту з бази
        public string Name { get; set; }     // Копія назви для зручності
        public double Weight { get; set; }   // Скільки грам цього інгредієнта
    }
}