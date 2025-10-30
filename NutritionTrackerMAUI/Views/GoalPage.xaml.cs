using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;

namespace NutritionTrackerMAUI.Views
{
    public partial class GoalPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;

        public GoalPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _db = db;
            _user = user;

            StartDatePicker.Date = DateTime.Today;
            EndDatePicker.Date = DateTime.Today.AddDays(30);
        }

        private async void OnSaveGoalClicked(object sender, EventArgs e)
        {
            if (GoalTypePicker.SelectedItem == null || StrategyPicker.SelectedItem == null)
            {
                await DisplayAlert("Помилка", "Будь ласка, оберіть ціль та стратегію", "OK");
                return;
            }

            if (EndDatePicker.Date < StartDatePicker.Date)
            {
                await DisplayAlert("Помилка", "Дата завершення не може бути раніше початку", "OK");
                return;
            }

            var goal = new Goal
            {
                UserId = _user.Id,
                Description = GoalTypePicker.SelectedItem.ToString(),
                Strategy = StrategyPicker.SelectedItem.ToString(),
                StartDate = StartDatePicker.Date,
                EndDate = EndDatePicker.Date
            };

            await _db.AddGoalAsync(goal);
            await DisplayAlert("✅ Успіх", "Ціль збережена!", "OK");

            // Відкриваємо MainPage як нову root-сторінку
            Application.Current.MainPage = new NavigationPage(new MainPage(_user, _db));
        }
    }
}
