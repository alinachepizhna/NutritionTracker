using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace NutritionTrackerMAUI.Views
{
    public partial class FoodDiaryPage : ContentPage
    {
        private readonly User _user;
        private readonly SqliteDatabaseService _db;
        public ObservableCollection<FoodLogEntry> TodaysFood { get; set; } = new();
        private async void OnOpenDatabaseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new FoodDatabasePage(_user, _db));
        }
        public FoodDiaryPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user;
            _db = db;
            FoodCollection.ItemsSource = TodaysFood;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var logs = await _db.GetFoodLogsAsync(_user.Id, DateTime.Today);
            TodaysFood.Clear();
            foreach (var log in logs) TodaysFood.Add(log);

            var anthropometry = (await _db.GetUserDataAsync(_user.Id)).OrderByDescending(a => a.Date).FirstOrDefault();
            var (goal, strategy) = await _db.GetLatestGoalWithStrategyAsync(_user.Id);

            var targets = NutritionCalculator.CalculateTargets(_user, anthropometry, goal, strategy);

            double eatenCals = logs.Sum(x => x.Calories);
            double eatenProt = logs.Sum(x => x.Protein);
            double eatenFat = logs.Sum(x => x.Fat);
            double eatenCarbs = logs.Sum(x => x.Carbs);

            CaloriesLabel.Text = $"{eatenCals} / {targets.Calories} ккал";
            CaloriesProgress.Progress = targets.Calories > 0 ? eatenCals / targets.Calories : 0;

            ProteinLabel.Text = $"{eatenProt} / {targets.Protein}г";
            FatLabel.Text = $"{eatenFat} / {targets.Fat}г";
            CarbsLabel.Text = $"{eatenCarbs} / {targets.Carbs}г";
        }

        private async void OnAddFoodClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync("Додати їжу", "Назва продукту:");
            if (string.IsNullOrWhiteSpace(name)) return;

            string calsStr = await DisplayPromptAsync("Калорії", "Кількість ккал:", keyboard: Keyboard.Numeric);
            if (!double.TryParse(calsStr, out double cals)) return;
            var newLog = new FoodLogEntry
            {
                UserId = _user.Id,
                Date = DateTime.Now,
                MealType = "Перекус", 
                Name = name,
                Calories = cals,
                Protein = cals * 0.07, 
                Fat = cals * 0.03,
                Carbs = cals * 0.10
            };

            await _db.AddFoodLogAsync(newLog);
            await LoadDataAsync(); 
        }
        private async void OnFoodItemTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not FoodLogEntry selectedEntry) return;

            string action = await DisplayActionSheet(
                $"Меню: {selectedEntry.Name}",
                "Скасувати",
                "Видалити",
                "Редагувати назву",
                "Редагувати калорії");

            switch (action)
            {
                case "Видалити":
                    await DeleteEntryAsync(selectedEntry);
                    break;
                case "Редагувати назву":
                    await EditEntryNameAsync(selectedEntry);
                    break;
                case "Редагувати калорії":
                    await EditEntryCaloriesAsync(selectedEntry);
                    break;
            }
        }

        private async Task DeleteEntryAsync(FoodLogEntry entry)
        {
            bool confirm = await DisplayAlert("Видалення", $"Видалити '{entry.Name}'?", "Так", "Ні");
            if (!confirm) return;

            await _db.DeleteFoodLogAsync(entry);

            // Оновлюємо список та прогрес-бари
            await LoadDataAsync();
        }

        private async Task EditEntryNameAsync(FoodLogEntry entry)
        {
            string newName = await DisplayPromptAsync("Редагування", "Нова назва продукту:", initialValue: entry.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName == entry.Name) return;

            entry.Name = newName;
            await _db.UpdateFoodLogAsync(entry);
            await LoadDataAsync();
        }

        private async void OnDietarySettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DietarySettingsPage(_user, _db));
        }
        private async Task EditEntryCaloriesAsync(FoodLogEntry entry)
        {
            string newCalsStr = await DisplayPromptAsync("Редагування", "Нова калорійність:",
                                                         initialValue: entry.Calories.ToString(),
                                                         keyboard: Keyboard.Numeric);

            if (!double.TryParse(newCalsStr, out double newCals)) return;
            entry.Calories = newCals;
            entry.Protein = newCals * 0.07;
            entry.Fat = newCals * 0.03;
            entry.Carbs = newCals * 0.10;
            await _db.UpdateFoodLogAsync(entry);
            await LoadDataAsync();
        }
    }
}