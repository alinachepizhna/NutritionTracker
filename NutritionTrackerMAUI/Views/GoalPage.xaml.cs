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

            LoadStrategiesAsync(); // завантаження стратегій при запуску
        }

        private async void LoadStrategiesAsync()
        {
            var strategies = await _db.GetAllStrategiesAsync();

            // Якщо стратегій ще немає — створюємо базові
            if (strategies.Count == 0)
            {
                await _db.AddStrategyAsync(new Strategy { Name = "Дефіцит калорій", Description = "Для схуднення" });
                await _db.AddStrategyAsync(new Strategy { Name = "Підтримка", Description = "Для збереження поточної ваги" });
                await _db.AddStrategyAsync(new Strategy { Name = "Надлишок калорій", Description = "Для набору м’язової маси" });

                strategies = await _db.GetAllStrategiesAsync();
            }

            StrategyPicker.ItemsSource = strategies;
            StrategyPicker.ItemDisplayBinding = new Binding("Name");
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

            var selectedStrategy = (Strategy)StrategyPicker.SelectedItem;

            var goal = new Goal
            {
                UserId = _user.Id,
                Description = GoalTypePicker.SelectedItem.ToString(),
                StartDate = StartDatePicker.Date,
                EndDate = EndDatePicker.Date,
                StrategyId = selectedStrategy.Id
            };

            await _db.AddGoalAsync(goal);
            await DisplayAlert("✅ Успіх", "Ціль збережена!", "OK");

            // Повертаємо користувача на головну
            Application.Current.MainPage = new NavigationPage(new MainPage(_user, _db));
        }
    }
}
