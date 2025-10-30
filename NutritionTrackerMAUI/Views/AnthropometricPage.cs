using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;

namespace NutritionTrackerMAUI.Views
{
    public class AnthropometricPage : ContentPage
    {
        private readonly User _user;
        private readonly SqliteDatabaseService _db;

        // 🔹 Конструктор отримує існуючий екземпляр бази
        public AnthropometricPage(User user, SqliteDatabaseService db)
        {
            _user = user;
            _db = db; // використовуємо той самий об’єкт, а не створюємо новий

            Title = "Антропометричні дані";

            var widget = new AnthropometricWidget(_user, _db);

            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Padding = 20,
                    Spacing = 15,
                    Children = { widget }
                }
            };
        }
    }
}
