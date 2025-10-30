using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class Goal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }        // Прив’язка до користувача
        public string Description { get; set; } = string.Empty;  // Опис цілі
        public DateTime StartDate { get; set; }                 // Початок
        public DateTime EndDate { get; set; }                   // Кінець
        public string Strategy { get; set; } = string.Empty;     // Напр. "Дефіцит", "Підтримка", "Надлишок"
    }
}
