using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class Dish
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } // Назва страви
        public double TotalCalories { get; set; }
        public double TotalProtein { get; set; }
        public double TotalFat { get; set; }
        public double TotalCarbs { get; set; }
        public double TotalWeight { get; set; } // Загальна вага страви
    }
}