// Models/UserDietarySettings.cs
using SQLite;

namespace NutritionTrackerMAUI.Models
{
    public class UserDietarySettings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool AvoidGluten { get; set; }
        public bool AvoidLactose { get; set; }
        public bool AvoidNuts { get; set; }
        public bool AvoidSugar { get; set; }
    }
}