using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Collections.ObjectModel;

namespace NutritionTrackerMAUI.Views
{
    public partial class FoodDiaryPage : ContentPage
    {
        private readonly User _user;
        private readonly SqliteDatabaseService _db;
        private DateTime _currentDate = DateTime.Today;

        public ObservableCollection<FoodLogEntry> DailyLogs { get; set; } = new();

        public FoodDiaryPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user;
            _db = db;

            FoodCollection.ItemsSource = DailyLogs;
            LogDatePicker.Date = _currentDate;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadLogsForDate(_currentDate);
        }

        private async Task LoadLogsForDate(DateTime date)
        {
            DailyLogs.Clear();
            var logs = await _db.GetFoodLogsAsync(_user.Id, date);

            foreach (var log in logs) DailyLogs.Add(log);

            double currentCals = logs.Sum(x => x.Calories);
            double currentProt = logs.Sum(x => x.Protein);
            double currentFat = logs.Sum(x => x.Fat);
            double currentCarbs = logs.Sum(x => x.Carbs);

            var anthropometry = (await _db.GetUserDataAsync(_user.Id)).OrderByDescending(a => a.Date).FirstOrDefault();
            var (goal, strategy) = await _db.GetLatestGoalWithStrategyAsync(_user.Id);
            var targets = NutritionCalculator.CalculateTargets(_user, anthropometry, goal, strategy);

            CaloriesLabel.Text = $"{currentCals} / {targets.Calories} ккал";
            CaloriesProgress.Progress = targets.Calories > 0 ? currentCals / targets.Calories : 0;
            ProteinLabel.Text = $"{currentProt:F0} / {targets.Protein:F0}г";
            FatLabel.Text = $"{currentFat:F0} / {targets.Fat:F0}г";
            CarbsLabel.Text = $"{currentCarbs:F0} / {targets.Carbs:F0}г";
        }

        private async void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            _currentDate = e.NewDate;
            await LoadLogsForDate(_currentDate);
        }
        private void OnPrevDayClicked(object sender, EventArgs e) => LogDatePicker.Date = LogDatePicker.Date.AddDays(-1);
        private void OnNextDayClicked(object sender, EventArgs e) => LogDatePicker.Date = LogDatePicker.Date.AddDays(1);

        // --- ДІЇ ---
        private async void OnOpenDatabaseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new FoodDatabasePage(_user, _db));
        }

        private async void OnAddFoodClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync("Додати їжу", "Назва продукту:");
            if (string.IsNullOrWhiteSpace(name)) return;

            string calsStr = await DisplayPromptAsync("Калорії", "Кількість ккал:", keyboard: Keyboard.Numeric);
            if (!double.TryParse(calsStr, out double cals)) return;

            string mealType = await DisplayActionSheet("Оберіть прийом їжі", "Скасувати", null, "Сніданок", "Обід", "Вечеря", "Перекус");
            if (mealType == "Скасувати" || mealType == null) return;

            var newLog = new FoodLogEntry
            {
                UserId = _user.Id,
                Date = _currentDate,
                MealType = mealType,
                Name = name,
                Calories = cals,
                Protein = cals * 0.07,
                Fat = cals * 0.03,
                Carbs = cals * 0.10
            };

            await _db.AddFoodLogAsync(newLog);
            await LoadLogsForDate(_currentDate);
        }

        private async void OnCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedEntry = e.CurrentSelection.FirstOrDefault() as FoodLogEntry;
            if (selectedEntry == null) return;

            if (sender is CollectionView cv) cv.SelectedItem = null;

            string action = await DisplayActionSheet(
                $"Меню: {selectedEntry.Name}",
                "Скасувати",
                "Видалити",
                "Змінити прийом їжі",
                "Редагувати назву",
                "Редагувати калорії");

            switch (action)
            {
                case "Видалити": await DeleteEntryAsync(selectedEntry); break;
                case "Змінити прийом їжі": await EditEntryMealTypeAsync(selectedEntry); break;
                case "Редагувати назву": await EditEntryNameAsync(selectedEntry); break;
                case "Редагувати калорії": await EditEntryCaloriesAsync(selectedEntry); break;
            }
        }

        private async void OnDeleteEntryClicked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is FoodLogEntry entry)
            {
                await DeleteEntryAsync(entry);
            }
        }

        private async Task DeleteEntryAsync(FoodLogEntry entry)
        {
            bool confirm = await DisplayAlert("Видалення", $"Видалити '{entry.Name}'?", "Так", "Ні");
            if (confirm)
            {
                await _db.DeleteFoodLogAsync(entry);
                await LoadLogsForDate(_currentDate);
            }
        }

        private async Task EditEntryMealTypeAsync(FoodLogEntry entry)
        {
            string newMeal = await DisplayActionSheet("Перенести в:", "Скасувати", null, "Сніданок", "Обід", "Вечеря", "Перекус");
            if (newMeal == "Скасувати" || newMeal == null || newMeal == entry.MealType) return;
            entry.MealType = newMeal;
            await _db.UpdateFoodLogAsync(entry);
            await LoadLogsForDate(_currentDate);
        }

        private async Task EditEntryNameAsync(FoodLogEntry entry)
        {
            string newName = await DisplayPromptAsync("Редагування", "Нова назва:", initialValue: entry.Name);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                entry.Name = newName;
                await _db.UpdateFoodLogAsync(entry);
                await LoadLogsForDate(_currentDate);
            }
        }

        private async Task EditEntryCaloriesAsync(FoodLogEntry entry)
        {
            string newCalsStr = await DisplayPromptAsync("Редагування", "Ккал:", initialValue: entry.Calories.ToString(), keyboard: Keyboard.Numeric);
            if (double.TryParse(newCalsStr, out double newCals))
            {
                entry.Calories = newCals;
                entry.Protein = newCals * 0.07; entry.Fat = newCals * 0.03; entry.Carbs = newCals * 0.10;
                await _db.UpdateFoodLogAsync(entry);
                await LoadLogsForDate(_currentDate);
            }
        }

        private async void OnDietarySettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DietarySettingsPage(_user, _db));
        }
    }
}