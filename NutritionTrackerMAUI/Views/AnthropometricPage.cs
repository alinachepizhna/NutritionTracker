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
        private readonly AnthropometricWidget _widget;

        public AnthropometricPage(User user, SqliteDatabaseService db)
        {
            _user = user;
            _db = db;

            Title = "Антропометричні дані";

            _widget = new AnthropometricWidget(_user, _db);
            _widget.CalculationCompleted += OnCalculationCompleted;

            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Padding = 20,
                    Spacing = 15,
                    Children = { _widget }
                }
            };
        }

        private async void OnCalculationCompleted(object sender, EventArgs e)
        {
            // Переходимо на GoalPage
            await Navigation.PushAsync(new GoalPage(_user, _db));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _widget.CalculationCompleted -= OnCalculationCompleted;
        }
    }
}
